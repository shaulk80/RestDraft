# New features: requirements

Status (2026-11-26): all questions are answered (section 7). The implementation plan is `new-features-plan.md`. Nothing is implemented yet.

## 1. Requested

1. Create an app icon, with 5 variants to choose from.
2. Track the active item in the tree (optional): activating a tab focuses its request in the tree.
3. Import from Swagger by its URL (UX to be discussed).
4. On "New request", focus an edit box to edit the name.
5. Search text in the response: Ctrl+F, when the response editor has focus, shows a panel above it with a search box; the found text is focused and F3 cycles through the matches, as in a browser.

Added on 2026-11-26:

6. The release packages carry RestDraft's `LICENSE` and `NOTICE` and the notices of every third-party component they ship, and the build fails when one is missing. Details: section 2 of the plan.

## 2. App icon

**Today:** the repository has no icon. The executable, the window, the taskbar and Alt+Tab show defaults.

**Chosen:** variant 3, request and response arrows in a circle (`docs/icons/icon-3-exchange.svg`). The five candidates are at https://claude.ai/artifact/8trPsD7fwcvNBjp9gtuB4C (private).

**Requirements:**

- The Windows executable has it (Explorer, taskbar shortcut), with sizes from 16 to 256 px.
- The main window and the dialogs have it (title bar, taskbar, Alt+Tab), on Windows and Linux.
- The About window shows it, and so does the top of the README.
- Linux gets the window icon only. No `.desktop` file, because there is no installer.
- Only the chosen source stays in the repository; the other candidates are removed.

## 3. Track the active request in the tree

**Today:** activating a tab does not change the selection in the Collections tree.

**Requirements:**

- A check box item in the View menu, off by default and remembered between starts. Turning it on shows the current tab's request at once.
- When it is on and the active tab changes (click, Ctrl+Tab, closing a tab, restore at start), the tree selects the request, expands the folders above it and scrolls it into view. They stay expanded.
- Keyboard focus does not move to the tree.
- A tab that is not saved in a collection leaves the selection as it is.
- A request hidden by the filter leaves the tree and the filter as they are. After the filter changes, the active request is revealed again if it is shown.
- The History tab or a hidden sidebar is not switched or opened. The selection is there when the tree is shown.
- A tree reload for another reason (new folder, rename, import) does not move the selection back to the active tab.
- The reverse direction does not change: selecting in the tree opens a request only by click, double-click or Enter, as today.

Note: the toolbar's "New folder" and "New request" act on the selected node, so with tracking on they act next to the active request.

## 4. Import from a Swagger URL

**Today:** File > Import... reads one Insomnia v5 file and shows a preview (target collection, check box tree, overwrite, warnings). Formats sit behind `ICollectionFormat`.

### 4.1 Flow

Step 1, a new dialog (File > Import from URL...):

```
Import from URL
  URL  [ https://localhost:5001/swagger/index.html            ]  (last used, prefilled)
  > Request headers (optional)
        [ Authorization ] [ Bearer ...                        ]
  Specification [ My API V1                                v  ]  (only when the page offers several)
  Downloading...  / error text
                                              [ Next ]  [ Cancel ]
```

Step 2, the existing preview window with one field added:

```
Import collection
  Import into (o) New collection [ Petstore API ]   ( ) Existing [ v ]
  Base URL    [ https://localhost:5001/api/v3 ]
  [x] pet                     New
      [x] GET   Find pet by ID
      [x] POST  Add a new pet
  [x] store                   New
  [ ] Overwrite existing items
  2 folders, 8 requests ticked: 8 new, 0 exist        > 3 warnings
                          [ Select all ] [ Clear all ] [ Import ] [ Cancel ]
```

A failed download, or a text that is not a specification, keeps the first dialog open with the reason, so the URL can be corrected. Cancel stops a running download.

### 4.2 Requirements

The address:

- The address of a specification, or of a Swagger UI page. For a page the app finds the specification; when the page offers several, the dialog asks which one.
- The address starts with `http://` or `https://`. The download uses the app's timeout, redirect, proxy and SSL verification settings. The size limit is the import limit (20 MB). The download is not added to the History.
- Headers typed in the dialog are used for the download only, are sent only to the host of the typed address (same scheme, host and port), and are never stored. Their values are taken literally (no `{{variables}}`).
- The last address is remembered, without a user name or password in it, together with the base URL used for it.

The preview:

- Base URL: prefilled from the specification, editable, and a variable such as `{{base_url}}` may be typed. When the address is the remembered one, the base URL used last time is prefilled. Import is disabled while it is empty.
- A specification without a server address gets the address it was downloaded from. A relative server address is resolved against it. A file import has no download address, so it gets `{{base_url}}` (followed by the relative path, if any).
- Updating later: import the same address again into the existing collection with "Overwrite existing items". Overwrite replaces a matching request, including edits made to it.

The mapping:

- Swagger 2.0 and OpenAPI 3.0 and 3.1, as JSON or YAML. File > Import... accepts such files too.
- One folder per tag (the first tag of an operation); operations without a tag go to the root.
- Request name: the summary, else the operation id, else `GET /path`. Duplicates get a number.
- Path parameters: the example or default value if the specification has one, else a variable (`/pet/{petId}` becomes `/pet/{{petId}}`). Sending then reports the unresolved variable until it is filled.
- Query and header parameters are all listed: required ones enabled, optional ones disabled, with the example or default value.
- Body: the specification's example, else a sample made from the schema. JSON is preferred when an operation offers several types; URL-encoded and multipart bodies become fields.
- Authentication: Basic, Bearer and API key are set with variables (`{{username}}`, `{{password}}`, `{{token}}`, `{{apiKey}}`). OAuth 2 and OpenID Connect become Bearer `{{token}}` with a warning.
- Descriptions, response definitions, callbacks, webhooks and references to other files are not kept or followed; the warnings say so.
- Export to OpenAPI is out of scope.

## 5. New request: edit the name at once

**Today:** "New request" in the tree (toolbar button or context menu) creates "New request", opens its tab and puts the cursor in the body editor. "New folder" and "New collection" already start a rename in the tree. File > New request (Ctrl+T) opens an unsaved tab; it has no name field, and the name is asked when it is saved.

**Requirements:**

- After "New request" in the tree, the new node is in rename mode with the whole name selected. The tab is open, and nothing else takes the focus from the box.
- Enter saves the name and moves the focus to the URL box. Escape keeps "New request" and moves the focus to the URL box too.
- Leaving the box with the mouse or Tab saves the name and leaves the focus where the user put it.
- The tab header follows the new name.
- A name already used in the folder is refused with the existing message.
- Ctrl+T, "Duplicate" and renaming an existing request are unchanged.

## 6. Search in the response

**Today:** Ctrl+F opens the Collections or History filter from anywhere, the response editor included. The editor library (AvaloniaEdit) has its own find box, which the app never reaches. That box floats over the text, while the request asks for a panel above it.

**Layout:**

```
( ) Pretty  ( ) Raw
Find [ token           ]  3 of 17  [ ^ ] [ v ]  [ ] Match case  [ ] Whole word  [ x ]
+----------------------------------------------------------------+
| { "access_[token]": "..." ,                                    |
```

**Requirements:**

- Ctrl+F opens the panel when the focus is in the response editor or in the panel; elsewhere it opens the filter as today. In the open panel it selects the whole search text.
- A one-line selection in the editor fills the box; otherwise the last text of this tab stays.
- Options "Match case" and "Whole word", both off at first. A whole word has no letter, digit or underscore running on past either end of the match.
- Matches update while typing. All are highlighted; the current one is marked more strongly and scrolled into view. The focus stays in the box.
- The first current match is the first one at or after the cursor.
- Enter and F3 go to the next match, Shift+Enter and Shift+F3 to the previous one, wrapping around. F3 works in the box and in the editor; in the editor with the panel closed it opens the panel with the last text.
- No match shows "0 of 0" and marks the box.
- Escape or the close button hides the panel, removes the highlights and returns to the editor with the current match selected, so it can be copied.
- Each tab keeps its own panel, search text and options while the app runs. A new response, a tab switch and Pretty or Raw search again.
- Only the shown text is searched. For a truncated preview the panel says so.
- The request body editor is out of scope.

## 7. Questions and decisions

Answered on 2026-11-26.

| # | Question | Decision |
|---|---|---|
| Q1 | Which icon? | 3, exchange arrows |
| Q2 | Where is the tracking switch, and its start value? | View menu, off |
| Q3 | What does Ctrl+T do? | As today |
| Q4 | Where does the focus go after Enter or Escape in the new name? | URL box |
| Q5 | Search options? | Match case and whole word, both off by default |
| Q6 | Entry point for the URL import? | New item File > Import from URL... |
| Q7 | Which addresses are accepted? | A specification, and also a Swagger UI page |
| Q8 | How are request URLs written? | With the base URL from the specification, editable in the preview |
| Q9 | Protected specifications? | Optional request headers in the dialog |
| Q10 | Updating a collection later? | The last address is remembered; import again with Overwrite |

Added while planning, not asked: the base URL is remembered with the address, a file import without a server address gets `{{base_url}}`, headers go only to the typed host and are literal, and references to other files are not followed (section 4.2).
