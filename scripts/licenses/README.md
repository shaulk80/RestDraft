# License texts of the packages

`ThirdPartyNotices.ps1` (run by `build.ps1`) writes `third-party-notices.txt` into every package. This folder holds what the NuGet packages do not provide themselves.

| File | Content |
|---|---|
| `MIT.txt`, `Apache-2.0.txt`, `BSD-3-Clause.txt` | Standard texts, named by SPDX identifier, for packages that carry no license file. `{copyright}` is filled with the copyright of the component. |
| `package-notices.json` | What a package does not say, keyed by `Id/Version`. |
| `bundled/` | License texts of components bundled inside a package. |

A package gets its license text from, in this order: the license file its nuspec names, a license file in the package, the standard text for its license expression. Its notices file, if it has one, is added too.

## `package-notices.json`

- `copyright`: replaces the copyright of the nuspec. Needed when the nuspec declares none.
- `includes`: components bundled inside the package. Each has `name`, `license` and `copyright`, and either `textFile` (relative to this folder) or a `license` that has a standard text here.

## When the build fails

The build names every problem. The usual ones:

- **No copyright:** read the license file or the source headers of the project at the commit the nuspec names, and add `copyright`.
- **No license text:** add the standard text for the license, or a file in `bundled/`.
- **A new version of a package that has an entry:** repeat the check below against the new version, then rename the key. This is on purpose: the build cannot see code bundled inside a package.

## What was checked by hand

Checked on 2026-11-27. Repeat at every upgrade of the package.

| Package | What is bundled, and how it was found |
|---|---|
| Avalonia 12.1.4 | `NOTICE.md` of the repository (code from other projects) is not in the package: `bundled/avalonia-notice.txt`. Resources of the DLLs: the Roboto Light font in `Avalonia.Dialogs.dll` (name table of the font) and a web page with React, ReactDOM, Scheduler and object-assign in `Avalonia.DesignerSupport.dll` (its source map). |
| Avalonia.Angle.Windows.Natives 2.1.27548.20260419 | The package carries the ANGLE license. xxHash is compiled in (`src/common/third_party/xxhash` of the Avalonia fork). Other libraries of the ANGLE tree were not verified against the binaries. |
| Avalonia.AvaloniaEdit 12.0.0 | Ported from AvalonEdit: the source files keep its copyright header. |
| SQLitePCLRaw.lib.e_sqlite3 2.1.13 | The native library is SQLite, which is public domain. |
| SkiaSharp and HarfBuzzSharp native assets | Their notices file covers the native libraries, so there is no entry. |

The checks looked at the files of a package, the resources of its DLLs and what its project says about itself. They did not search the source trees for code copied from other projects.
