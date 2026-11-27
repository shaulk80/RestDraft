using Avalonia.Controls;

namespace RestDraft.UI.Views;

public partial class ResponseSearchView : UserControl
{
    public ResponseSearchView()
    {
        InitializeComponent();
    }

    public bool IsQueryFocused => QueryBox.IsFocused;

    // Selects the whole text, so typing replaces it.
    public void FocusQuery()
    {
        // A panel that just opened has no controls on screen until it is laid out.
        UpdateLayout();
        QueryBox.Focus();
        QueryBox.SelectAll();
    }
}
