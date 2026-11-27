# RestDraft

<img src="src/RestDraft.UI/Assets/app-icon.png" alt="RestDraft icon" width="96" height="96">

RestDraft - a REST API client. A desktop application for Windows and Linux (x64). Build requests, send them, and keep them in collections. Everything is stored on your machine, in a folder next to the program.

## Features

- Requests with any method, URL, query parameters, headers, body and authorization. Parameters and headers are rows that can be switched on and off. Bodies can be raw text (JSON and XML are highlighted and can be formatted), form-urlencoded, or multipart with files.
- Authorization: Basic, Bearer token, and API key (in a header or in the query).
- Responses show status, timing, size, headers, and a body that can be viewed pretty-printed or raw. Large bodies are kept on disk, and the first 1 MB is shown.
- Search in the response: Ctrl+F in the response body opens a panel above it, with Match case and Whole word. All matches are highlighted, F3 and Shift+F3 step through them, and Escape closes the panel with the current match selected. Each tab keeps its own search. Only the shown text is searched, so for a large body only the preview.
- Collections with folders and subfolders. Drag and drop to move items. A new request is named in the tree as soon as it is created, and Enter or Escape then moves on to its URL.
- View > Track active request selects the request of the active tab in the Collections tree, opening the folders above it. It is off by default and remembered.
- Filter for the Collections tree and for the History, from the funnel button at the left of the top bar: text (names and URLs), method, and optionally the request bodies. Each tab keeps its own filter, and the funnel is filled while one is applied.
- History of every request that was sent, with search and restore. History keeps the URL as you typed it, with `{{placeholders}}`, so secret values never reach it.
- Environments with `{{variables}}`. Variables can be marked secret. Secrets are encrypted with a master password. In the Environments window the names are edited in the list, and one environment is marked as the current one.
- Import and export of a collection as an Insomnia v5 YAML file (File > Import... and File > Export...). Import shows a preview with a check box on every folder and request, goes into a new or an existing collection, and can overwrite requests that have the same name. Export writes the whole collection or the ticked part. Insomnia v4 JSON and Postman are not supported yet. What RestDraft does not keep (descriptions, scripts, settings, cookies, environments, folder-level headers and authentication, OAuth 2 and other authentication types) is listed as warnings. Exported files hold Basic auth passwords and tokens in plain text.
- Import of an API specification: Swagger 2.0 and OpenAPI 3.0 and 3.1 files, in JSON or YAML, through the same File > Import.... Each tag becomes a folder and each operation a request, with its parameters, a body from the specification's example or a sample made from the schema, and Basic, Bearer or API key authentication that uses variables such as `{{token}}`. The preview has a Base URL box, filled from the specification; for a file that names no server it holds `{{base_url}}`. To update a collection later, import the new file into it with "Overwrite existing items", which also replaces your edits to those requests. Descriptions, response definitions, callbacks, webhooks and references to other files are not imported, and there is no export to OpenAPI.
- Several request tabs. Open tabs and unsaved edits come back after a restart.
- Settings for the request timeout, redirects, SSL certificate checks, and a proxy.
- Light and dark themes. The Theme setting is System (follows the operating system, the default), Light or Dark.
- A main menu (File, Edit, View, Tools, Help) with the shortcuts shown next to each item. Settings are in the File menu. Lock or unlock secrets and manage environments are in the Tools menu.
- Three panels: Collections and History, the request, and the response on the right. Drag the splitters to change the widths. The widths are remembered. The View menu hides the Collections panel, the Response panel and the bar with the secrets and the environment. What is shown is remembered.
- A setting to open a request with a single click in the Collections tree. It is off by default, which keeps the double-click.

## Install and run

1. Download `RestDraft-win-x64.zip` and `SHA256SUMS.txt` from the release page.
2. Check the download. In PowerShell, in the folder with both files:

   ```powershell
   (Get-FileHash .\RestDraft-win-x64.zip -Algorithm SHA256).Hash.ToLower()
   Get-Content .\SHA256SUMS.txt
   ```

   The two hashes must be the same.
3. Unzip the file anywhere you can write to. It creates one folder, `RestDraft-win-x64`. Run `RestDraft.exe` from it and keep the other files in that folder next to the executable. There is no installer. It needs no .NET installation.

### Uninstall

There is no installer, so to uninstall, delete the program folder. It includes the `data` folder, so copy `data` somewhere first if you want to keep your collections, history and environments.

All your data stays in the `data` folder, and the program writes nothing outside its folder. Versions up to 0.2.0 were a single file and unpacked their native libraries (graphics, fonts, SQLite) into `%TEMP%\.net\RestDraft` on the first run. If you used one of them, you can delete that folder.

### Windows warnings

The release is **not code-signed yet**. Windows may react in one of these ways:

- **SmartScreen** shows "Windows protected your PC". Choose *More info*, then *Run anyway*, after you have checked the hash above.
- **Smart App Control** (Windows 11, when turned on) can block unsigned programs without an option to run them. If that happens, the program cannot be started on that machine until Smart App Control is turned off or the program is signed. Smart App Control cannot be turned on again once it is off.

### Linux

Since 0.1.1 the release has a Linux x64 package, `RestDraft-linux-x64.tar.gz`, next to the Windows zip. It passed the checks on Ubuntu 24.04 (under WSL2); other distributions and a full Linux desktop are untested. To use it:

1. Check the download, in the folder with both files (`--ignore-missing` skips the Windows zip): `sha256sum -c SHA256SUMS.txt --ignore-missing`.
2. Install the system libraries the program needs (names from Ubuntu 24.04; other distributions use similar names):

   ```bash
   sudo apt install -y libicu74 libfontconfig1 fonts-dejavu-core libx11-6 libice6 libsm6 libxext6 libxrandr2 libxi6 libxcursor1 libgl1
   ```

3. Extract and run. The archive holds one folder, `RestDraft-linux-x64`, and keeps the executable permission, so no `chmod` is needed:

   ```bash
   tar -xzf RestDraft-linux-x64.tar.gz
   ./RestDraft-linux-x64/RestDraft
   ```

The `data` folder, `--data-dir` and `RESTDRAFT_DATA_DIR` work as described under [Your data](#your-data), with `/` in paths. A startup error is printed in the terminal, because there is no message box on Linux.

To uninstall, delete the program folder (copy `data` first if you want to keep it). Versions up to 0.2.0 also unpacked their native libraries into `~/.net/RestDraft`; delete that folder if it exists.

On WSL2 with WSLg, closing a window or a popup can lag by about a second, and the first window after WSL starts can take a minute or more to appear while WSLg connects to Windows. Both come from WSLg, not from the program.

## Your data

All data is kept in a `data` folder:

| Path | Content |
|---|---|
| `data/app.db` | Collections, history, environments, settings and open tabs (SQLite) |
| `data/logs/` | Log files |
| `data/temp/` | Large response bodies saved by the app. Files are removed when their history entry expires |

By default `data` is created next to `RestDraft.exe`. To use another folder, use either of these. The command-line argument wins when both are set.

```powershell
.\RestDraft.exe --data-dir D:\RestDraftData
$env:RESTDRAFT_DATA_DIR = 'D:\RestDraftData'; .\RestDraft.exe
```

To move the app to another computer or back it up, copy the whole folder, including `data`.

### Secrets and the master password

Secret variables are encrypted with AES-256-GCM, with a key derived from your master password (PBKDF2, 600,000 iterations). The password itself is never stored. **There is no way to recover a forgotten master password.** The secrets then have to be entered again.

Secrets are unlocked for the running session only. Use *Lock* in the bar under the menu, or *Lock secrets* in the Tools menu, to lock them sooner.

## Privacy

RestDraft does not collect, store remotely or send any information about you or your use of the program. It has no telemetry, no analytics, no crash reporting and no update check.

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it. The only network traffic is the requests you choose to send, and they go only to the addresses you enter (through the proxy you configure, if any). Collections, history, environments, settings and secrets stay in the local `data` folder described above. None of the third-party components used by the program sends data on its own.

## Keyboard

| Keys | Action |
|---|---|
| Ctrl+Enter | Send the request in the active tab |
| Ctrl+S | Save the active tab |
| Ctrl+Shift+S | Save all changed tabs. A tab that is not in a collection yet asks for a folder and a name. Cancel stops the rest |
| Ctrl+T | Open a new tab |
| Ctrl+F4 | Close the active tab |
| Ctrl+F | In the response body or its search panel: search the response. Anywhere else: open the filter popup of the Collections or History tab, whichever is shown, even when the bar at the top is hidden |
| F3 / Shift+F3 | In the response body or its search panel: next / previous match. In the body with the panel closed, it opens the panel with the text searched last |
| Enter / Shift+Enter | In the search box of the response: next / previous match |
| Esc | While the search panel of the response is open, in the panel or the response body: close it and select the current match |
| Ctrl+Tab / Ctrl+Shift+Tab | Switch the request tabs in the order they were last used. Keep Ctrl down to go further back. Other tabs (Collections, History, Body, Params, and so on) are not affected |
| F2 / Delete / Enter | In the Collections tree: rename, delete, or open the selected item |
| Shift+F10 or the Menu key | In the Collections tree: open the context menu. Esc closes it |
| Esc / Shift+Esc | In a text editor (request or response body): move focus to the next / previous control. Tab types a tab character there. In the response body with the search panel open, Esc closes the panel first |
| Esc / Enter | In dialogs: cancel / confirm (Enter does not confirm a delete) |

## Known limitations

See [RELEASE_NOTES.md](RELEASE_NOTES.md).

## Build from source

You need the .NET 10 SDK.

```powershell
.\scripts\build.ps1
```

The Linux package is built on a Linux host (the CI Linux job does it): `pwsh ./scripts/build.ps1 -Runtimes linux-x64`, which needs `xvfb` for its smoke test. On Windows that runtime is skipped.

The script restores packages, checks the licenses of all dependencies, builds (nullable warnings and the rules listed in `.editorconfig` fail the build; other analyzer findings are warnings), runs the tests and reports coverage (a warning below 80% for the domain and application layers), publishes `win-x64` self-contained (the executable, its DLLs and native libraries in one folder, not a single file), adds the license and notice files to that folder, zips it to `dist/RestDraft-win-x64.zip`, writes `dist/SHA256SUMS.txt`, and checks the unzipped package: the license files are in it, and the program started from it creates its `data` folder. Close the sample API before running it if you started it.

Project layout and design are described in [ARCHITECTURE_PLAN.md](docs/ARCHITECTURE_PLAN.md) and [implementation-plan.md](docs/implementation-plan.md). The current state is in [STATUS.md](docs/STATUS.md).

## Code signing policy

Releases are not code-signed. The application to [SignPath Foundation](https://signpath.org) for free code signing through [SignPath.io](https://signpath.io) was declined for now, because the project does not yet have enough public visibility. The project will apply again when it has. If a certificate is issued, this is the policy that will apply.

Only binaries built from the source in this repository, by a GitHub Actions workflow, will be submitted for signing. Every signing request will be approved manually.

| Role | Member |
|---|---|
| Authors (commit access) | Shaul Kipod ([@shaulk80](https://github.com/shaulk80)) |
| Reviewers | Shaul Kipod ([@shaulk80](https://github.com/shaulk80)) |
| Approvers (approve each signing request) | Shaul Kipod ([@shaulk80](https://github.com/shaulk80)) |

Privacy: see the [Privacy](#privacy) section above.

> Status: unsigned. See [Windows warnings](#windows-warnings) for what Windows may show, and verify each download against `SHA256SUMS.txt`. When a certificate is issued and a signed release is published, rewrite the first paragraph to "Free code signing provided by SignPath.io, certificate by SignPath Foundation" and delete this note.

## License

Apache License 2.0. See [LICENSE](LICENSE) and [NOTICE](NOTICE). Dependencies are checked by the build to carry MIT, Apache-2.0, BSD, ISC or public-domain licenses.

Each package holds `LICENSE`, `NOTICE` and `third-party-notices.txt`, which lists the components in the package with their copyright and license texts, including components bundled inside them under other licenses. The build fails when a shipped component has no copyright or no license text (see [scripts/licenses](scripts/licenses/README.md)).
