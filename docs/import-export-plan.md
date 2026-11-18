# Import and Export: plan

Status: plan agreed in conversation, 2026-11-18. Nothing is implemented. All questions are answered (section 11).

## 1. Goal

Add `File > Import...` and `File > Export...`. The first supported format is the Insomnia v5 YAML collection (`type: collection.insomnia.rest/5.0`), for example `C:\Vibe\z_tmp\AITestRequests.yaml` and `requests_v2.yaml`. The format sits behind an interface so another format (Insomnia v4 JSON, Postman, OpenAPI) can be added later without touching the dialogs or the merge logic.

As in Insomnia, one file holds one collection. Import reads one file into either a new collection or an existing one (with an option to overwrite items that already exist). Export writes one collection, or a chosen part of it, to one file.

Import: pick a file, then a preview with a checkbox tree, a target selector, an overwrite option, and Import and Cancel buttons. Export: pick the collection, tick folders and requests in a tree (parent and children linked), then Export and Cancel, then choose where to save.

## 2. What the sample files show

Both samples are one Insomnia workspace ("Scratch Pad"). `requests_v2.yaml` has two top-level folders ("AI - local", "Kibana - test") under `collection:`; the first has one. The structure is:

```
type, schema_version, name, meta{id, created, modified, description}
collection:                          # the root: a list of folders and requests mixed
  - name, meta{id, created, modified, sortKey, description}, children: [...]     # folder
  - name, url, method, meta{..., isPrivate}, headers[], body{mimeType, text}, settings{...}   # request
```

Folders nest to any depth through `children`. A file can also hold `environments`, `cookieJar` and other top-level keys; the samples have none. Order in Insomnia's UI comes from `meta.sortKey`, not from the file order, so the reader sorts siblings by `sortKey` (stable, missing key last). A body can hold a large base64 image (the first sample has about 20 KB), so a size limit applies (section 8).

## 3. Mapping between Insomnia and RestDraft

| Insomnia v5 | RestDraft | Notes |
|---|---|---|
| top-level `name` | `Collection.Name` | The default name of a new collection on import; on export the collection's name. |
| `collection:` entries | children of `Collection.Root` | The root folder has an empty name in RestDraft. Loose requests and top-level folders both become root children. |
| folder (has `children`) | `Folder` | Nested the same way. |
| request | `RequestDefinition` | |
| `method` | `RequestMethod` | The enum has 7 values. Another method (TRACE, CONNECT, custom) skips the request and adds a warning. |
| `url` | `Url` | |
| `parameters[]` (name, value, disabled) | `QueryParams` | `disabled: true` maps to `IsEnabled = false`. |
| `headers[]` | `Headers` | Same. |
| `body.mimeType` json, xml, text, yaml, html, graphql | `RawBody(ContentType, Text)` | GraphQL text is already a JSON string, so it imports as `application/json`. |
| `body.mimeType: application/x-www-form-urlencoded` + `params[]` | `FormUrlEncodedBody` | |
| `body.mimeType: multipart/form-data` + `params[]` | `MultipartBody` | `type: file` with `fileName` becomes `FilePath`; the path is kept as is. |
| `body` with a file (`application/octet-stream`) | not supported | Body becomes `NoBody` and a warning is added. |
| `authentication` basic, bearer, apikey | `BasicAuth`, `BearerAuth`, `ApiKeyAuth` | `addTo: header` or `queryParams` maps to `ApiKeyLocation`. `disabled: true` maps to `NoAuth`. |
| other auth (OAuth 2, digest, AWS, NTLM, ...) | `NoAuth` | A warning is added. |
| folder-level `headers` and `authentication` | dropped | RestDraft folders have neither. One warning for the whole file, only if any folder has them ("Some folders have headers or authentication that RestDraft does not keep"). |
| `{{ _.name }}` | `{{name}}` | Converted in URL, headers, params, body and auth. Reverse on export. Insomnia tags (`{% ... %}`, `{{ _.a.b }}`) cannot be converted: kept as text and listed as a warning. |
| `meta.description`, `settings`, `scripts`, `meta.isPrivate`, `cookieJar`, `environments` | dropped | One summary warning per kind in the preview ("12 requests have settings that RestDraft does not keep"). |
| `meta.id`, `created`, `modified` | not read | New ids come from `IIdGenerator`. On export the writer makes Insomnia-style ids (`fld_`/`req_`/`wrk_` plus 32 hex digits from a GUID) and sets `created`/`modified` to the export time. |

Export is the reverse. Fields that Insomnia needs and RestDraft lacks get defaults (`settings` with the values seen in the samples, `description: ""`, `isPrivate: false`, `sortKey` ascending in tree order). The header `User-Agent` is not added.

Name rules differ: Insomnia allows two siblings with the same name, RestDraft does not (`ChildNameRules`, case-insensitive across folders and requests). The reader makes names unique with `ChildNameRules.NextAvailableName` ("Name 2") and warns. An empty name becomes "Untitled".

## 4. Behavior

### 4.1 Import

1. `File > Import...` opens the file dialog (filter: `*.yaml;*.yml`, plus "All files").
2. The file is read and parsed off the UI thread. Each registered format tries the file (`CanRead`, based on the `type` key); if none does, a message names the problem ("Not an Insomnia v5 collection. Insomnia v4 JSON is not supported yet").
3. A preview window opens:
   - **Target** at the top: "New collection" with a name box (default: the file's `name`, made unique among the existing collections with a number if needed), or "Existing collection" with a list of the current collections (preselected: the one selected in the sidebar tree, if any).
   - **Tree** of what the file contains, with a checkbox on every node (the same tri-state tree as the export, all ticked at the start) and a status on each node: New, or Exists when the target is an existing collection and an item with that name is already there.
   - **Overwrite existing items** checkbox, enabled only when the target is an existing collection.
   - A summary line ("3 folders, 12 requests ticked: 9 new, 3 exist"), a warnings list (expandable), Select all and Clear all, and Import and Cancel buttons. Import is disabled when nothing ticked would change, or when the new collection name is empty.
   - Changing the target, the overwrite checkbox or any tick recomputes the statuses and the summary in place.
4. Import applies the ticked part of the plan (unticked items are left out; a ticked request brings its parent folders) in one `Workspace.UpdateAsync` call, so the whole import is saved at once or not at all. The tree reloads, the target collection is expanded and selected, and the message line says "Imported 9 new and 3 overwritten requests into 'Scratch Pad'; 2 warnings".

Matching (existing target only): a folder matches by name inside the same parent, a request by name inside the same folder (case-insensitive, trimmed, the same rule as `ChildNameRules`). A folder and a request never match each other. With a new collection nothing can match.

Overwrite unchecked: a matching item is left alone; a matching folder is still entered, so new requests inside it are added. Overwrite checked: a matching request is replaced by the file's version and keeps its id, so history links and open tabs keep working; a matching folder is merged (its other children stay). Nothing is ever deleted by an import.

### 4.2 Export

1. `File > Export...` opens a selection window. At the top a list picks the collection (preselected: the one selected in the sidebar tree, else the first). Below it is the tree of that collection's folders and requests, with a checkbox on every node and the collection itself as the root node, all ticked at the start. A count ("5 folders, 14 requests selected"), Select all and Clear all, and Export and Cancel buttons complete it. Export is disabled while nothing is selected. Changing the collection rebuilds the tree.
2. Checking a parent checks all its descendants; unchecking does the reverse. A parent whose children are partly checked shows the indeterminate state. A parent turns checked when all its children are checked and unchecked when none are.
3. Export asks for the save location (`SaveFilePicker`, default name `<collection name>.yaml`), writes the file, and reports in the message line ("Exported 14 requests to ...").

Selection to output: a selected request always brings its ancestor folders; a selected folder brings its whole subtree; the output is the collection pruned from a copy. An empty selected folder is exported as an empty folder. The export dialog shows a one-line notice when any selected request has auth credentials: "Basic auth passwords and tokens are written to the file in plain text." RestDraft keeps them in plain text in its own database too, so this is a warning only.

## 5. Design

Layering follows `CLAUDE.md`: Domain has no references, Application holds logic and interfaces, Infrastructure holds the format and file code, UI holds windows and view models.

### 5.1 Application (`RestDraft.Application/Exchange`)

- `ICollectionFormat`: `Id`, `DisplayName`, `FileExtensions`, `Result<bool> CanRead(string text)`, `Result<ExchangeDocument> Read(string text)`, `Result<string> Write(Collection collection)`. `ExchangeDocument` is a record: one `Collection` plus `IReadOnlyList<ImportWarning>`.
- `ImportWarning`: record (message, path such as `Ollama / Prompt - ollama`).
- `ImportTarget`: record, either a new collection name or an existing collection id.
- `ImportPlanner` (pure): takes the existing collections, the document, the target, the overwrite flag and the ticked ids; returns an `ImportPlan` holding the merged result, a `PlanNode` tree (name, kind, status New / Exists / Overwritten / Skipped, children) for the preview, and the counts. The preview and the apply step use the same plan, so what the user sees is what is applied.
- `ImportService`: `ReadAsync(path)` returns the document; `ApplyAsync(document, target, overwrite, ticked)` runs the planner inside `Workspace.UpdateAsync` and returns the counts and the ids of overwritten requests. Files are read through the existing `IFileReader` plus a new `IFileWriter` for export.
- `ExportService`: `ExportAsync(collectionId, ticked)`. `SelectionPruner` (pure) builds the pruned collection from the ticked ids; the import uses the same pruner on the file's collection.
- Name uniqueness and `{{ _.x }}` conversion live in one small `InsomniaVariableSyntax` helper next to the format, not in the dialogs.

### 5.2 Infrastructure (`RestDraft.Infrastructure/Exchange/Insomnia`)

- `InsomniaV5Format : ICollectionFormat`.
- DTOs for the file (`InsomniaCollectionDto`, `InsomniaFolderDto`, `InsomniaRequestDto`, ...) in their own files, one type per file, and mappers to and from the domain, like `Persistence/Mappers`.
- YAML library: YamlDotNet 18.1.0 (MIT, passes `LicenseCheck`). Checked on NuGet on 2026-11-18: the package has no dependencies for `net10.0` (the other target frameworks list none either), so it adds one DLL of about 0.3 MB and nothing transitive. It is a new third-party dependency, so it gets a line in `ARCHITECTURE_PLAN.md` and the pinned version goes in `Directory.Packages.props`. Reader and writer use it only through the DTOs so the library stays in one folder. The exact version is checked again at implementation time.
- `FileWriter : IFileWriter` (atomic write: temp file in the target folder, then move).
- Registered in `InfrastructureServiceCollectionExtensions`; formats are injected as `IEnumerable<ICollectionFormat>`.

### 5.3 UI

- Menu: in `MainWindow.axaml`, `_Import...` and `_Export...` after `Close tab`, in their own group between two separators (before `Se_ttings...`). Commands `ImportCommand` and `ExportCommand` on `MainWindowViewModel`, handled like `SettingsCommand` through a new `IExchangeDialogs` (`ImportAsync`, `ExportAsync`) so the view model stays testable. Export is disabled when there are no collections.
- File dialogs: a new `IExchangeFilePicker` (open with filter, save with suggested name) built on `StorageProvider`. `IFilePickerService` (multipart file picker) stays as is.
- `SelectableNodeViewModel`: the shared tri-state node (title, kind, icon, `IsChecked` bool?, children, parent, optional status text). All the checkbox propagation lives here and has no Avalonia dependency, so it is unit tested directly. It is used by both the export tree and the import preview tree.
- `ExportSelectionWindow` + `ExportSelectionViewModel`; `ImportPreviewWindow` + `ImportPreviewViewModel` (a `SelectableNodeViewModel` tree built from the `PlanNode` tree, target selector, overwrite checkbox, warnings list). The ticked ids go to the same `SelectionPruner` that the export uses.
- Both windows follow the existing pattern (`SaveDestinationWindow`: `ShowDialog`, `IsDefault`/`IsCancel` buttons, `InitialFocus`, `AutomationProperties.Name` on every control, keyboard operation: Space toggles a node, Left/Right collapse and expand, Enter confirms).
- After an import with overwrite, tabs showing an overwritten request must not keep stale content. The apply step returns the ids of overwritten requests; the tab strip reloads tabs without unsaved edits from the service. When a tab with unsaved edits is affected, the preview lists "N open tabs have unsaved edits that will be replaced" and Import asks once more before applying.

### 5.4 Errors

Parsing runs on a background thread with the `CancellationToken` of the dialog. Every failure (unreadable file, bad YAML, wrong type, no permission to write) is a `Result` error shown in the message line or in the preview; nothing is thrown for bad input. Deliberate throws: none planned.

## 6. Testing

- Application.Tests: `ImportPlanner` (parameterized: new collection, existing target without and with overwrite, nested folder merge, request vs folder with the same name, case differences, duplicate names inside the file, unticked items left out), `SelectionPruner` (request only, folder only, nothing, whole collection, empty folder), `ImportService` against a fake repository (one save call, nothing saved on failure).
- Infrastructure.Tests: `InsomniaV5Format` read (sortKey ordering, every body type, every auth type, disabled flags, unsupported method, unsupported auth, variable conversion, duplicate names, folder-level headers giving one warning, wrong `type`, broken YAML, empty file), write, and a round trip (RestDraft to YAML to RestDraft gives the same collection except ids). Fixtures are small synthetic YAML files modeled on the samples; the image body of the sample is not copied and nothing from the personal files is committed.
- UI.Tests: `SelectableNodeViewModel` (parameterized: check parent, uncheck one child, check last child, clear all), `ImportPreviewViewModel` (target change and overwrite toggle recompute statuses, unticking an item removes it from the plan, Import disabled when nothing ticked would change or the new name is empty), `ExportSelectionViewModel` (count, disabled when empty, collection change rebuilds the tree), menu command availability. A headless window test opens both windows and clicks Cancel and Confirm. No tests for log messages and no reflection.
- Integration.Tests: import a fixture into the real SQLite repository, reload the repository, and check the tree; export again and compare.
- Manual check before the pull request is ready (Definition of Done): run the app; import `AITestRequests.yaml` as a new collection; import `requests_v2.yaml` into that collection without and with overwrite; send one imported request to the local sample API; export a part of the tree; import the exported file into a fresh data folder; open the exported file in Insomnia if available. The summary will say what was exercised and what was not (Linux file dialogs, Insomnia itself).

## 7. Work breakdown

One branch, `feat/import-export`, one pull request, commits per logical change:

1. `feat(exchange)`: Application contracts, `ImportPlanner`, `SelectionPruner`, services, tests.
2. `feat(exchange)`: YamlDotNet dependency, `InsomniaV5Format` reader, writer, mappers, fixtures, tests.
3. `feat(ui)`: `SelectableNodeViewModel`, export window and command, file pickers.
4. `feat(ui)`: import preview window and command, tab refresh after overwrite.
5. `docs`: README (feature and supported format), `RELEASE_NOTES.md`, `STATUS.md`, the dependency line in `ARCHITECTURE_PLAN.md`, a Linux file-dialog check in the portability checklist.

Steps 1 and 2 can be reviewed without any UI; 3 and 4 need the services.

## 8. Risks and limits

- Large or hostile files: a size limit (default 20 MB, message "The file is larger than 20 MB") and a nesting depth limit (64). YAML aliases are rejected on read; Insomnia exports do not use them.
- Data loss in translation: description, scripts, settings, cookies, environments and folder-level headers and auth are dropped. The preview lists them before the user confirms; the export writes only what RestDraft has.
- Tabs and stale content after an overwrite (5.3). The riskiest part for the user's work; it gets its own test.
- Plain-text credentials in exported files (4.2): a notice, no encryption.
- Linux: file dialogs under WSLg and other desktops are untested; the portability checklist gets a line for them.
- The Insomnia v5 format is young (`schema_version: "5.1"` in the samples). The reader accepts any 5.x and ignores unknown keys, so a newer minor version still imports.
- The package gets larger by one library (YamlDotNet, about 0.3 MB).

## 9. Out of scope

Several collections in one file (Insomnia does not do it either). Environments, secrets, history and cookies (not exported or imported). Import from a URL or the clipboard. Drag and drop of a file onto the window. Matching by id (matching is by name). Postman, OpenAPI, curl and Insomnia v4 JSON (the interface allows them later). Command-line import and export.

## 10. Decisions

- One collection per file, as in Insomnia. Import goes to a new collection or to an existing one; export takes one collection.
- Matching is by name, not by Insomnia id.
- Nothing is deleted by an import. Without overwrite a matching item is skipped; with overwrite its content is replaced and its id kept.
- Unsupported pieces are dropped with a warning, never a hard failure; a request with an unsupported method is skipped. Folder-level headers and authentication give one warning for the file.
- Variables are converted between `{{ _.x }}` and `{{x}}`.
- Both dialogs use a checkbox tree; unticked items are not imported or exported.
- The menu items are plain `Import...` and `Export...` in the File menu, not a submenu.
- YamlDotNet 18.1.0 as the YAML library (MIT, no dependencies on net10.0).
- Test fixtures are synthetic.

## 11. Questions

All answered on 2026-11-18:

- Q1. One collection per file (Insomnia does the same); import into a new or an existing collection, export one collection.
- Q2. Without overwrite, skip the matching item (matching folders are still entered).
- Q3. After an overwrite, reload tabs without unsaved edits; warn and ask again when a tab has unsaved edits.
- Q4. The import preview tree has checkboxes.
- Q5. Insomnia v4 JSON is a later change.
- Q6. YamlDotNet, checked for dependencies.
- Q7. Folder-level headers and authentication are ignored, with one warning that they exist in the source.
- Q8. Synthetic test fixtures.
- Q9. The second sample is one workspace with two top-level folders; no multi-collection layout exists.
