param(
    [Parameter(Mandatory)] [string] $Configuration,
    [Parameter(Mandatory)] [string] $Godot,
    [Parameter(Mandatory)] [string] $StableDataDir,
    [Parameter(Mandatory)] [string] $BetaDataDir,
    [Parameter(Mandatory)] [string] $BetaGameDir,
    [Parameter(Mandatory)] [string] $StableSourceRef,
    [Parameter(Mandatory)] [string] $BetaDll,
    [string] $ModsPath = $env:TASHKENT_BUNDLE_MODS_PATH
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$root = Split-Path -Parent $PSScriptRoot
$modId = 'TashkentSpire2'
$stableVersion = '0.111.0'
$betaVersion = '0.113.1'
$artifacts = Join-Path $root '.artifacts'
$buildDir = Join-Path $artifacts $Configuration
$packages = Join-Path $root 'packages'
$manifestPath = Join-Path $root 'mod_manifest.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath -Encoding UTF8 | ConvertFrom-Json
$releasePath = Join-Path $BetaGameDir 'release_info.json'
if ($IsMacOS -and !(Test-Path -LiteralPath $releasePath)) {
    $releasePath = Join-Path $BetaGameDir 'SlayTheSpire2.app/Contents/Resources/release_info.json'
}
$release = Get-Content -Raw -LiteralPath $releasePath | ConvertFrom-Json

if ($release.version -ne "v$betaVersion" -and $release.version -ne $betaVersion) {
    throw "Expected the beta game at $BetaGameDir to report $betaVersion, found '$($release.version)'."
}
foreach ($dataDir in @($StableDataDir, $BetaDataDir)) {
    if (!(Test-Path -LiteralPath (Join-Path $dataDir 'sts2.dll') -PathType Leaf)) {
        throw "Missing sts2.dll in '$dataDir'. Set StableSts2DataDir or Sts2DataDir in Directory.Build.props."
    }
}
if ($manifest.id -ne $modId -or $manifest.min_game_version -ne $stableVersion -or $manifest.has_pck -or !$manifest.has_dll) {
    throw "mod_manifest.json must declare $modId, min_game_version $stableVersion, has_dll true and has_pck false."
}
$assemblyVersion = ([version] $manifest.version.TrimStart('v', 'V')).ToString()
$expectedAssemblyVersion = [version] "$assemblyVersion.0"
if ([Reflection.AssemblyName]::GetAssemblyName($BetaDll).Version -ne $expectedAssemblyVersion) {
    throw 'Rebuild the beta implementation to match the version in mod_manifest.json.'
}

function Invoke-BuildTool([string] $Tool, [string[]] $Arguments) {
    & $Tool @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool failed with exit code $LASTEXITCODE." }
}

function Export-Pack([string] $Project, [string] $Destination, [string] $Version, [string] $BasePack) {
    $log = Join-Path $buildDir "godot-g$Version.log"
    $arguments = @('--headless', '--editor', '--path', $Project)
    if ($BasePack) {
        Write-Host "Exporting g$Version PCK patch"
        $arguments += @('--patches', $BasePack, '--export-patch', 'BasicExport', $Destination)
    }
    else {
        Write-Host "Exporting g$Version base PCK"
        $arguments += @('--export-pack', 'BasicExport', $Destination)
    }
    # Windows PowerShell reports native stderr as error records, including Godot warnings.
    & {
        $ErrorActionPreference = 'Continue'
        & $Godot @arguments 2>&1
    } | ForEach-Object { $_.ToString() } | Set-Content -LiteralPath $log -Encoding UTF8
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $Destination -PathType Leaf)) {
        throw "Godot export failed. See $log"
    }
    if (Select-String -LiteralPath $log -Quiet -Pattern '^ERROR:|^System\.\w+Exception:') {
        throw "Godot reported export errors. See $log"
    }
    # The diff omits unchanged files, including the Spine remaps.
    if (!$BasePack -and (!(Select-String -LiteralPath $log -Quiet -Pattern 'tashigan_2\.skel\.import') -or
        !(Select-String -LiteralPath $log -Quiet -Pattern 'tashigan_2\.atlas\.import'))) {
        throw "Godot did not pack the Spine resources. Check the editor extension and $log"
    }
}

function Install-Bundle([string] $Source, [string] $ModsDirectory) {
    New-Item -ItemType Directory -Force -Path $ModsDirectory | Out-Null
    $mods = (Resolve-Path -LiteralPath $ModsDirectory).ProviderPath
    $destination = Join-Path $mods $modId
    $gameExecutable = Join-Path (Split-Path -Parent $mods) 'SlayTheSpire2.exe'
    if ((Test-Path -LiteralPath $gameExecutable -PathType Leaf) -and
        (Get-Process -Name SlayTheSpire2 -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $gameExecutable })) {
        Write-Warning "Bundle built at $Source. Installation skipped because Slay the Spire 2 is running. Close the game and build again to install."
        return
    }

    # Stage next to mods so the game cannot discover the staged or backup manifest.
    $transaction = New-Item -ItemType Directory -Path (Join-Path (Split-Path -Parent $mods) ".$modId-install-$([guid]::NewGuid().ToString('N'))")
    $staged = Join-Path $transaction.FullName 'new'
    $previous = Join-Path $transaction.FullName 'previous'
    $savedFiles = [Collections.Generic.List[string]]::new()
    $installedFiles = [Collections.Generic.List[string]]::new()
    $canCleanUp = $true
    try {
        Copy-Item -LiteralPath $Source -Destination $staged -Recurse -Force
        $files = @(Get-ChildItem -LiteralPath $staged -Recurse -File |
            Sort-Object { $_.Name -eq 'mod_manifest.json' } |
            ForEach-Object { $_.FullName.Substring($staged.Length + 1) })
        $retiredFiles = @("$modId.json", "$modId.pck", "bin/g$betaVersion/$modId.pck")
        foreach ($directory in @($destination, (Join-Path $destination 'bin'),
                (Join-Path $destination "bin/g$stableVersion"), (Join-Path $destination "bin/g$betaVersion"))) {
            if (Test-Path -LiteralPath $directory) {
                $item = Get-Item -LiteralPath $directory -Force
                if (!$item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                    throw "Expected an ordinary mod directory at $directory."
                }
            }
        }

        foreach ($relative in ($files + $retiredFiles)) {
            $target = Join-Path $destination $relative
            if (Test-Path -LiteralPath $target) {
                $backup = Join-Path $previous $relative
                [IO.Directory]::CreateDirectory((Split-Path -Parent $backup)) | Out-Null
                [IO.File]::Move($target, $backup)
                $savedFiles.Add($relative)
            }
        }
        foreach ($relative in $files) {
            $target = Join-Path $destination $relative
            [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
            [IO.File]::Move((Join-Path $staged $relative), $target)
            $installedFiles.Add($relative)
        }
    }
    catch {
        $installError = $_
        $canCleanUp = $false
        try {
            foreach ($relative in $installedFiles) {
                [IO.File]::Delete((Join-Path $destination $relative))
            }
            foreach ($relative in $savedFiles) {
                [IO.File]::Move((Join-Path $previous $relative), (Join-Path $destination $relative))
            }
            $canCleanUp = $true
        }
        catch {
            throw "Installation failed and could not be rolled back. Backup is at $previous. $($_.Exception.GetBaseException().Message)"
        }
        $failure = $installError.Exception.GetBaseException()
        if ($env:OS -eq 'Windows_NT' -and ($failure.HResult -band 0xffff) -in 5, 32, 33) {
            Write-Warning "Bundle built at $Source. Installation skipped: $($failure.Message) Close any program using the mod or check folder permissions, then build again."
            return
        }
        throw $installError
    }
    finally {
        if ($canCleanUp -and
            (Resolve-Path -LiteralPath $transaction.FullName).ProviderPath -eq $transaction.FullName) {
            Remove-Item -LiteralPath $transaction.FullName -Recurse -Force
        }
    }
    Write-Host "Installed mod bundle: $destination"
}

New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $artifacts '.gdignore') | Out-Null
$commit = & git -C $root rev-parse --verify "$StableSourceRef^{commit}"
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') {
    throw "Cannot resolve the 0.111.0 source ref '$StableSourceRef'."
}
$stablePatch = $null
$stableCacheName = "stable-$commit"
if ($commit -eq '9234a743ea19d750b4adee6ee6c2be1a0295b904') {
    $stablePatch = Join-Path $PSScriptRoot 'compat/g0.111.0-sync-hooks.patch'
    $patchHash = & git -C $root hash-object --no-filters -- $stablePatch
    if ($LASTEXITCODE -ne 0) { throw "Cannot hash $stablePatch." }
    $stableCacheName += '-' + $patchHash.Substring(0, 12)
}
$stable = Join-Path $buildDir $stableCacheName
if (!(Test-Path -LiteralPath (Join-Path $stable '.source-ready'))) {
    $archive = Join-Path $buildDir "$commit.tar"
    New-Item -ItemType Directory -Force -Path $stable | Out-Null
    Invoke-BuildTool git @('-C', $root, 'archive', '--format=tar', "--output=$archive", $commit)
    Invoke-BuildTool tar @('-xf', $archive, '-C', $stable)
    Remove-Item -LiteralPath $archive
    if ($stablePatch) {
        $stablePrefix = $stable.Substring($root.Length + 1).Replace('\', '/')
        Invoke-BuildTool git @('-C', $root, 'apply', "--directory=$stablePrefix", $stablePatch)
    }
    New-Item -ItemType File -Path (Join-Path $stable '.source-ready') | Out-Null
}

# Build the archived source with the same project settings and resource paths as beta.
$projectName = "$modId.Impl.csproj"
Copy-Item -LiteralPath (Join-Path $root $projectName), (Join-Path $root "$modId.Impl.sln"), $manifestPath -Destination $stable -Force
$entry = Join-Path $stable "${modId}Code/MainFile.cs"
$source = [IO.File]::ReadAllText($entry)
$source = $source -replace '(?m)^\s*\[ModInitializer\(nameof\(Initialize\)\)\]\r?\n', ''
$source = $source.Replace('harmony.PatchAll();', 'harmony.PatchAll(typeof(MainFile).Assembly);')
[IO.File]::WriteAllText($entry, $source)
$godotProject = Join-Path $stable 'project.godot'
$settings = [IO.File]::ReadAllText($godotProject).Replace('project/assembly_name="TashkentSpire2"', 'project/assembly_name="TashkentSpire2.Impl"')
[IO.File]::WriteAllText($godotProject, $settings)
$exportPresets = Join-Path $stable 'export_presets.cfg'
$presets = [IO.File]::ReadAllText($exportPresets)
if (!$presets.Contains('exclude_filter="mod_manifest.json,')) {
    $presets = $presets -replace '(?m)^exclude_filter="', 'exclude_filter="mod_manifest.json,TashkentSpire2.json,'
    [IO.File]::WriteAllText($exportPresets, $presets)
}

$spineRoot = Join-Path $root 'bin'
$spineDestination = Join-Path $stable 'bin'
if (Test-Path -LiteralPath $spineRoot) {
    New-Item -ItemType Directory -Force -Path $spineDestination | Out-Null
    Get-ChildItem -LiteralPath $spineRoot -Recurse -File | Where-Object { !$_.Name.StartsWith('~') } |
        ForEach-Object {
            $relativePath = $_.FullName.Substring($spineRoot.Length + 1)
            $destination = Join-Path $spineDestination $relativePath
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
}

$staging = Join-Path $buildDir ([guid]::NewGuid().ToString('N'))
$stableOut = Join-Path $staging "bin/g$stableVersion"
$betaOut = Join-Path $staging "bin/g$betaVersion"
New-Item -ItemType Directory -Force -Path $stableOut, $betaOut | Out-Null
$launcherProject = Join-Path $PSScriptRoot 'Launcher/TashkentSpire2.Launcher.csproj'
$launcherOut = Join-Path $buildDir 'launcher'

$env:BuildBundle = 'false'
$env:NUGET_PACKAGES = $packages
$env:MSBUILDDISABLENODEREUSE = '1'
Invoke-BuildTool dotnet @('build', (Join-Path $stable $projectName), '-c', $Configuration, '-v:q', '-clp:NoSummary',
    "-p:Sts2DataDir=$StableDataDir", "-p:RestorePackagesPath=$packages", '-p:BuildBundle=false')
Invoke-BuildTool dotnet @('build', $launcherProject, '-c', $Configuration, '-v:q', '-clp:NoSummary',
    "-p:Sts2DataDir=$StableDataDir", "-p:RestorePackagesPath=$packages", "-p:Version=$assemblyVersion", '-o', $launcherOut)

$stableDll = Join-Path $stable ".godot/mono/temp/bin/$Configuration/$modId.Impl.dll"
if ([Reflection.AssemblyName]::GetAssemblyName($stableDll).Version -ne $expectedAssemblyVersion) {
    throw 'The stable implementation does not match the version in mod_manifest.json.'
}
Copy-Item -LiteralPath $stableDll -Destination $stableOut
Copy-Item -LiteralPath $BetaDll -Destination $betaOut
Copy-Item -LiteralPath (Join-Path $launcherOut "$modId.dll"), $manifestPath -Destination $staging

$env:Sts2DataDir = $StableDataDir
$basePack = Join-Path $stableOut "$modId.pck"
Export-Pack $stable $basePack $stableVersion
$env:Sts2DataDir = $BetaDataDir
Export-Pack $root (Join-Path $betaOut "$modId.patch.pck") $betaVersion $basePack

$bundle = Join-Path $buildDir $modId
if (Test-Path -LiteralPath $bundle) {
    $backup = Join-Path $buildDir ("previous-" + [guid]::NewGuid().ToString('N'))
    Move-Item -LiteralPath $bundle -Destination $backup
    try { Move-Item -LiteralPath $staging -Destination $bundle }
    catch {
        Move-Item -LiteralPath $backup -Destination $bundle
        throw
    }
    if ((Resolve-Path -LiteralPath $backup).Path -eq [IO.Path]::GetFullPath($backup)) {
        Remove-Item -LiteralPath $backup -Recurse -Force
    }
}
else {
    Move-Item -LiteralPath $staging -Destination $bundle
}
Write-Host "Mod bundle: $bundle"
if ($ModsPath) {
    Install-Bundle $bundle $ModsPath
}
