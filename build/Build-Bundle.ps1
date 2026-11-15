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

function Export-Pack([string] $Project, [string] $Destination, [string] $Version) {
    $log = Join-Path $buildDir "godot-g$Version.log"
    Write-Host "Exporting g$Version PCK"
    # Windows PowerShell reports native stderr as error records, including Godot warnings.
    & {
        $ErrorActionPreference = 'Continue'
        & $Godot --headless --editor --path $Project --export-pack BasicExport $Destination 2>&1
    } | ForEach-Object { $_.ToString() } | Set-Content -LiteralPath $log -Encoding UTF8
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $Destination -PathType Leaf)) {
        throw "Godot export failed. See $log"
    }
    if (Select-String -LiteralPath $log -Quiet -Pattern '^ERROR:|^System\.\w+Exception:') {
        throw "Godot reported export errors. See $log"
    }
    # A failed Spine importer can yield a PCK that appears to export successfully.
    if (!(Select-String -LiteralPath $log -Quiet -Pattern 'tashigan_2\.skel\.import') -or
        !(Select-String -LiteralPath $log -Quiet -Pattern 'tashigan_2\.atlas\.import')) {
        throw "Godot did not pack the Spine resources. Check the editor extension and $log"
    }
}

function Install-Bundle([string] $Source, [string] $ModsDirectory) {
    New-Item -ItemType Directory -Force -Path $ModsDirectory | Out-Null
    $mods = (Resolve-Path -LiteralPath $ModsDirectory).ProviderPath
    $destination = Join-Path $mods $modId

    # Stage next to mods so the game cannot discover the staged or backup manifest.
    $transaction = New-Item -ItemType Directory -Path (Join-Path (Split-Path -Parent $mods) ".$modId-install-$([guid]::NewGuid().ToString('N'))")
    $staged = Join-Path $transaction.FullName 'new'
    $previous = Join-Path $transaction.FullName 'previous'
    $installed = $false
    try {
        Copy-Item -LiteralPath $Source -Destination $staged -Recurse -Force
        if (Test-Path -LiteralPath $destination) {
            $installedMod = Get-Item -LiteralPath $destination -Force
            if (!$installedMod.PSIsContainer -or ($installedMod.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
                throw "Expected an ordinary mod directory at $destination."
            }

            $managedFiles = @('mod_manifest.json', "$modId.dll", "$modId.json", "$modId.pck", 'bin')
            Get-ChildItem -LiteralPath $destination -Force | Where-Object { $_.Name -notin $managedFiles } |
                Copy-Item -Destination $staged -Recurse -Force
            $installedBin = Join-Path $destination 'bin'
            if (Test-Path -LiteralPath $installedBin -PathType Container) {
                Get-ChildItem -LiteralPath $installedBin -Force |
                    Where-Object { $_.Name -notin "g$stableVersion", "g$betaVersion" } |
                    Copy-Item -Destination (Join-Path $staged 'bin') -Recurse -Force
            }
            [IO.Directory]::Move($destination, $previous)
        }

        try {
            [IO.Directory]::Move($staged, $destination)
            $installed = $true
        }
        catch {
            if (Test-Path -LiteralPath $previous) {
                [IO.Directory]::Move($previous, $destination)
            }
            throw
        }
    }
    finally {
        # Keep the backup available if both installation and restoration fail.
        if (($installed -or !(Test-Path -LiteralPath $previous)) -and
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
Export-Pack $stable (Join-Path $stableOut "$modId.pck") $stableVersion
$env:Sts2DataDir = $BetaDataDir
Export-Pack $root (Join-Path $betaOut "$modId.pck") $betaVersion

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
