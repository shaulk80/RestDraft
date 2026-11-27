# Portability and release checklist

Run this against the **extracted release zip**, not the build output. It is the file a user receives. Record the date, the machine and the result for every step. A step that fails stops the release.

Release under test: `dist/RestDraft-win-x64.zip` (from `scripts/build.ps1`)
SHA-256 (from `dist/SHA256SUMS.txt`): ______________________________

## 1. Windows portability (plan M8 task 1)

Machine: ______________________  Windows version: ______________  Date: ____________

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Compare the zip hash with `SHA256SUMS.txt` | Hashes are equal | |
| 2 | Unzip into a new folder that is not the repository, for example `C:\Temp\RestDraftCheck` | One folder `RestDraft-win-x64` with `RestDraft.exe`, its DLLs (`RestDraft.*.dll` and the libraries), `MigrationScripts\`, `LICENSE`, `NOTICE` and `third-party-notices.txt`; no `.pdb` files. The Linux tarball holds the same three files | |
| 3 | Run `RestDraft.exe` | The window opens. `data\app.db` and `data\logs\<file>` exist beside the executable | |
| 3a | Look at the icon: `RestDraft.exe` and a shortcut to it in Explorer, the taskbar, Alt+Tab, the title bar of the window and of a dialog, Help > About | The two arrows in a circle everywhere, still readable at the small sizes | |
| 4 | Look for `data` anywhere else (`%APPDATA%`, `%LOCALAPPDATA%`, the current directory) | Nothing there, and `%TEMP%\.net\RestDraft` is not created. The program puts nothing outside its folder (releases up to 0.2.0, which were single-file, unpacked native libraries there) | |
| 5 | Create a collection, a folder, and a request. Enter `https://example.com`, send | Status 200 shows, the response body is displayed | |
| 5a | Open the Params tab and add `q` = `1`, open the Headers tab and add `X-Test` = `abc`, then send to an echo endpoint such as `https://httpbin.org/anything` | The echo shows `q=1` in the query and `X-Test: abc` as a header. The tab titles show `Params (1)` and `Headers (1)` | |
| 6 | Save the request (Ctrl+S) | The tab loses its `*` | |
| 7 | Close the app and run it again | The collection tree, the open tab (with its parameter and header) and the History entry are back | |
| 8 | Close the app. Run `RestDraft.exe --data-dir D:\RestDraftData` (any other folder) | A new empty data folder is created there. The first one is untouched | |
| 9 | Set `RESTDRAFT_DATA_DIR` to another folder and run without the argument | That folder is used | |
| 10 | Make the data folder impossible to create (for example `--data-dir Z:\missing` for a drive that does not exist) | A message box "RestDraft" explains the problem. The process ends | |
| 11 | Delete the unzipped folder | Nothing is left behind except the files you made yourself | |

## 2. Accessibility check (plan M7 task 1)

Do it once per release, in light and in dark mode (Windows *Settings > Personalization > Colors*).

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Use only the keyboard: Tab through the main window | Focus reaches every control and is visible; no focus trap | |
| 2 | In the Collections tree: arrow keys, F2, Delete, Enter, Shift+F10, Esc | Moves, renames, asks to delete, opens a request, opens and closes the menu | |
| 2a | Tab into the body editor (choose a raw body first), then press Esc / Shift+Esc | Focus moves to the next / previous control | |
| 3 | New collection / New folder | The name box has focus with its text selected | |
| 4 | Open each dialog (Save, Settings, Unlock, Set master password, Environments, Delete, Unsaved changes) | Focus starts in the first input. Enter confirms where it is safe, Esc cancels | |
| 5 | Switch Windows between light and dark while the app runs | Text stays readable. Error and warning text is readable. JSON and XML colors follow the theme | |
| 6 | Run Narrator (Win+Ctrl+Enter) and visit the controls | Each control is announced with a name, not just its type | |
| 7 | Press Alt, then use the arrow keys and Enter in the main menu | File, Edit, View, Tools and Help open, every item shows its shortcut, and each item works | |
| 8 | Tab to a splitter (named "Resize the Collections panel" / "Resize the Response panel") and press the left and right arrow keys | The panel changes width. After a restart the width is the same | |
| 9 | Open three request tabs, then press Ctrl+Tab with focus in the tree, in the Params tab, and in the body editor | Only the request tab changes, to the one used before. The other tabs stay where they are | |
| 10 | Open a dialog (Settings, Environments, a confirmation) | Only the main window has a taskbar button | |

## 3. Smart App Control (plan M8 task 5)

Needs a clean Windows 11 install or VM where Smart App Control is **On**. (It can only be turned on in a fresh install, so a VM snapshot is the easiest way to repeat this.)

Machine / VM: ______________________  Build: ______________  Date: ____________

| # | Step | Expected | Result |
|---|---|---|---|
| 1 | Download or copy the zip to the machine, so it carries the mark of the web | | |
| 2 | Unzip and run `RestDraft.exe` | Record exactly what happens: runs, SmartScreen prompt, or blocked. All the DLLs carry the mark of the web too: record whether any is blocked (the window opens and renders text, and requests work) | |
| 3 | If it is blocked: **stop and revisit signing (Q7 in ARCHITECTURE_PLAN.md) before the release** | | |

Result for the 0.3.0 folder package (exe + DLLs): checked by the owner and works as expected, reported on 2026-11-24. Machine, build and the exact behavior were not recorded.

## 4. Before the first public release (plan M8 task 4)

- [x] Apply to SignPath Foundation (Q7). Outcome: **declined** (answer received by 2026-10-31): the project does not yet show enough public visibility (stars, forks, contributors, articles, discussions). Reapply when it does. The owner declined the paid subscription.
- [x] Not eligible, so the releases ship unsigned, with checksums and the README notes. Signing is not planned until a new application is approved.
- [ ] `README.md`, `RELEASE_NOTES.md` and the version in `Directory.Build.props` agree.
- [ ] CI is green on `main` for the commit being released, and its uploaded zip has the same hash as the one checked above.

## 5. Linux (plan M9, section 8.1)

Machine: Windows 10 Pro 22H2 (build 19045), WSL2 with WSLg  Ubuntu: 24.04.5 LTS (noble)  Date: 2026-10-31
WSL 3.0.3.0, kernel 6.18.54.1-1, WSLg 1.0.79. Build: `dist/RestDraft-linux-x64.zip` from `scripts/build.ps1 -Runtimes linux-x64`, copied to the Linux home folder and extracted there.

System packages installed before the first start: `unzip libicu74 libfontconfig1 fonts-dejavu-core libx11-6 libice6 libsm6 libxext6 libxrandr2 libxi6 libxcursor1 libgl1`. Not tested which of them are strictly required.

| # | Check | Result |
|---|---|---|
| 1 | Data folder (`app.db`, log) beside the executable | Pass |
| 2 | SQLite native library loads (unpacked to `~/.net/RestDraft`) | Pass |
| 3 | Nothing else written outside the app folder and `~/.net` | Pass |
| 4 | `--data-dir` redirects | Pass |
| 5 | `RESTDRAFT_DATA_DIR` redirects | Pass |
| 6 | Startup error is readable on stderr | Pass |
| 7 | Send a request (`https://example.com`) | Pass |
| 8 | Collections, tab and History survive a restart | Pass |
| 9 | Params and headers reach the server | Pass |
| 10 | Secrets with a master password | Pass |
| 11 | Multipart file from the file picker | Pass |
| 12 | UI behavior (menus, shortcuts, drag and drop, splitters, themes, fonts) | Pass, with the lag below |
| 13 | Large response (over 1 MB) | Pass |

Findings:
- **Executable permission:** the zip is made on Windows, so after `unzip` the file `RestDraft` is not executable. `chmod +x RestDraft` is needed. Not an app bug. Fix: a `.tar.gz` built on an Ubuntu CI runner that keeps the permissions.
- **About 1 s lag when a window or popup closes** (for example the request method combo box). Environment, not the app: gedit started with `GDK_BACKEND=x11` (XWayland) shows the same lag, gedit over Wayland does not, and `LIBGL_ALWAYS_SOFTWARE=1` does not change RestDraft. Not checked on a real Linux desktop.
- **Extraction folder:** the single-file publish of that time unpacked the native libraries to `~/.net/RestDraft/<hash>`. Not the case any more, see the re-check below.

### Import and export (to check on a Linux desktop)

File > Import... and File > Export... use the system file dialogs of Avalonia (`StorageProvider`). Checked by the owner with 0.3.0 on Windows and on Linux under WSL2 (same Ubuntu 24.04 setup as above, not a full Linux desktop) and works as expected, reported on 2026-11-24. The checks were: the open dialog shows `*.yaml` and `*.yml` files, the save dialog proposes `<collection name>.yaml` and asks before it overwrites, and a file written to a folder without permission gives a message instead of an error.

### Re-check of the exe + DLLs package (2026-11-18)

Same machine and WSL as above. The package stopped being single-file (decision of 2026-11-18, `docs/ARCHITECTURE_PLAN.md` section 3.2). Built in WSL from a copy of the tree in the Linux home folder, with .NET SDK 10.0.115 and PowerShell 7.6.10, by `pwsh ./scripts/build.ps1 -Runtimes linux-x64`. Not repeated: the manual checks 1 to 13 above (only that the window opens and the app runs).

| # | Check | Result |
|---|---|---|
| 1 | Tests (95, 243, 84, 16, 422), license check, coverage targets, smoke test under Xvfb | Pass |
| 2 | The tarball has one folder `RestDraft-linux-x64/` (280 entries); `RestDraft` and `createdump` are executable, every other file is 644 | Pass |
| 3 | The native libraries (`libSkiaSharp.so`, `libHarfBuzzSharp.so`, `libe_sqlite3.so`, `libonigwrap.so`) are files next to the executable | Pass |
| 4 | `~/.net/RestDraft` gets no new folder when the app runs | Pass |
| 5 | The window opens under WSLg and the app runs | Pass (see the WSLg note below) |

### Testing on WSL: avoid a false slowness

- The first GUI window after WSL starts, or after `wsl --shutdown`, can take 1 to 5 minutes while WSLg connects to Windows. Meanwhile the app is healthy: it has logged its start and uses about 0% CPU (measured 2026-11-18: `data/app.db` and the log within a second, all D-Bus calls answered within a second, then idle).
- It is not the app, the build or the working folder: the released 0.2.0 (single-file) was equally slow at first; after `gedit` had opened a window, every start was quick. When the delay is not an app bug, running another GUI app first will show the same delay.
- Before timing an app on WSLg: open `gedit`, wait for its window, close it, then time the app. To measure a cold start on purpose, run `wsl --shutdown`, open the distro, and time `gedit` first.
- Build in the WSL home folder, not under `/mnt/c`: the Windows drive keeps no Linux file permissions, so the executable-bit check is meaningless there, and it is slow.
- `~/.dotnet/tools` (where the `pwsh` tool lives) is not on `PATH` in non-interactive shells such as `wsl.exe -- bash script.sh`. Set it in the script.

### Verify a release in WSL

`scripts/verify-release-wsl.sh` checks a `RestDraft-linux-x64.tar.gz` (a downloaded release asset, or `dist/` of a CI artifact) the way a user would run it. Use it for step 8 of the release procedure and after any change to the package layout.

1. Download the release assets into a new, empty folder, for example `gh release download v<version> -D C:\Temp\rd-check`. Keep `SHA256SUMS.txt` next to the tarball so the script can check the hash.
2. Run it from Windows, with the tarball path as a Linux path (`/mnt/c/...`) or a Windows path:

   ```powershell
   wsl.exe -d Ubuntu-24.04 -- bash /mnt/c/Vibe/RestDraft/scripts/verify-release-wsl.sh /mnt/c/Temp/rd-check/RestDraft-linux-x64.tar.gz
   ```

3. Read the `PASS` and `FAIL` lines. The exit code is 0 only when all of them passed.

What it checks: the hash (when `SHA256SUMS.txt` is there), one top-level folder `RestDraft-linux-x64/`, that only `RestDraft` and `createdump` are executable, that the app starts from a fresh extraction in the WSL home folder (not `/mnt/c`), that `data/app.db` and a log appear inside the program folder, that the app is still running 5 seconds later, and that `~/.net/RestDraft` got no new entries. It opens `gedit` first when it is installed, so the WSLg warm-up (see above) is not mistaken for a slow app. `WAIT_SECONDS` (default 360) is how long it waits for `data/app.db`. It deletes its extraction folder when it ends.

It does not click through the UI: sending a request, the file dialogs and the other manual checks above stay manual (next section). The script is kept as LF in git (`.gitattributes`), because bash cannot run a CRLF file.

### Run the app in WSL for manual tests

For the checks the script cannot do: use the app in its window, the way a user would. Do it after the script passed, with the same tarball.

1. **Libraries.** The distro needs the system libraries from the README (Linux, step 2). They are already installed in `Ubuntu-24.04`; on a new distro, run the `sudo apt install` line from the README yourself.
2. **Warm up WSLg.** Open the distro (`wsl -d Ubuntu-24.04`), run `gedit &`, wait for its window to appear (up to 5 minutes after WSL starts), then close it. Skip this and the first start of the app looks hung (see above).
3. **Extract in the WSL home folder**, not under `/mnt/c` (no file permissions there, and slow):

   ```bash
   mkdir ~/rd-manual
   tar -xzf /mnt/c/Temp/rd-check/RestDraft-linux-x64.tar.gz -C ~/rd-manual
   ```

4. **Start it from the terminal**, so that startup errors and anything the app prints are visible:

   ```bash
   ~/rd-manual/RestDraft-linux-x64/RestDraft
   ```

   The window opens on the Windows desktop through WSLg. `data/` is created inside `~/rd-manual/RestDraft-linux-x64/`. To keep data between runs or between package versions, start with `--data-dir ~/rd-data` (or set `RESTDRAFT_DATA_DIR`). Stop the app by closing its window or with Ctrl+C in the terminal.
5. **Something to send requests to.** `https://example.com` works without any setup (check 7 above). For a server you control, run `tests/RestDraft.SampleApi` (listens on `http://localhost:5080`) inside the same distro, from the copy of the tree in `~/RestDraft-build`: `dotnet run --project tests/RestDraft.SampleApi`. Reaching a server that runs on Windows from WSL2 depends on the WSL network mode and was not tried.
6. **What to check.** Go through the manual checks 1 to 13 in the first table of this section, and the items still open: Import and Export file dialogs (see "Import and export" above), the filter popup (funnel button, Ctrl+F, Edit > Filter...), the icon of the windows, and the window behaviour (menus, shortcuts, drag and drop, splitters, themes, fonts). Record the result as a new dated section after the tables above, with the distro and WSL versions (`wsl --version`, `lsb_release -d`).
7. **Clean up.** Delete `~/rd-manual` (copy `data` out first if it holds anything you want). `~/.net/RestDraft` is not used by 0.3.0 and later; a leftover one comes from older versions and can be deleted.

Known WSLg behaviour that is not a bug of the app: the first window can take minutes, and closing a popup can lag by about a second.
