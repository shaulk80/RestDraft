# New features: implementation plan

Status (2026-11-27): approved. PRs 1 (#26), 2 (#28), 3 (#29), 4 (#31) and 5 (#32) are merged, PR 6 is not started. Requirements and decisions: `new-features-requirements.md`.

## 1. Delivery

Six pull requests: the license notices first, because the packages need them whatever else ships, then the features, smallest first. Each one starts from an up-to-date `main`, ends with tests green on both CI jobs, a run of the app, and its lines in `README.md`, `RELEASE_NOTES.md` and `STATUS.md`. Commits follow Conventional Commits, one per logical change.

| PR | Branch | Content | Size | Needs |
|---|---|---|---|---|
| 0 | `docs/new-features-requirements` | Requirements and this plan | docs | |
| 1 | `fix/third-party-notices` | License and notice files in the packages (item 6) | S to M | |
| 2 | `feat/app-icon` | Icon (item 1) | S | |
| 3 | `feat/new-request-and-tracking` | New request rename (item 4), tracking (item 2) | S to M | |
| 4 | `feat/response-search` | Search in the response (item 5) | M | |
| 5 | `feat/openapi-import` | OpenAPI and Swagger files in File > Import... (item 3, part 1) | L | 1 |
| 6 | `feat/import-from-url` | Download, Swagger UI discovery, dialog (item 3, part 2) | M to L | 5 |

PR 5 is useful on its own (import of a downloaded file) and keeps the mapping review apart from the network review. A release (suggested: 0.4.0) follows with `/release` when the owner decides.

## 2. PR 1: license and third-party notices

**Why:** the packages ship no `LICENSE`, no `NOTICE` and no third-party notices. Checked in the local win-x64 package of 2026-11-24; `build.ps1` has no step that adds them, so the published releases most likely lack them too. MIT, BSD and Apache-2.0 require their notices to travel with the binaries. RestDraft's own license (Apache-2.0) does not change.

1. `scripts/ThirdPartyNotices.ps1`, dot-sourced by `build.ps1`, runs for each runtime after publish and before packaging. It copies `LICENSE` and `NOTICE` into the package folder and writes `third-party-notices.txt` there.
2. What ships is read from the published `RestDraft.deps.json`: every package with files, and the .NET runtime pack (60 packages and the pack in the win-x64 build above). Test packages are not in it.
3. Each entry has the name, version, license, copyright and license text, plus the notices file the package carries, if any (SkiaSharp, HarfBuzzSharp, the Microsoft.Extensions packages and Onigwrap have one). The runtime pack gives its `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT`. Identical texts are written once.
4. Most packages carry no license file. Standard texts in `scripts/licenses/` (MIT, Apache-2.0, BSD-3-Clause) are filled with the copyright from the nuspec.
5. A small data file in `scripts/licenses/` adds what the packages do not say, keyed by id and version like the reviewed license list: the copyright of the four that declare none (Serilog.Extensions.Logging, Serilog.Sinks.File, TextMateSharp, TextMateSharp.Grammars), and code or assets bundled inside a package without a notice. To check against the upstream projects: SQLite in SQLitePCLRaw, Oniguruma in Onigwrap, the grammars and themes in TextMateSharp.Grammars, and any fonts embedded in the Avalonia packages.
6. The build fails when a shipped component has no copyright or no license text, so a new library cannot be missed.
7. `README.md` (License section), `RELEASE_NOTES.md` and the portability checklist say that the files are in the package.

Checks: the repository has no PowerShell tests, so run `build.ps1` on Windows and let CI run it on Linux. The smoke test also asserts that the three files are in the extracted package. Remove one data file entry and see the build fail. Compare the entries with the list from `deps.json`, and read the notices file once.

Releases already published stay as they are; the next release carries the files.

## 3. PR 2: app icon

1. Move `docs/icons/icon-3-exchange.svg` to `src/RestDraft.UI/Assets/app-icon.svg` and delete the other candidates.
2. `scripts/icon/MakeIcon.cs`, a .NET file-based app (`dotnet run`) that renders the SVG with SkiaSharp 3.119.4 (the version the app already ships) into `app-icon.ico` (16, 20, 24, 32, 40, 48, 64, 128, 256 px) and `app-icon.png` (256 px). It switches central package management off for itself. The generated files are committed; the build does not run the script.
3. `RestDraft.UI.csproj`: `ApplicationIcon` and `AvaloniaResource` items.
4. Window icon: one `Icon` setter in the `:is(Window)` style of `AppStyles.axaml`. If a style setter cannot load the icon, set it in one small behavior used by every window.
5. `AboutWindow.axaml` shows the PNG; `README.md` shows it at the top.

Tests: a headless test, parameterized over the windows, asserts that each has an icon. Manual: Explorer, taskbar, Alt+Tab, a dialog, the About window; the window icon on WSL; add these lines to the portability checklist. Look at the 16 and 20 px sizes on a real taskbar: if the ring or the arrows blur, add a heavier drawing for sizes up to 24 px.

## 4. PR 3: new request rename and tracking

### 4.1 New request (item 4)

Today `CollectionsTreeViewModel.CreateRequestAsync` raises `RequestOpenRequested`, and opening the tab posts a focus to the body editor. A rename box started at the same time would lose the focus, and losing it commits the name.

1. `CollectionsTreeViewModel` raises a separate `RequestCreated` event. `MainWindowViewModel` handles it in this order: await `TabStrip.OpenRequestAsync(id, focusEditor: false)`, then `Sidebar.BeginRenameOfNew(id)`.
2. `TabStripViewModel.OpenRequestAsync` gets `focusEditor` (default true); `TabOpened` is raised only when it is true.
3. `TreeNodeViewModel` remembers that the rename is the first one of a new request. The Enter and Escape key bindings get commands of their own, apart from the lost-focus commit. For a new request they raise `UrlFocusRequested` after the rename has ended and the tree has reloaded.
4. `MainWindow` posts the focus to the URL box through a new `RequestBarView.FocusUrl()`.

Tests: view model tests, parameterized over Enter, Escape and lost focus (name saved or kept, URL focus asked or not); an existing rename asks for no URL focus. Headless: after "New request" the rename box has the focus with the name selected; Enter moves it to the URL box.

### 4.2 Tracking (item 2)

1. `AppSettings.TrackActiveRequest` (false), key `ui.trackActiveRequest` in `AppSettingsStorage`.
2. `WorkspaceLayoutViewModel.IsTrackingActiveRequest`, restored and stored like the panel visibility. View menu: `_Track active request` (check box).
3. `CollectionsTreeViewModel.Reveal(Guid requestId)`: find the shown node, expand its parents, select it. No node, no change.
4. New `ActiveRequestTracker` (UI view models): listens to `TabStrip.SelectedTab`, to the switch, and to `Sidebar.Search.Applied`, and calls `Reveal` for a linked tab. It does not listen to `TreeChanged`, so a reload for another reason keeps the selection.
5. Check in a headless test that the tree scrolls to a node selected this way, parents just expanded included. If Avalonia does not do it, the view calls `BringIntoView` after the layout pass.

Tests: `ActiveRequestTracker`, parameterized (off, on, switched on later, unlinked tab, filtered-out request, folder reload); settings round trip; headless scroll test.

Manual: new request by button and by context menu, with the single-click setting on and off; tracking with Ctrl+Tab, tab close, restart and an active filter.

As built, two points go beyond the steps above. Step 4 of 4.2: the tracker listens to a new `TabStrip.ActiveTabChanged`, not to `SelectedTab`, because the tab control selects tabs by itself while tabs are added or replaced, and an import or a start then moved the tree to a request that was not the active one. Step 5: Avalonia scrolls only to a newly selected node, so `Reveal` raises `NodeRevealed` and the view calls `BringIntoView`.

## 5. PR 4: search in the response

1. `RestDraft.Application/Search`: `TextMatch` (start, length), `TextSearchOptions` (match case, whole word), `TextMatchFinder` (pure, matches do not overlap).
2. `ResponseSearchViewModel`, owned by `ResponseViewModel` as `Search`, so each tab keeps its own: `IsOpen`, `Query`, `MatchCase`, `WholeWord`, matches, current index, counter text, no-match flag, truncation note, commands for next, previous and close. `ResponseViewModel.UpdateBodyText` gives it the shown text.
3. `ResponseSearchView` (the panel), in a new row of `ResponseView` between the Pretty and Raw bar and the body.
4. `Controls/SearchHighlightRenderer` (an AvaloniaEdit background renderer that draws only the visible matches) and a small class that connects the view model to the editor: highlights, scroll to the current match, selection on close.
5. Keys: `MainWindow.OnPreviewKeyDown` asks `ResponseView` first (`TryOpenSearch`, true when the focus is in the response editor or the panel) and opens the filter only otherwise. `ResponseView` takes F3, Shift+F3 and Escape in the tunnel phase, ahead of the editor's own Escape (which leaves the editor), and Enter and Shift+Enter in the search box.
6. Remove AvaloniaEdit's built-in search panel from the response editor, so two find boxes cannot compete.
7. Highlight brushes for both themes in `AppStyles.axaml`; accessible names, and a polite live region for the counter.

Tests: `TextMatchFinder`, parameterized (case, whole word with punctuation at the edges, start and end of text, repeated letters, empty query, no match); view model (wrap around, start at the cursor, new text, options); headless (Ctrl+F in the editor opens the panel and not the filter, Ctrl+F elsewhere opens the filter, F3 cycles, Escape selects the match, tab switch keeps each state, accessibility check).

Manual: a response near the 1 MB preview limit searched for a single letter must stay responsive; long wrapped lines; light and dark.

As built, one point goes beyond the steps above. Step 4: the renderer does not use AvaloniaEdit's `BackgroundGeometryBuilder`, because far down a very long wrapped line it takes a good part of a second per match, so a repaint took over 40 s. `VisibleTextRows` gives the rows on screen and each row gives the places of its matches. Two details the requirements leave open: with a selection, Ctrl+F starts at its beginning, so the selected text is the current match; Shift+F3 with the panel closed opens it at the match before the cursor.

## 6. PR 5: OpenAPI and Swagger files

### 6.1 Dependencies and YAML reading

`Microsoft.OpenApi` 3.11.1 (MIT, no dependencies on net8.0 and later) reads 2.0, 3.0 and 3.1 from JSON. Version checked on NuGet on 2026-11-26 and checked again at implementation. The notices step of PR 1 must cover it.

YAML is read by our own `YamlToJsonConverter`, which turns it into JSON with YamlDotNet, already part of the app. No further package is needed, and one parser both checks and reads the file. The value rule: keys, quoted values and block values are text; a plain value is null, true or false when written so, a number when it matches the JSON number grammar (kept as written, so `1.50` stays `1.50`), and text otherwise (`00501`, `+12`, `0x1F`). YamlDotNet's own settings are not enough, as tried on 18.1.0 on 2026-11-26: by default every value except null becomes text, and with type guessing `00501` becomes `501` and block text `123` becomes a number.

**Decision step: keep the converter or switch.** It is the first work of PR 5, before the mapping.

1. Write the converter and tests that go through `OpenApiFormat`, so they hold for either reader: the values above as examples and defaults in a YAML specification, checked in the resulting requests; an unquoted `info.version: 1.0`; the same specification as YAML and as JSON giving the same collection; at least two large real specifications.
2. Keep the converter when the tests pass, or when a fix stays within the value rule and the walk over the YAML tree.
3. Switch to the official reader when a valid specification is read wrongly or refused because of the conversion and the fix needs more YAML handling than that (explicit tags, several documents and the like), or when the converter grows past about 150 lines. The same rule applies to a limitation found later.
4. To switch: add `Microsoft.OpenApi.YamlReader` 3.11.1 (MIT), which brings `SharpYaml` 2.1.5 (MIT), remove the converter, keep the safety check in front of the reader and keep the tests. The pull request says what caused the switch.

Aliases are no reason to switch: the safety check refuses them with either reader. Record the outcome as a decision in `ARCHITECTURE_PLAN.md`.

### 6.2 Application changes

1. Split the format interface: `ICollectionFormat` reads; `IWritableCollectionFormat : ICollectionFormat` adds `Write`. `ExportService` takes the writable one instead of `formats.First()`.
2. `ExchangeDocument.BaseUrl` (null when request URLs are complete, as for Insomnia) and `WithBaseUrl(text)`, which puts it in front of every request URL.
3. `BaseUrlResolver` (pure): declared server address and optional download address to the value shown in the preview (section 4.2 of the requirements).
4. Extract the "which format reads this text" part of `ImportService` into `CollectionTextReader`, which PR 6 uses too.

### 6.3 Infrastructure (`Exchange/OpenApi`)

One class per file: `OpenApiFormat` (detects the top-level `openapi` or `swagger` key and converts YAML first), `YamlToJsonConverter` (section 6.1; bad YAML, duplicate keys and keys that are not text give a failed `Result`), `OpenApiImportMapper` (tags to folders, names), `OpenApiRequestMapper` (parameters and URL), `OpenApiBodyMapper`, `OpenApiAuthMapper`, `SchemaSampleBuilder` (examples, defaults, enums, formats, `allOf` merged, first of `oneOf` and `anyOf`, read-only properties left out, a depth limit and a stop at recursive references). The YAML safety check (no aliases, depth limit) moves from `InsomniaYamlScanner` to a shared class, and runs before the OpenAPI reader. External references are never loaded.

Mapping cases that give a warning, never a failure: TRACE operations (skipped), cookie parameters and cookie API keys, several security schemes combined (first one used), several servers or a server per operation (first top-level one used), unsupported body types, external references. Server variables take their default values.

### 6.4 UI

`ImportPreviewViewModel` and window: a Base URL row, shown when the document has one; Import disabled while it is empty; `ImportRequest` carries the value and `ExchangeDialogs` applies it. The open file dialog gets "All supported files" as its first filter, with `.json` added.

### 6.5 Tests

Synthetic fixtures for 2.0, 3.0 and 3.1, in JSON and YAML. After the decision step of section 6.1, which includes a parameterized table of YAML values, write these first, because they check assumptions about the library: the server address of a 2.0 file with and without `host`, recursive schemas, and diagnostics for a broken file. Then parameterized mapper tests (names, parameters, bodies, each authentication type, warnings), hostile input (aliases, deep nesting, too large, external reference), `BaseUrlResolver`, preview view model, and an integration test that imports a fixture into SQLite and sends one request to the in-process server.

Manual: import a real specification file (for example the public Petstore one), send a request with a body.

As built (2026-11-27):

- Decision step of section 6.1: the converter stays (about 140 lines). The YAML and JSON editions of the Petstore (2.0 and 3.0), Stripe and GitHub specifications gave identical collections, and every operation of those and of the Kubernetes one was imported (up to 1281 requests, about 2 s at most).
- RestDraft parses the JSON itself, to a depth of 128, and hands the tree to the library, because the library's own parsing stops at 64.
- The library throws on a reference that leads back to itself, and on some malformed files with exceptions such as a null reference. `OpenApiReferences` follows references one step at a time, and any exception from the reader fails the read with its reason. A tag name of only spaces or an empty `$ref` therefore refuses the whole file.
- The library joins the bare types of an OpenAPI 3.0 `oneOf` or `anyOf` into one list of types, so "the first one" cannot be told there; a text is taken where it is allowed.
- More classes than section 6.3 names, one job each: `OpenApiTagFolders`, `OpenApiParameterValue`, `OpenApiValues`, `OpenApiReferences`, `OpenApiServerAddress`, and the shared `ImportWarningList` and `YamlRootValueReader`.
- Beyond the mapping rules: Swagger 2.0 `x-example` and a schema's `const` count as given values; a form without fields is no body (Stripe declares one on every GET); properties that the reader skips are reported apart from errors (Kubernetes has 2453 and is imported completely).
- All fields of a URL-encoded body are enabled, like the properties of a JSON sample. A nested object becomes JSON text in one field.

## 7. PR 6: import from URL

### 7.1 Application (`Exchange/Remote`)

- `ISpecificationDownloader` (in `Abstractions`): address, headers and the host allowed to get them, to `Result<DownloadedText>` (final address and text).
- `SwaggerPageScanner` (pure): from a page or script, the specification links (`url`, `urls`, `configUrl`, `spec-url`) and the scripts worth reading, such as `swagger-initializer.js`, `swagger-ui-init.js` and Swashbuckle's `index.js`.
- `RemoteImportService`: downloads the address; if a format reads the text, returns the document; if it is a page, follows the scanner (scripts of the same host only), then tries the usual paths (`swagger/v1/swagger.json`, `swagger.json`, `openapi.json`, `openapi/v1.json`, `v3/api-docs`) under the page's folder and the host root. At most 20 downloads per attempt. The result is a document, a list of specifications to choose from, or an error that says what was tried.

### 7.2 Infrastructure

`HttpSpecificationDownloader`, built on `RequestHttpClientFactory`: the app's timeout, proxy and SSL settings; redirects followed by hand (when the setting allows), so typed headers are dropped when scheme, host or port change; reading stops at 20 MB; a status other than 2xx is an error with the status; failures are logged without header values or query strings.

### 7.3 UI

- `ImportUrlWindow` and `ImportUrlViewModel`: URL, headers (the existing key and value editor), choice list, progress, error, Next and Cancel (which cancels a running download).
- File > `Import _from URL...`, `MainWindowViewModel.ImportFromUrlCommand`, `IExchangeDialogs.ImportFromUrlAsync`. After the download it shares the preview and apply steps with the file import.
- Settings `import.lastUrl` and `import.lastBaseUrl`, stored after a successful import, user name and password removed from the address.

### 7.4 Tests

`SwaggerPageScanner`, parameterized over page samples (Swagger UI initializer, swagger-ui-express, Swashbuckle `index.js`, springdoc `swagger-config`, FastAPI inline, Redoc); `RemoteImportService` with a fake downloader (direct, page, several choices, fallback paths, limit, nothing found); integration tests on the in-process server (header sent to the same host, not after a redirect to another one, 401, oversize, timeout, cancel); view model and headless window tests.

Manual: a real Swagger UI page on the public Petstore and one local ASP.NET service with Swashbuckle; re-import with Overwrite and an open tab; a wrong address; a self-signed local service with SSL verification on and off.

## 8. Risks

- **Notices completeness:** the build catches a package without a notice, not code bundled inside one. That part rests on reading the upstream projects, again at every upgrade.
- **Swagger UI discovery** depends on how each tool writes its page. It covers the listed tools; the specification address always works as a fallback.
- **Mapping quality** on large real specifications (sample bodies, recursive schemas). The depth limit keeps samples small; real files are part of the manual check.
- **Overwrite** replaces edits made to imported requests. The preview already says "will be overwritten", and open tabs with unsaved edits ask a second time.
- **Focus order** (PR 3 and PR 4) is where headless tests and a real window can differ, so both are also checked by hand.
- **Linux** checks stay limited to WSL2; a full desktop is untested.
- **YAML conversion** is our own code and may meet YAML that the tests did not cover. The decision step and the switch to the official reader limit this.
- **Package size** grows by one library (PR 5), or by three after a switch.
