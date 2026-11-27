# Release notes

## Unreleased

- Import of API specifications. File > Import... also reads Swagger 2.0 and OpenAPI 3.0 and 3.1 files, in JSON or YAML: one folder per tag, one request per operation, with parameters, a sample body, and authentication set with variables. The preview has a new Base URL box. The file dialog starts on "All supported files", which now includes `.json`. What is not imported is listed as warnings. A nested object in a form body is written as JSON text into one field. New dependency: Microsoft.OpenApi 3.11.1 (MIT licence).
- Search in the response. Ctrl+F in the response body opens a panel above it with a search box, Match case and Whole word. All matches are highlighted and F3 and Shift+F3 (in the box also Enter and Shift+Enter) step through them. Escape closes the panel and leaves the current match selected, ready to copy. Each tab keeps its own search. Ctrl+F elsewhere opens the filter, as before; in the response body it no longer does. In a very long line, such as a large minified body in the Raw view, selecting a match with Escape can take a few seconds.
- "New request" in the Collections tree starts a rename of the new request. Enter saves the name and moves to the URL box, Escape keeps "New request" and moves there too, and clicking elsewhere saves the name. Before, the cursor went to the body editor.
- View > Track active request: the Collections tree selects the request of the active tab, opens the folders above it and scrolls to it. It is off by default and remembered. The keyboard focus does not move, and a request that the filter hides is left hidden.
- RestDraft has an icon: request and response arrows in a circle. The Windows executable, every window and the About window show it. On Linux only the windows have it, because there is no installer and no `.desktop` file.
- The packages carry `LICENSE`, `NOTICE` and `third-party-notices.txt`, which lists every component in the package with its copyright and license text. The 0.3.0 package was published without these files.
- JSON and XML highlighting no longer uses TextMateSharp, because of a doubt about its license. The colors are about the same. In a very long line, such as a minified body, the first 10,000 characters are colored. The package is smaller.

## 0.3.0 (import and export, a filter popup and a folder package)

Two new features and a change in how the program is packaged. The packages are the same as in 0.2.0: `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz` and `SHA256SUMS.txt`, built by GitHub Actions from the release tag.

### What changed

- Import and export. File > Import... reads a collection from an Insomnia v5 YAML file and File > Export... writes one. Import shows a preview with a check box on every folder and request, goes into a new or an existing collection, and can overwrite requests that have the same name (they keep their identity, and open tabs of them are reloaded; a tab with unsaved edits asks first). Things RestDraft does not keep (descriptions, scripts, settings, cookies, environments, folder-level headers and authentication, OAuth 2 and other authentication types) are listed as warnings. Insomnia v4 JSON, Postman and OpenAPI are not supported yet.
- One filter for the Collections tree and the History. The funnel button at the left of the top bar (or Ctrl+F, or Edit > Filter...) opens a popup with text, method and "Search in bodies". Each tab keeps its own filter, and a filter never closes an open tab. The search box and the method list moved off the History tab.
- The package is now a folder of files instead of a single executable. Each archive holds one folder, `RestDraft-<runtime>/`, with the executable, its DLLs and the native libraries (about 270 files). Nothing is unpacked into `%TEMP%\.net\RestDraft` (Linux: `~/.net/RestDraft`) any more, and the program writes only inside its own folder. The download is about 4 MB (8%) larger: the zip is 52.0 MB against 47.9 MB, the tarball 47.6 MB against 44.0 MB.
- Internal: a new dependency, YamlDotNet 18.1.0 (MIT licence), for the YAML files.

### Upgrading from 0.2.0 or earlier

Unzip the new package into a new folder, run `RestDraft.exe` from it, and keep the other files next to it. To keep your collections, history and environments, copy the old `data` folder into the new program folder (or point to it with `--data-dir` or `RESTDRAFT_DATA_DIR`). The old `%TEMP%\.net\RestDraft` (Linux: `~/.net/RestDraft`) is no longer used and can be deleted.

### Not signed

The executables are not code-signed. SignPath Foundation declined the application for now, because the project does not yet have enough public visibility, and the project will apply again later. See the README for the SmartScreen and Smart App Control notes (Windows), and verify every download against `SHA256SUMS.txt`. With the folder package, the program's own DLLs are unsigned files too.

### Known limitations

- A file exported from RestDraft has not been opened in Insomnia itself yet. Exported files hold Basic auth passwords and tokens in plain text.
- The file dialogs of import and export were not checked on Linux.
- The limitations listed under 0.1.1 (Linux) and 0.1.0 still apply.

## 0.2.0 (window state, tree icons and a taller body editor)

Small improvements to the window and the request editor. The packages are the same as in 0.1.1: `RestDraft-win-x64.zip`, `RestDraft-linux-x64.tar.gz` and `SHA256SUMS.txt`, built by GitHub Actions from the release tag.

### What changed

- The window remembers its size and whether it is maximized.
- Collections and folders in the tree have an icon and text one size larger than requests.
- The request body editor fills the height of the Body tab (it was a fixed 320 pixels), so a long body needs less scrolling. The message shown when Format fails now appears next to the Format button instead of under the editor.
- The splitter between the response body and the response details can no longer be dragged to the bottom edge, where it could not be grabbed again.
- Internal: the build fails on nullable warnings and on a list of analyzer rules, and other analyzer findings are warnings, so a new .NET SDK cannot break the build of unchanged code. This changes nothing for users.

### Not signed

The executables are not code-signed. SignPath Foundation declined the application for now, because the project does not yet have enough public visibility, and the project will apply again later. See the README for the SmartScreen and Smart App Control notes (Windows), and verify every download against `SHA256SUMS.txt`.

### Known limitations

The limitations listed under 0.1.1 (Linux) and 0.1.0 still apply.

## 0.1.1 (Windows and Linux)

Adds a Linux x64 package. The program itself has the same features as 0.1.0. Both packages are built by GitHub Actions from the release tag (0.1.0 was built on the maintainer's machine). `SHA256SUMS.txt` lists both.

- `RestDraft-win-x64.zip`: as in 0.1.0.
- `RestDraft-linux-x64.tar.gz`: a single self-contained executable plus its `MigrationScripts` folder. The archive keeps the executable permission. A few system libraries must be installed first; the README lists them under Linux.

### What changed

- Linux x64 package. It passed 13 end-to-end checks (data folder next to the program, SQLite, `--data-dir` and `RESTDRAFT_DATA_DIR`, sending requests, collections and history surviving a restart, secrets, multipart files, large responses, and the UI) on Ubuntu 24.04 under WSL2.
- CI runs the build and all tests on Linux as well as on Windows.
- Internal: migration folders are read in an explicit order. This changes nothing for users.

### Not signed

The executables are not code-signed. SignPath Foundation declined the application for now, because the project does not yet have enough public visibility, and the project will apply again later. See the README for the SmartScreen and Smart App Control notes (Windows), and verify every download against `SHA256SUMS.txt`.

### Known limitations on Linux

- Checked on Ubuntu 24.04 under WSL2 (WSLg) only. Other distributions and a full Linux desktop are untested.
- Under WSLg, closing a window or a popup can lag by about a second. This was reproduced with another application (gedit started with `GDK_BACKEND=x11`), so it comes from WSLg and not from RestDraft.
- Startup problems (for example a data folder that cannot be created) are printed in the terminal, because there is no message box.
- The limitations listed under 0.1.0 still apply.

## 0.1.0 (first Windows release)

First packaged release. Windows x64 only: `RestDraft-win-x64.zip`, a single self-contained executable plus its `MigrationScripts` folder. Linux follows in a later version.

### What is in it

- Request builder: method, URL, query parameters and headers (rows that can be switched off), raw / form-urlencoded / multipart bodies, JSON and XML highlighting and formatting.
- Authorization: none, Basic, Bearer, API key.
- Response viewer: status, timing (connect, first byte, total), size, headers, pretty or raw body, large bodies spilled to disk.
- Collections, folders and subfolders, with drag and drop, rename, duplicate and delete (with confirmation).
- History with search by URL and method, and restore into a new unsaved tab. An entry shows the URL and enabled query parameters as typed, with `{{placeholders}}` left in place. Resolved values, including secrets, are never stored in it.
- Environments, `{{variables}}` and secret variables encrypted with a master password.
- Tabs with autosaved drafts that survive a restart.
- Settings: timeout, redirects, SSL verification, proxy.
- Main menu (File, Edit, View, Tools, Help) and a three-panel window: Collections and History, the request, and the response on the right, with splitters. Panel widths are remembered. The View menu hides the panels and the bar with the secrets and the environment, and the choice is remembered. The Tools menu locks or unlocks secrets and opens the environments.
- Environments window: names are edited in the list, the current environment is marked, and Create, Rename and Delete are small buttons and context menu items. Dialogs have no taskbar button of their own.
- A new request opens on its Body page with the body focused. Save All (Ctrl+Shift+S). Ctrl+Tab switches request tabs in the order they were last used.
- Setting: open a request with a single click in the Collections tree (off by default).
- Keyboard use throughout: tree shortcuts, dialog defaults, Esc to cancel, named controls for screen readers, light and dark themes.

### Not signed

The executable is not code-signed in this release. See the README for the SmartScreen and Smart App Control notes, and verify the download against `SHA256SUMS.txt`. The project's [Code signing policy](README.md#code-signing-policy) describes the status of signing.

### Known limitations

- Query parameters in the Params tab and a query string typed into the URL are both sent, and are not kept in sync.
- A forgotten master password cannot be recovered.
- A proxy with credentials is not supported. Turning SSL verification off disables all certificate checks.
- A multipart file is read fully into memory before sending.
- Pasting a single line of about 2 MB into the body editor takes several seconds. Format the body first.
- Collections cannot be moved or duplicated as a whole. Folders cannot be duplicated.
- Each change to the collection tree saves the whole tree. This is fine for normal sizes.
- Drag and drop in the tree needs a mouse.
- Startup problems (for example a data folder that cannot be created) are shown in a message box. Other platforms print them to the terminal only.
