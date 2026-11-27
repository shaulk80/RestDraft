# RestDraft: status

Updated: 2026-11-27. Keep this file short and current. The old milestone log is in `history/implementation-progress-M0-M11.txt`.

## Now

Version 0.3.0 is the latest published release (2026-11-18; Windows x64 and Linux x64, unsigned). It adds import and export of collections (Insomnia v5 YAML), a filter popup for the Collections tree and the History, and a package that is a folder of files instead of a single executable (see `RELEASE_NOTES.md`). All planned milestones (M0 to M11) are done. New work starts from a pull request; see `CLAUDE.md` for the workflow.

**Folder package (0.3.0):** each package holds one folder `RestDraft-<runtime>/` with the executable, about 270 DLLs and native libraries, and nothing is unpacked to `%TEMP%\.net\RestDraft` (Linux: `~/.net/RestDraft`) any more (decision Q13 in `ARCHITECTURE_PLAN.md`). The zip is 52.0 MB against 47.9 MB for 0.2.0, the tarball 47.6 MB against 44.0 MB, so each is about 4 MB (8%) larger.

**Filter popup (0.3.0):** one popup for both sidebar tabs. A funnel button at the left of the top bar opens it (text, method, "Search in bodies"); it filters the tree or the history, whichever tab is shown, and each tab keeps its own filter. The search box and method list moved off the History tab (Restore stays). The tree is pruned from a copy, and open tabs are matched against all requests, so a filter never closes a tab. The popup has a Clear button (disabled while no filter is applied). Ctrl+F (also Edit > Filter...) opens it from anywhere, including when the top bar is hidden from the View menu; then it hangs under the main menu.

**Import and export (0.3.0):** File > Import... and File > Export... for one collection as an Insomnia v5 YAML file (plan: `docs/import-export-plan.md`, all of it implemented). The format sits behind `ICollectionFormat`, so Insomnia v4 JSON, Postman or OpenAPI are one more class. Import shows a preview tree with check boxes, a new or an existing target, an overwrite option, and warnings; overwritten requests keep their id and their open tabs reload (with a second question when a tab has unsaved edits). New dependency: YamlDotNet 18.1.0 (MIT, no transitive packages; decision Q14 in `ARCHITECTURE_PLAN.md`).

**Checked on the published 0.3.0 assets (2026-11-18):** `sha256sum -c` OK for both archives; one top-level folder in each; in the tarball only `RestDraft` and `createdump` are executable. The Windows zip and the Linux tarball (Ubuntu 24.04 under WSL2) each started from a fresh folder and created `data/app.db` and a log inside the program folder; nothing was written to `%TEMP%\.net` or `~/.net`.

## Releases

| Version | Date | Assets | Notes |
|---|---|---|---|
| v0.3.0 | 2026-11-18 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Import and export (Insomnia v5 YAML), filter popup, folder package instead of a single file. |
| v0.2.0 | 2026-11-13 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Window state, tree icons, taller body editor, splitter fix. |
| v0.1.1 | 2026-10-31 | `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz`, `SHA256SUMS.txt` | Built by CI from the tag. Linux added. |
| v0.1.0 | 2026-10-30 | `RestDraft-win-x64.zip`, `SHA256SUMS.txt` | Windows only, built on the maintainer's machine. |

v0.3.0 assets: zip `3920cd2d7fac1d3c2c8ffa788acade22e7d293e419a38cd3c3ec8f2fa704a09c`, tarball `9aa406036306702e07d8d682235f0bfe6dafa657aef6a564c9d7ea8281ac81c1`.

v0.2.0 assets: zip `547e206fca071f2a2ed2a2743f7f644e44f69d41750e66365f2e8ed8a6dded01`, tarball `7fd5d498766d76e6e190d941576a62ced7a0046103d0890dc0e3dcaf58012c75`.

v0.1.1 assets: zip `df389225b6bc39757e006cae2ca0f6761f9ce0019e96f7ae1f7f8a3f7d98b88b`, tarball `9d2c2b819fcbe7b0f406cc882c0ff3c961c7ba46ec936bfa7952882064df0cfd`.

## Open

- **Next features (2026-11-26):** app icon, tracking of the active request in the tree, import from a Swagger URL, rename on "New request", search in the response. Requirements are decided (`docs/new-features-requirements.md`); the plan (`docs/new-features-plan.md`, six pull requests) is approved. PRs 1 to 4 are merged and PR 5 is on its branch (below); PR 6 is not started.
- **OpenAPI and Swagger import (PR 5, 2026-11-27, branch `feat/openapi-import`, not merged):** File > Import... reads Swagger 2.0 and OpenAPI 3.0 and 3.1 (JSON, YAML); the preview has a Base URL box. New dependency Microsoft.OpenApi 3.11.1; our YAML to JSON converter stays (Q16 in `ARCHITECTURE_PLAN.md`). Checked: `build.ps1` on Windows (SDK 11), tests also on SDK 10.0.402; real specifications (Petstore, Stripe, GitHub, Kubernetes) read outside the app; in the published Windows build, driven by script: the file dialog filter, the Petstore 3.0 YAML preview, Import disabled with an empty base URL, the import, and one request with a body sent to a local echo server. Not checked: Linux, Overwrite with a specification by hand. Known: a crafted file with thousands of chained references is slow to read (10,000 took 77 s in the library).
- **Search in the response (PR 4, merged 2026-11-27 in #31, not released):** Ctrl+F in the response body opens a panel above it; Ctrl+F elsewhere still opens the filter. Checked: `build.ps1` on Windows (SDK 11), tests also on SDK 10.0.402; in the published Windows build, driven by script, light theme: the panel, highlights, F3 and Enter, Escape, no match, the filter from the URL box, and a pretty-printed 1 MB response searched for one letter. That run found highlights that took over 40 s to repaint far down a one-line response; fixed, and checked off-screen with Skia rendering (84 ms on a 895,000-character line; dark theme and narrow panel too). Not checked: the fixed build in a real window with such a line, Linux.
- **New request rename and tracking (PR 3, merged 2026-11-27 in #29, not released):** "New request" in the tree starts a rename and Enter or Escape moves on to the URL box; View > Track active request selects the active tab's request in the tree. Checked: `build.ps1` on Windows, tests also on SDK 10.0.402; in the published Windows build, driven by script: the toolbar button and context menu with single-click open on and off, clicking elsewhere, tracking switched on, a closed folder, a filter, a closed tab and a restart. The Linux CI job passed. Not checked: the two features by hand on Linux, real Ctrl+Tab key presses (headless test only).
- **App icon (PR 2, 2026-11-27, #28):** the executable, every window, the About window and the README show it. `dotnet run scripts/icon/MakeIcon.cs` makes the `.ico` and `.png` from `src/RestDraft.UI/Assets/app-icon.svg` after a change to the drawing. Checked: `build.ps1` on Windows and on Linux (Ubuntu 24.04 under WSL2); the title bar, About window and executable icon on Windows; the icon the window sets on Linux (read under Xvfb); then checked by eye by the owner (reported 2026-11-27).
- **License notices (PR 1, merged 2026-11-27 in #26):** `build.ps1` puts `LICENSE`, `NOTICE` and `third-party-notices.txt` into each package and fails when a shipped component has no copyright or license text, or when a package with an entry in `scripts/licenses/package-notices.json` changes version. The published 0.3.0 zip has none of the three files. Built and smoke-tested on Windows and on Linux (Ubuntu 24.04 under WSL2).
- **TextMateSharp removed (2026-11-27):** it declares MIT but appears to be derived from the EPL-2.0 project `eclipse/tm4e` (`ARCHITECTURE_PLAN.md`, Q15). JSON and XML highlighting is now AvaloniaEdit's engine with RestDraft's definitions; only the first 10,000 characters of a line are colored. Releases up to 0.3.0 contain the removed packages.
- **License questions still open:** Avalonia contains MS-PL code (listed in the notices). The libraries compiled into ANGLE besides xxHash were not verified. The notices cover code copied into a package's sources only where it was found.
- **Long unbroken line (found 2026-11-27, not new):** the editor wraps a line without spaces very slowly, with or without highlighting. A 200,000-character line took 25 s and 400,000 characters more than 90 s (headless measurement). A minified response without spaces can freeze the response view. A selection far down a very long line is slow too, even with spaces: about 3 s at the end of a 895,000-character line (off-screen measurement, 2026-11-27), because AvaloniaEdit computes its place slowly. Escape in the response search selects a match, so it shows this; the search highlights do not use that computation.
- **Code signing:** SignPath Foundation declined (2026-10, not enough public visibility; a paid subscription was declined by the owner). Reapply when the project has stars, forks, contributors and external mentions. If approved: SignPath GitHub Action in CI, MFA on the SignPath account, version without the `+<hash>` suffix, README policy wording (see the archive, M10).
- **Found in use of 0.3.0 (2026-11-24), plan in `docs/tree-and-tabs-fixes-plan.md`:** fixed on `main`, not released: numpad keys in the tree's rename box, tree rows under the thin scrollbar, indent of empty rows. Also on `main`, not released: reorder of tabs and folders by dragging, tried by hand with the mouse and working. A release with these fixes (`0.3.1` or `0.4.0`) is for the owner to decide.
- **Backlog:** the known limitations in `RELEASE_NOTES.md` (0.1.0 section), and anything new. No issues are filed yet; GitHub Issues is the place for them.

## How things are set up

- **CI:** `ci.yml` has a Windows job and a Linux job (`ubuntu-24.04`); both run `scripts/build.ps1`, upload their package from `main` and from tags. `release.yml` builds both from a `v*` tag and drafts the release (dry run: manual run from `main`).
- **Branch protection:** rulesets `protect-main` (pull request, both checks required, up to date, no force push or deletion; admin bypass) and `protect-release-tags` (`v*`, no deletion or force push).
- **Build policy:** no `TreatWarningsAsErrors`. Nullable warnings and the rules listed in `.editorconfig` fail the build; other analyzer findings are warnings. Reason: the Linux job takes the newest 10.0.x SDK, and its new rule CA2027 broke the build of unchanged code (2026-11-13).
- **Coverage:** a warning below 80% for Domain and Application; never fails a build.
- **Verified:** the Windows build (portability checklist section 1), accessibility, Smart App Control (section 3), and Linux on Ubuntu 24.04 under WSL2 (section 5, 13 checks passed).
- **Verified for 0.3.0 by hand (2026-11-24):** Smart App Control and SmartScreen with the folder package and its DLLs; the import and export file dialogs on Windows and on Linux (Ubuntu 24.04 under WSL2, not a full Linux desktop); the filter popup on Linux (same WSL2 setup); an exported file opened in Insomnia itself. All worked as expected.
- **Not verified:** a full Linux desktop and other distributions; signing.

## Decisions worth remembering

- Data stays next to the executable; no installer.
- Linux packages are `.tar.gz` built on Linux (a zip made on Windows cannot keep the executable bit).
- The package is a folder of files, not a single exe (2026-11-18, Q13). The checksum covers the zip or tarball. Signing, if it comes, must cover the executable and the project's DLLs.
- Testing on WSL: a slow first window is WSLg warming up, not the app; see `CLAUDE.md` (Gotchas) and the portability checklist section 5.
- Linux limitations and the unsigned warnings are in `RELEASE_NOTES.md` and the README.
