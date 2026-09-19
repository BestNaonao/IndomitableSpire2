Add-Type -AssemblyName System.Drawing

$workshopDir = $PSScriptRoot
$buildDir = Join-Path $workshopDir '.build'
$renderDir = Join-Path $workshopDir 'renders'
$outputDir = Join-Path $workshopDir 'images'

function New-Canvas([int]$Width, [int]$Height) {
    $bitmap = [System.Drawing.Bitmap]::new(
        $Width,
        $Height,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    return @($bitmap, $graphics)
}

function Draw-CoverImage(
    [System.Drawing.Graphics]$Graphics,
    [System.Drawing.Image]$Image,
    [int]$Width,
    [int]$Height) {
    $targetAspect = $Width / [double]$Height
    $sourceAspect = $Image.Width / [double]$Image.Height
    if ($sourceAspect -gt $targetAspect) {
        $sourceHeight = $Image.Height
        $sourceWidth = [int][Math]::Round($sourceHeight * $targetAspect)
        $sourceX = [int](($Image.Width - $sourceWidth) / 2)
        $sourceY = 0
    }
    else {
        $sourceWidth = $Image.Width
        $sourceHeight = [int][Math]::Round($sourceWidth / $targetAspect)
        $sourceX = 0
        $sourceY = [int](($Image.Height - $sourceHeight) / 2)
    }
    $destination = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
    $source = [System.Drawing.Rectangle]::new($sourceX, $sourceY, $sourceWidth, $sourceHeight)
    $Graphics.DrawImage($Image, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Character(
    [System.Drawing.Graphics]$Graphics,
    [System.Drawing.Image]$Image,
    [int]$X,
    [int]$Bottom,
    [int]$Height) {
    # Every capture uses the same 600x600 transparent viewport. This crop keeps
    # all rigging and clothing while removing most unused border pixels.
    $source = [System.Drawing.Rectangle]::new(35, 5, 530, 570)
    $width = [int][Math]::Round($Height * $source.Width / [double]$source.Height)
    $destination = [System.Drawing.Rectangle]::new($X, $Bottom - $Height, $width, $Height)
    $Graphics.DrawImage($Image, $destination, $source, [System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-OutlinedText(
    [System.Drawing.Graphics]$Graphics,
    [string]$Text,
    [string]$FontFamily,
    [float]$FontSize,
    [System.Drawing.RectangleF]$Bounds,
    [System.Drawing.Color]$Fill,
    [System.Drawing.Color]$Outline,
    [float]$OutlineWidth) {
    $family = [System.Drawing.FontFamily]::new($FontFamily)
    $format = [System.Drawing.StringFormat]::new()
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddString(
        $Text,
        $family,
        [int][System.Drawing.FontStyle]::Bold,
        $FontSize,
        $Bounds,
        $format)
    if ($OutlineWidth -gt 0) {
        $pen = [System.Drawing.Pen]::new($Outline, $OutlineWidth)
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $Graphics.DrawPath($pen, $path)
        $pen.Dispose()
    }
    $brush = [System.Drawing.SolidBrush]::new($Fill)
    $Graphics.FillPath($brush, $path)
    $brush.Dispose()
    $path.Dispose()
    $format.Dispose()
    $family.Dispose()
}

function Save-Hero {
    $parts = New-Canvas 1200 450
    $bitmap = $parts[0]
    $graphics = $parts[1]
    $base = [System.Drawing.Image]::FromFile((Join-Path $buildDir 'hero_base.png'))
    Draw-CoverImage $graphics $base 1200 450

    $navy = [System.Drawing.Color]::FromArgb(255, 24, 53, 91)
    $gold = [System.Drawing.Color]::FromArgb(255, 214, 180, 101)
    $white = [System.Drawing.Color]::FromArgb(245, 248, 252, 255)
    Draw-OutlinedText $graphics 'AZUR LANE × SLAY THE SPIRE 2' 'Impact' 54 `
        ([System.Drawing.RectangleF]::new(155, 24, 890, 66)) $navy $white 7
    Draw-OutlinedText $graphics 'TASHKENT CHARACTER MOD' 'Arial' 29 `
        ([System.Drawing.RectangleF]::new(245, 82, 710, 42)) $navy $gold 2
    Draw-OutlinedText $graphics '塔什干角色模组' 'Microsoft YaHei UI' 22 `
        ([System.Drawing.RectangleF]::new(350, 116, 500, 38)) $navy $white 2

    $skins = 1..4 | ForEach-Object {
        [System.Drawing.Image]::FromFile((Join-Path $renderDir "tashkent_spine_$_.png"))
    }
    # Keep all four outfits equally prominent. Their centers form two mirrored
    # pairs around the middle of the banner, leaving the central vista readable.
    Draw-Character $graphics $skins[1] 58 445 220
    Draw-Character $graphics $skins[2] 272 445 220
    Draw-Character $graphics $skins[0] 723 445 220
    Draw-Character $graphics $skins[3] 937 445 220

    $path = Join-Path $outputDir 'tashkent_workshop_hero.png'
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $skins | ForEach-Object { $_.Dispose() }
    $base.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

function Save-Section(
    [string]$BaseName,
    [string]$OutputName,
    [string]$EnglishTitle,
    [string]$ChineseTitle,
    [int]$SkinNumber) {
    $parts = New-Canvas 1200 320
    $bitmap = $parts[0]
    $graphics = $parts[1]
    $base = [System.Drawing.Image]::FromFile((Join-Path $buildDir $BaseName))
    Draw-CoverImage $graphics $base 1200 320

    $skin = [System.Drawing.Image]::FromFile(
        (Join-Path $renderDir "tashkent_spine_$SkinNumber.png"))
    Draw-Character $graphics $skin 18 296 252

    $navy = [System.Drawing.Color]::FromArgb(255, 24, 53, 91)
    $white = [System.Drawing.Color]::FromArgb(250, 250, 252, 255)
    Draw-OutlinedText $graphics $EnglishTitle 'Impact' 49 `
        ([System.Drawing.RectangleF]::new(260, 72, 820, 72)) $navy $white 6
    Draw-OutlinedText $graphics $ChineseTitle 'Microsoft YaHei UI' 27 `
        ([System.Drawing.RectangleF]::new(360, 148, 620, 52)) $navy $white 2

    $path = Join-Path $outputDir $OutputName
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $skin.Dispose()
    $base.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
Save-Hero
Save-Section 'content_base.png' 'tashkent_workshop_content.png' 'MOD CONTENT' '模组内容' 3
Save-Section 'mechanics_base.png' 'tashkent_workshop_mechanics.png' 'CORE MECHANICS' '核心机制' 1
Save-Section 'extras_base.png' 'tashkent_workshop_extras.png' 'EXTRA FEATURES' '额外功能' 4

Get-ChildItem -LiteralPath $outputDir -Filter 'tashkent_workshop_*.png' |
    Select-Object Name, Length, LastWriteTime
