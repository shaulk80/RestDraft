using Avalonia.Controls;
using Avalonia.Input;

namespace RestDraft.UI.Controls;

// A row of EditAwareTreeView. The row handles the keys that expand and collapse it, so a key press from a text box
// inside the row must not reach that code: while an editor has the focus, all keys are the editor's.
public sealed class EditAwareTreeViewItem : TreeViewItem
{
    // Themes and style selectors are found by the control type, so this type is styled as a TreeViewItem.
    protected override Type StyleKeyOverride => typeof(TreeViewItem);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source is TextBox)
        {
            return;
        }

        base.OnKeyDown(e);
    }
}
