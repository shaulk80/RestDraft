# License and notice files of a package. Dot-sourced by build.ps1, which runs it after publish and before packaging.
# Needs NuGetPackages.ps1.
#
# What ships is read from the published RestDraft.deps.json: every package with files, and the .NET runtime pack.
# License and copyright come from each nuspec, the license text from the package or from a standard text in
# scripts/licenses, and scripts/licenses/package-notices.json adds what the packages do not say (see the README there).
# Every problem is collected, so one run names all of them.

$projectLicenseFiles = @('LICENSE', 'NOTICE')
$noticesFileName = 'third-party-notices.txt'
$licenseFilePattern = '^(license|licence|copying)(\.[a-z]+)?$'
$noticeFilePattern = '^(third-?party-?notices|notice)(\.[a-z]+)?$'

# The license files a package must hold that are not in the folder. Empty means all are there.
function Get-MissingLicenseFiles {
    param([string] $ProgramDir)

    foreach ($fileName in ($projectLicenseFiles + $noticesFileName)) {
        if (-not (Test-Path -LiteralPath (Join-Path $ProgramDir $fileName))) {
            $fileName
        }
    }
}

function Get-ShippedComponents {
    param([string] $DepsPath)

    $deps = [System.IO.File]::ReadAllText($DepsPath) | ConvertFrom-Json
    $target = $deps.targets.($deps.runtimeTarget.name)
    $fileGroups = @('runtime', 'native', 'resources', 'runtimeTargets')
    foreach ($library in $target.PSObject.Properties) {
        $type = $deps.libraries.($library.Name).type
        $groups = @($library.Value.PSObject.Properties | Where-Object { $fileGroups -contains $_.Name })
        # The projects are RestDraft itself. A package without files is only a list of dependencies.
        if ($type -eq 'project' -or $groups.Count -eq 0) {
            continue
        }

        $name, $version = $library.Name -split '/', 2
        [pscustomobject]@{ Id = ($name -replace '^runtimepack\.', ''); Version = $version; Type = $type }
    }
}

function Read-PackageAdditions {
    param([string] $Path)

    $additions = @{}
    $data = [System.IO.File]::ReadAllText($Path) | ConvertFrom-Json
    foreach ($entry in $data.PSObject.Properties) {
        $additions[$entry.Name] = $entry.Value
    }

    return $additions
}

# Line ends are made equal, so that the same text from two sources is recognized as one.
function Read-LicenseText {
    param([string] $Path)

    return [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n").TrimStart("`n").TrimEnd()
}

function Get-StandardLicenseText {
    param([string] $LicensesDir, [string] $License, [string] $Copyright)

    $templatePath = Join-Path $LicensesDir "$License.txt"
    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
        return $null
    }

    return (Read-LicenseText -Path $templatePath).Replace('{copyright}', $Copyright)
}

function Get-MatchingFileTexts {
    param([string] $Directory, [string] $Pattern)

    Get-ChildItem -LiteralPath $Directory -File |
        Where-Object { $_.Name -match $Pattern } |
        Sort-Object Name |
        ForEach-Object { Read-LicenseText -Path $_.FullName }
}

# The license text the package carries (named in the nuspec, or a license file beside it), else the standard text.
function Get-PackageLicenseTexts {
    param([string] $Directory, [pscustomobject] $Metadata, [string] $License, [string] $Copyright, [string] $LicensesDir)

    if ($Metadata.LicenseType -eq 'file') {
        $namedFile = Join-Path $Directory $Metadata.LicenseValue
        if (Test-Path -LiteralPath $namedFile -PathType Leaf) {
            return Read-LicenseText -Path $namedFile
        }
    }

    $carried = @(Get-MatchingFileTexts -Directory $Directory -Pattern $licenseFilePattern)
    if ($carried.Count -gt 0) {
        return $carried
    }

    return Get-StandardLicenseText -LicensesDir $LicensesDir -License $License -Copyright $Copyright
}

# A component that is bundled inside a package, as package-notices.json describes it.
function Get-IncludedComponent {
    param([pscustomobject] $Entry, [string] $OwnerKey, [string] $LicensesDir, [System.Collections.IList] $Problems)

    $subject = "$OwnerKey, included '$($Entry.name)'"
    foreach ($field in @('name', 'license', 'copyright')) {
        if (-not $Entry.$field) {
            [void] $Problems.Add("${subject}: package-notices.json gives no $field.")
        }
    }

    $text = $null
    if ($Entry.textFile) {
        $textPath = Join-Path $LicensesDir $Entry.textFile
        if (Test-Path -LiteralPath $textPath -PathType Leaf) {
            $text = Read-LicenseText -Path $textPath
        }
    }
    else {
        $text = Get-StandardLicenseText -LicensesDir $LicensesDir -License $Entry.license -Copyright $Entry.copyright
    }

    if (-not $text) {
        [void] $Problems.Add("${subject}: no license text. Name a textFile in package-notices.json, or add a standard text for '$($Entry.license)'.")
    }

    return [pscustomobject]@{
        Name         = $Entry.name
        License      = $Entry.license
        Copyright    = $Entry.copyright
        LicenseTexts = @($text)
        NoticeTexts  = @()
        Includes     = @()
    }
}

function Get-PackageComponent {
    param(
        [pscustomobject] $Package,
        [string[]] $PackageFolders,
        [hashtable] $Additions,
        [hashtable] $ReviewedLicenses,
        [string] $LicensesDir,
        [System.Collections.IList] $Problems
    )

    $key = "$($Package.Id)/$($Package.Version)"
    if (@('package', 'runtimepack') -notcontains $Package.Type) {
        [void] $Problems.Add("${key}: unknown kind of dependency '$($Package.Type)'.")
        return
    }

    $directory = Find-PackageDirectory -PackageFolders $PackageFolders -Id $Package.Id -Version $Package.Version
    $nuspecPath = Join-Path $directory "$($Package.Id.ToLowerInvariant()).nuspec"
    if (-not (Test-Path -LiteralPath $nuspecPath)) {
        [void] $Problems.Add("${key}: nuspec not found at $nuspecPath.")
        return
    }

    $metadata = Get-NuspecMetadata -NuspecPath $nuspecPath
    $addition = $Additions[$key]
    $copyright = $metadata.Copyright
    if ($addition.copyright) {
        $copyright = $addition.copyright
    }

    $license = $metadata.LicenseValue
    if ($ReviewedLicenses.ContainsKey($key)) {
        $license = $ReviewedLicenses[$key]
    }

    if (-not $copyright) {
        [void] $Problems.Add("${key}: no copyright. Add it to package-notices.json.")
    }

    $licenseTexts = @(Get-PackageLicenseTexts -Directory $directory -Metadata $metadata -License $license -Copyright $copyright -LicensesDir $LicensesDir |
        Where-Object { $_ })
    if ($licenseTexts.Count -eq 0) {
        [void] $Problems.Add("${key}: no license text. The package carries none and scripts/licenses has no standard text for '$license'.")
    }

    $includes = @(foreach ($entry in $addition.includes) {
        Get-IncludedComponent -Entry $entry -OwnerKey $key -LicensesDir $LicensesDir -Problems $Problems
    })

    return [pscustomobject]@{
        Name         = "$($Package.Id) $($Package.Version)"
        License      = $license
        Copyright    = $copyright
        LicenseTexts = $licenseTexts
        NoticeTexts  = @(Get-MatchingFileTexts -Directory $directory -Pattern $noticeFilePattern)
        Includes     = $includes
    }
}

# An entry for another version of a shipped package is a review that was not repeated after an upgrade. Without this
# check the notices of bundled code would disappear silently.
function Add-OutdatedAdditionProblems {
    param([pscustomobject[]] $Packages, [hashtable] $Additions, [System.Collections.IList] $Problems)

    foreach ($key in ($Additions.Keys | Sort-Object)) {
        $id, $version = $key -split '/', 2
        $shipped = @($Packages | Where-Object { $_.Id -eq $id })
        if ($shipped.Count -gt 0 -and $shipped[0].Version -ne $version) {
            [void] $Problems.Add("${key} in package-notices.json: version $($shipped[0].Version) ships now. Check the entry against that version and rename it.")
        }
    }
}

# Numbers each distinct text once, in the order of first use.
function Get-TextReferences {
    param([string[]] $Texts, [System.Collections.Specialized.OrderedDictionary] $Numbers)

    $references = foreach ($text in $Texts) {
        if (-not $Numbers.Contains($text)) {
            $Numbers.Add($text, $Numbers.Count + 1)
        }
        "[$($Numbers[$text])]"
    }

    return $references -join ' '
}

function Format-ComponentLines {
    param([pscustomobject] $Component, [string] $Indent, [System.Collections.Specialized.OrderedDictionary] $Numbers)

    "$Indent$($Component.Name)"
    "$Indent  License: $($Component.License)"
    "$Indent  Copyright: $($Component.Copyright)"
    "$Indent  License text: $(Get-TextReferences -Texts $Component.LicenseTexts -Numbers $Numbers)"
    if ($Component.NoticeTexts.Count -gt 0) {
        "$Indent  Notices: $(Get-TextReferences -Texts $Component.NoticeTexts -Numbers $Numbers)"
    }
    if ($Component.Includes.Count -gt 0) {
        "$Indent  Contains:"
        foreach ($included in $Component.Includes) {
            Format-ComponentLines -Component $included -Indent "$Indent    " -Numbers $Numbers
        }
    }
}

function Format-Notices {
    param([pscustomobject[]] $Components, [string] $Runtime)

    # Ordinal comparison: two texts that differ only in the case of a letter are two texts.
    $numbers = New-Object System.Collections.Specialized.OrderedDictionary ([System.StringComparer]::Ordinal)
    $rule = '=' * 78
    $lines = @(
        "Third-party notices of the RestDraft package for $Runtime",
        '',
        'RestDraft is licensed under the Apache License 2.0: see the files LICENSE and NOTICE beside this one.',
        'The package also contains the components below. Each one remains subject to its own license.',
        'The numbers point to the texts after the list. A text that several components share appears once.',
        '',
        $rule,
        'Components',
        $rule
    )
    foreach ($component in $Components) {
        $lines += ''
        $lines += Format-ComponentLines -Component $component -Indent '' -Numbers $numbers
    }

    $lines += '', $rule, 'License texts and notices', $rule
    foreach ($entry in $numbers.GetEnumerator()) {
        $lines += '', ('-' * 78), "[$($entry.Value)]", ('-' * 78), '', $entry.Key
    }

    return ($lines -join "`n") + "`n"
}

# Copies LICENSE and NOTICE into the published folder and writes third-party-notices.txt there. Fails the build, naming
# every problem, when a shipped component has no copyright or no license text.
function Add-LicenseFiles {
    param(
        [string] $Root,
        [string] $PublishDir,
        [string] $Runtime,
        [string] $AssetsPath,
        [hashtable] $ReviewedLicenses
    )

    $licensesDir = Join-Path $Root 'scripts/licenses'
    $packages = @(Get-ShippedComponents -DepsPath (Join-Path $PublishDir 'RestDraft.deps.json') | Sort-Object Id)
    $packageFolders = @(Get-PackageFolders -AssetsPath $AssetsPath)
    $additions = Read-PackageAdditions -Path (Join-Path $licensesDir 'package-notices.json')
    $problems = New-Object System.Collections.Generic.List[string]

    Add-OutdatedAdditionProblems -Packages $packages -Additions $additions -Problems $problems
    $components = @(foreach ($package in $packages) {
        Get-PackageComponent -Package $package -PackageFolders $packageFolders -Additions $additions `
            -ReviewedLicenses $ReviewedLicenses -LicensesDir $licensesDir -Problems $problems
    })
    if ($problems.Count -gt 0) {
        $problems | ForEach-Object { Write-Host "FAIL  $_" }
        throw "Third-party notices failed for ${Runtime} with $($problems.Count) problem(s), listed above. See scripts/licenses/README.md."
    }

    foreach ($fileName in $projectLicenseFiles) {
        Copy-Item -LiteralPath (Join-Path $Root $fileName) -Destination $PublishDir
    }

    $text = (Format-Notices -Components $components -Runtime $Runtime).Replace("`n", [System.Environment]::NewLine)
    [System.IO.File]::WriteAllText((Join-Path $PublishDir $noticesFileName), $text, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Third-party notices written for ${Runtime}: $($components.Count) components."
}
