# RestDraft: status

Updated: 2026-11-28. Keep this file short and current. The old milestone log is in `history/implementation-progress-M0-M11.txt`.

## Now

Version 0.4.0 is the latest published release (2026-11-28; Windows x64 and Linux x64, unsigned). It adds import of API specifications (Swagger 2.0, OpenAPI 3.0 and 3.1) from a file or a URL, search in the response, a rename on "New request", tracking of the active request in the tree, drag reordering of tabs, folders and requests, an app icon, license notices in the packages and several fixes (see `RELEASE_NOTES.md`; requirements and plan in `docs/new-features-requirements.md` and `docs/new-features-plan.md`). All planned milestones (M0 to M11) are done. New work starts from a pull request; see `CLAUDE.md` for the workflow.

**Folder package (0.3.0):** each package holds one folder `RestDraft-<runtime>/` with the executable, about 270 DLLs and native libraries, and nothing is unpacked to `%TEMP%\.net\RestDraft` (Linux: `~/.net/RestDraft`) any more (decision Q13 in `ARCHITECTURE_PLAN.md`). The zip is 52.0 MB against 47.9 MB for 0.2.0, the tarball 47.6 MB against 44.0 MB, so each is about 4 MB (8%) larger.

**Filter popup (0.3.0):** one popup for both sidebar tabs. A funnel button at the left of the top bar opens it (text, method, "Search in bodies"); it filters the tree or the history, whichever tab is shown, and each tab keeps its own filter. The search box and method list moved off the History tab (Restore stays). The tree is pruned from a copy, and open tabs are matched against all requests, so a filter never closes a tab. The popup has a Clear button (disabled while no filter is applied). Ctrl+F (also Edit > Filter...) opens it from anywhere but the response body and its search panel, including when the top bar is hidden from the View menu; then it hangs under the main menu.

**Import and export:** File > Import... and File > Export... for one collection as an Insomnia v5 YAML file (0.3.0, plan: `docs/import-export-plan.md`). Import also reads Swagger and OpenAPI files, and File > Import from URL... downloads one (0.4.0). The formats sit behind `ICollectionFormat`, so Insomnia v4 JSON or Postman are one more class. Import shows a preview tree with check boxes, a new or an existing target, an overwrite option, and warnings; overwritten requests keep their id and their open tabs reload (with a second question when a tab has unsaved edits). Dependencies: YamlDotNet 18.1.0 and Microsoft.OpenApi 3.11.1, both MIT (Q14 and Q16 in `ARCHITECTURE_PLAN.md`).

**Checked on the published 0.4.0 assets (2026-11-28):** `sha256sum -c` OK for both downloads; one top-level folder in each; in the tarball only `RestDraft` and `createdump` are executable. The Windows zip (Windows 10, with the three license files) and the Linux tarball (Ubuntu 24.04 under WSL2, `scripts/verify-release-wsl.sh`, 0 failures) each started from a fresh folder and created `data/app.db` and a log inside the program folder; nothing new was written to `%TEMP%\.net\RestDraft` or `~/.net/RestDraft`. The owner reported portability checklist sections 1 (Windows), 2 (accessibility) and 3 (Smart App Control) as verified on the release assets (2026-11-28); machines and per-step results were not recorded.

## Releases

| Version | Date | Assets | Notes |
|---|---|---|---|
| v0.4.0 | 2026-11-28 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. OpenAPI and Swagger import (file or URL), search in the response, drag reordering, app icon, license notices, fixes. |
| v0.3.0 | 2026-11-18 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Import and export (Insomnia v5 YAML), filter popup, folder package instead of a single file. |
| v0.2.0 | 2026-11-13 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Window state, tree icons, taller body editor, splitter fix. |
| v0.1.1 | 2026-10-31 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Linux added. |
| v0.1.0 | 2026-10-30 | `RestDraft-win-x64.zip`, `SHA256SUMS.txt` | Windows only, built on the maintainer's machine. |

v0.4.0 assets: zip `18a8dc0d2dc63aadd27f774215786acba176aeb778024c3159f71337e56baee1`, tarball `d188e11a1339f549d7252238f93ceea0b23354d7d31a99de229d0d523765e265`.

v0.3.0 assets: zip `3920cd2d7fac1d3c2c8ffa788acade22e7d293e419a38cd3c3ec8f2fa704a09c`, tarball `9aa406036306702e07d8d682235f0bfe6dafa657aef6a564c9d7ea8281ac81c1`.

v0.2.0 assets: zip `547e206fca071f2a2ed2a2743f7f644e44f69d41750e66365f2e8ed8a6dded01`, tarball `7fd5d498766d76e6e190d941576a62ced7a0046103d0890dc0e3dcaf58012c75`.

v0.1.1 assets: zip `df389225b6bc39757e006cae2ca0f6761f9ce0019e96f7ae1f7f8a3f7d98b88b`, tarball `9d2c2b819fcbe7b0f406cc882c0ff3c961c7ba46ec936bfa7952882064df0cfd`.

## Open

- **Not checked in 0.4.0:** Import from URL against running swagger-ui-express, springdoc, FastAPI and Redoc services (samples in tests only); the tree delete question in the app after the fix of the import question. No headless test runs the whole import flow, because the dialog services find the main window through a static locator.
- **Known in 0.4.0:** on a page with several specifications the remembered base URL is offered for any of them. A crafted file with thousands of chained references is slow to read (10,000 took 77 s in the library).
- **Long unbroken line (found 2026-11-27, older than 0.4.0):** the editor wraps a line without spaces very slowly, with or without highlighting. A 200,000-character line took 25 s and 400,000 characters more than 90 s (headless measurement). A minified response without spaces can freeze the response view. A selection far down a very long line is slow too, even with spaces: about 3 s at the end of a 895,000-character line (off-screen measurement), because AvaloniaEdit computes its place slowly. Escape in the response search selects a match, so it shows this (2.7 s in the published build); the search highlights do not use that computation.
- **License questions still open:** Avalonia contains MS-PL code (listed in the notices). The libraries compiled into ANGLE besides xxHash were not verified. The notices cover code copied into a package's sources only where it was found. Releases up to 0.3.0 have no notice files and contain TextMateSharp, removed because it appears to be derived from an EPL-2.0 project (`ARCHITECTURE_PLAN.md`, Q15).
- **Code signing:** SignPath Foundation declined (2026-10, not enough public visibility; a paid subscription was declined by the owner). Reapply when the project has stars, forks, contributors and external mentions. If approved: SignPath GitHub Action in CI, MFA on the SignPath account, version without the `+<hash>` suffix, README policy wording (see the archive, M10).
- **Backlog:** the known limitations in `RELEASE_NOTES.md` (0.4.0 and 0.1.0 sections), and anything new. No issues are filed yet; GitHub Issues is the place for them.

## How things are set up

- **CI:** `ci.yml` has a Windows job and a Linux job (`ubuntu-24.04`); both run `scripts/build.ps1`, upload their package from `main` and from tags. `release.yml` builds both from a `v*` tag and drafts the release (dry run: manual run from `main`).
- **Branch protection:** rulesets `protect-main` (pull request, both checks required, up to date, no force push or deletion; admin bypass) and `protect-release-tags` (`v*`, no deletion or force push).
- **Build policy:** no `TreatWarningsAsErrors`. Nullable warnings and the rules listed in `.editorconfig` fail the build; other analyzer findings are warnings. Reason: the Linux job takes the newest 10.0.x SDK, and its new rule CA2027 broke the build of unchanged code (2026-11-13).
- **Coverage:** a warning below 80% for Domain and Application; never fails a build.
- **License notices:** `build.ps1` puts `LICENSE`, `NOTICE` and `third-party-notices.txt` into each package and fails when a shipped component has no copyright or license text, or when a package with an entry in `scripts/licenses/package-notices.json` changes version.
- **App icon:** after a change to `src/RestDraft.UI/Assets/app-icon.svg`, `dotnet run scripts/icon/MakeIcon.cs` makes the `.ico` and `.png`.
- **Verified:** the Windows build (portability checklist section 1), accessibility, Smart App Control (section 3), and Linux on Ubuntu 24.04 under WSL2 (section 5, 13 checks passed).
- **Verified for 0.3.0 by hand (2026-11-24):** Smart App Control and SmartScreen with the folder package and its DLLs; the import and export file dialogs on Windows and on Linux (Ubuntu 24.04 under WSL2, not a full Linux desktop); the filter popup on Linux (same WSL2 setup); an exported file opened in Insomnia itself. All worked as expected.
- **Not verified:** a full Linux desktop and other distributions; signing.

## Decisions worth remembering

- Data stays next to the executable; no installer.
- Linux packages are `.tar.gz` built on Linux (a zip made on Windows cannot keep the executable bit).
- The package is a folder of files, not a single exe (2026-11-18, Q13). The checksum covers the zip or tarball. Signing, if it comes, must cover the executable and the project's DLLs.
- Testing on WSL: a slow first window is WSLg warming up, not the app; see `CLAUDE.md` (Gotchas) and the portability checklist section 5.
- Linux limitations and the unsigned warnings are in `RELEASE_NOTES.md` and the README.
