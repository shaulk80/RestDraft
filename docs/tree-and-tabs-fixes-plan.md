# Collections tree and request tabs: issues and fix plan

Status: draft (2026-11-24), nothing implemented. Found by hand on version 0.3.0. The screenshots are `C:\Vibe\z_tmp\scrollbar.png` and `C:\Vibe\z_tmp\indent.png` (not in the repository).

## 1. Issues

| # | Issue | Kind | Area |
|---|---|---|---|
| 1 | The numpad keys `-`, `/`, `*` typed into the rename box of a tree node act on the tree instead. | Bug | `CollectionsTreeView` |
| 2 | Request tabs cannot be reordered by dragging. | Feature | Tab strip in `MainWindow.axaml` |
| 3 | Hover and selection of a tree row run under the scrollbar while the scrollbar is in its thin (collapsed) state. | Bug (look) | Tree, and any scrolling list |
| 4 | A folder or collection without content gets no indent: it sits one level too far left, and its own children as well. | Bug (look) | Tree item template |
| 5 | Folders cannot be reordered: a folder dropped on a folder always becomes its subfolder. | Feature | Tree drag and drop |

## 2. Findings per issue

### 2.1 Numpad keys in the rename box (issue 1)

The rename `TextBox` lives inside a `TreeViewItem`, so every key press bubbles up to the `TreeView`. The tree most likely treats `Add`, `Subtract`, `Multiply` and `Divide` as expand and collapse keys and marks the event handled. A handled key press never becomes text input, so the box does not get the character. This is the same cause as the Delete key (an existing workaround in `CollectionsTreeView.OnKeyDown` skips `TextBox` sources). That handler runs in the tunnel phase above the tree, so it cannot stop the tree's own bubble handler without marking the event handled, which would also swallow the text.

Not verified yet: the exact list of keys the Avalonia 12.1.4 `TreeView` takes. Left, Right, Up, Down, Home, End and type-to-search are probably affected the same way, and the plan covers them (the expectation is "while an editor is active, all keys are the editor's").

### 2.2 Tab reorder (issue 2)

`TabStripViewModel.Tabs` is an `ObservableCollection<RequestTabViewModel>` shown by a `TabControl`; Avalonia has no built-in reorder. The pieces that already work for a reorder:

- The open tabs are stored with a `position` column (`SqliteOpenTabsStore`, `ORDER BY position`), and `OnTabsChanged` schedules an autosave on every collection change, a move included. The new order therefore survives a restart with no store change.
- `RecentTabs.Add` ignores a tab it already has, so the `Move` notification does not disturb the Ctrl+Tab order.

Risk: a `Move` in the bound collection can reset the `TabControl` selection. The view model must put `SelectedTab` back after a move.

### 2.3 Scrollbar over the rows (issue 3)

The Fluent scrollbar auto-hides: it is a thin overlay over the content, so the content (the row highlight) spans the full width and the scrollbar draws on top of it. When the pointer comes near, the bar expands and takes its own space, as in the lower screenshot, which the reporter prefers. The app already turns auto-hide off for the code editors (`AppStyles.axaml:83`); the tree and the history list (`ListBox` in `HistoryListView.axaml`) have no such rule.

### 2.4 Indent of empty folders (issue 4)

Measured on `indent.png` (icon left edge): Collection1 68, `Local Tests` 84, `f5` 84, `f1` to `f4` 68, `f5_sub` 84. Each level adds 16 px, so `f1` to `f4` are drawn exactly one level too far left, and `f5_sub` (a child of `f5`) lines up with its parent. The pattern points to the expander: Fluent hides the chevron of an item without children (the `:empty` state), and with it the cell that the chevron takes in the layout, so a leaf loses one chevron width. Requests are leaves too, so they have the same offset (request text lines up with the icons of empty folders).

So the wrong rows are the empty ones (`f1` to `f4`, `f5_sub`), not `f5`: they move 16 px right. The result matches what the reporter expects: `f4` and `f5` at the same indent, and `f5_sub` one level below `f5`. Requests move with them, which also fixes their alignment with sibling folders. To be confirmed with a measurement in a headless test before the change.

### 2.5 Folder reorder (issue 5)

- `FolderNodeViewModel.DropBeforeId` is always `null` and `DropFolderId` is the folder itself, so a drop on a folder always means "append into this folder". A drop on a request means "insert before this request". There is no way to say "after" or "between two folders", and no drop indicator is drawn.
- The domain already supports the order: `CollectionTreeService.MoveAsync(nodeId, targetFolderId, beforeNodeId)` and `FolderTree.InsertFolder(..., beforeId)` accept a folder id as the sibling. Only the view model and the view are missing.
- A folder stores `Folders` and `Requests` as two lists, folders first. A folder can never sit between requests, and a request never above a folder. Drop positions must be mapped to that rule (section 3.5).
- `FolderNodeViewModel` does not know its parent folder (a request does, as `_parentFolderId`), which "before" and "after" need.
- Collections cannot be moved at all today ("Collections cannot be moved." in `MoveAsync`). Reordering collections is not asked for and stays out of scope.

## 3. Plan

Two pull requests, because the first is small visual and key handling work and the second is the drag and drop work that shares one helper.

### 3.1 Pull request A: tree keys and looks (issues 1, 3, 4)

Each fix starts with a failing headless test in `RestDraft.UI.Tests` (`CollectionsTreeViewInteractionTests`), then the change.

1. Issue 1. Add a `TreeView` subclass (one file) that overrides `OnKeyDown` and returns when `e.Source` is a `TextBox`, and use it in `CollectionsTreeView.axaml`. Then the existing Delete special case in `CollectionsTreeView.OnKeyDown` can use the same rule. Test, parameterized: with a node in rename mode, send `NumPadSubtract`, `NumPadDivide`, `NumPadMultiply`, `NumPadAdd`, plus Home, End, Left, Right (key plus the key text) and assert the box text or caret changed and the tree's expanded state did not.
2. Issue 3. Add a style in `AppStyles.axaml`, next to the editor rule, that sets `AllowAutoHide` to `False` on the `ScrollViewer` inside `TreeView` and `ListBox` (the sidebar tree and the history). Test: fill the tree beyond its height, then assert the right edge of a row is at or left of the scrollbar's left edge.
3. Issue 4. Keep the expander cell for items without children: style `TreeViewItem:empty /template/ ToggleButton#PART_ExpandCollapseChevron` to `IsVisible` true, `Opacity` 0, `IsHitTestVisible` false (a click there then selects the row). Test, parameterized: build a tree with an empty folder, a non-empty folder and a request as siblings; assert the same icon or text x for the siblings and a larger x for the child of an expanded folder, whether the folder is empty or not. If the measurement in section 2.4 turns out different (for example the indent comes from the item level), fix the real cause and keep the test.

Manual check before reporting done: run the app, look at the tree with many folders (the `indent.png` and `scrollbar.png` cases), type `-` `/` `*` in a rename box.

### 3.2 Pull request B: drag and drop (issues 2, 5)

Shared helper: the tree already has press, threshold, `DoDragDropAsync` code in `CollectionsTreeView.axaml.cs`. Move it into one small class (for example `DragStarter`) and use it for the tree and the tab strip, so the logic exists once.

Tab reorder (issue 2):

1. `TabStripViewModel.MoveTab(RequestTabViewModel tab, int newIndex)`: calls `Tabs.Move`, then sets `SelectedTab` back if it changed. A no-op for an equal or out of range index (a result or a silent return, no exception).
2. A behavior on the tab strip `TabControl` (attached to it in `MainWindow.axaml`): a press on a tab header starts the drag, except a press on the close button. During the drag, an insertion marker shows on the left or right edge of the header under the pointer; a drop calls `MoveTab`. A tab dropped on itself or on the neighbouring edge is a no-op.
3. Tests: unit tests for `MoveTab` (parameterized: first to last, last to first, same index, out of range; selection kept; autosave scheduled and the stored order matches). A headless test that drags a header onto another and asserts the new order.

Folder reorder (issue 5):

1. Add `DropPosition` (`Before`, `Inside`, `After`) as an enum in its own file. The view computes it from the pointer's vertical position in the target row: for a folder, the top quarter is `Before`, the bottom quarter `After`, the middle `Inside`; for a request, the top half is `Before`, the bottom half `After`; a collection is always `Inside`. A collapsed or empty folder behaves the same (after a collapsed folder means after it, not inside).
2. `CollectionsTreeViewModel.MoveAsync(source, target, position)` turns the position into `(targetFolderId, beforeNodeId)`:
   - `Inside`: the target folder, append (as now).
   - `Before` a folder or request: the target's parent, before the target.
   - `After`: the target's parent, before the next sibling of the target, or append when it is the last.
   - Folder-first rule: a folder dropped relative to a request goes last among the folders (`beforeNodeId` null); a request dropped relative to a folder goes first among the requests.
   - Dropping a node before or after itself, or to the position it already has, does nothing and does not reload the tree.
   - `FolderNodeViewModel` gets its parent folder id, so the target's parent and the next sibling are known (the view model reads them from the loaded tree).
3. A drop indicator: a thin line above or below the row for `Before` and `After`, and the row highlight for `Inside`. It is drawn by the view during `DragOver`, so the user sees what a drop will do (today there is no feedback). The cursor shows "not allowed" for a drop into the dragged folder's own subtree.
4. Keep the server rules: a folder cannot move into itself, and a name that already exists in the new parent is an error shown in the status line.
5. Tests: unit tests for the position to (folder, before) mapping, parameterized over target kind and position, including last sibling, only child, collapsed folder, and the folder-first rule. An application test that moving a folder before and after a sibling folder keeps the order after a reload. A headless drag test: dropping at the top edge of a folder reorders, dropping in the middle nests.

Manual check before reporting done: reorder folders and requests, nest and un-nest, drag a tab and restart the app to see the order kept.

## 4. Decisions made here (change if wrong)

- Scrollbars: auto-hide off for the tree and the history list only, not app-wide. The other scroll areas (request and response panels, editors) are not mentioned in the report; app-wide is one more selector if wanted.
- Collections stay immovable (issue 5 is about folders).
- Dropping on the middle of a folder keeps its current meaning (nest), so nothing existing changes for the people who nest by dragging.
- Version: both pull requests are fixes and small features; a `0.3.1` or `0.4.0` release is for the owner to decide, `RELEASE_NOTES.md` is updated when that is decided.

## 5. Out of scope

Reordering collections, drag between the History tab and the tree, keyboard reorder (Alt+Up and Alt+Down) for tabs or tree nodes, and a tab context menu with "Move left" and "Move right". Each is a possible follow-up.
