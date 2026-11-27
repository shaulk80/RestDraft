# Reading restored NuGet packages. Dot-sourced by build.ps1; used by LicenseCheck.ps1 and ThirdPartyNotices.ps1.

# The package folders of a restored project: the global packages folder first, then any fallback folders.
function Get-PackageFolders {
    param([string] $AssetsPath)

    $assets = [System.IO.File]::ReadAllText($AssetsPath) | ConvertFrom-Json
    return $assets.packageFolders.PSObject.Properties | ForEach-Object { $_.Name }
}

# The folder of one package: the first package folder that holds it. When none does, the place where it belongs in the
# first folder, so that the caller can name the missing path.
function Find-PackageDirectory {
    param([string[]] $PackageFolders, [string] $Id, [string] $Version)

    $relativePath = Join-Path $Id.ToLowerInvariant() $Version.ToLowerInvariant()
    foreach ($packageFolder in $PackageFolders) {
        $directory = Join-Path $packageFolder $relativePath
        if (Test-Path -LiteralPath $directory) {
            return $directory
        }
    }

    return Join-Path $PackageFolders[0] $relativePath
}

# License and copyright as the nuspec states them. Load() reads the encoding of the file, which Get-Content in
# Windows PowerShell 5.1 does not, so names with non-ASCII characters stay intact.
function Get-NuspecMetadata {
    param([string] $NuspecPath)

    $nuspec = New-Object System.Xml.XmlDocument
    $nuspec.Load($NuspecPath)
    $metadata = '/*[local-name()="package"]/*[local-name()="metadata"]'

    $licenseType = 'none'
    $licenseValue = ''
    $license = $nuspec.SelectSingleNode("$metadata/*[local-name()='license']")
    $licenseUrl = $nuspec.SelectSingleNode("$metadata/*[local-name()='licenseUrl']")
    if ($license) {
        $licenseType = $license.type
        $licenseValue = $license.InnerText
    }
    elseif ($licenseUrl) {
        $licenseType = 'url'
        $licenseValue = $licenseUrl.InnerText
    }

    $copyright = ''
    $copyrightNode = $nuspec.SelectSingleNode("$metadata/*[local-name()='copyright']")
    if ($copyrightNode) {
        $copyright = $copyrightNode.InnerText.Trim()
    }

    return [pscustomobject]@{ LicenseType = $licenseType; LicenseValue = $licenseValue; Copyright = $copyright }
}
