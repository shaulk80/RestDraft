using Avalonia.Controls;
using Avalonia.Input;

namespace RestDraft.UI.Controls;

// A tree whose rows can hold an in-place editor. A key press that comes from a text box inside a row bubbles up to the
// tree, which would take Home, End, the arrows and the numpad + - * / for expanding, collapsing and moving, and mark the
// key handled, so the box never receives it. While an editor has the focus, all keys are the editor's.
public sealed class EditAwareTreeView : TreeView
{
    // The theme of the tree is found by the control type, so this type uses the theme of TreeView.
    protected override Type StyleKeyOverride => typeof(TreeView);

    // The rows are created here, also for the nested levels: a row asks its tree for the container of its children.
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new EditAwareTreeViewItem();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        return NeedsContainer<EditAwareTreeViewItem>(item, out recycleKey);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source is TextBox)
        {
            return;
        }

        base.OnKeyDown(e);
    }
}
