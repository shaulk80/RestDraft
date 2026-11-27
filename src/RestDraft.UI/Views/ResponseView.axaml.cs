using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RestDraft.UI.Controls;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Views;

public partial class ResponseView : UserControl
{
    private readonly EditorSearchConnector _connector;

    public ResponseView()
    {
        InitializeComponent();
        _connector = new EditorSearchConnector(BodyViewer);
        // Taken before the controls inside see them, because the editor leaves on Escape by itself.
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private ResponseSearchViewModel? Search => (DataContext as RequestTabViewModel)?.Response.Search;

    private bool IsSearchTarget => BodyViewer.IsKeyboardFocusWithin || FindPanel.IsKeyboardFocusWithin;

    // Ctrl+F belongs to the search in the response only while the focus is in the body or in the search panel.
    public bool TryOpenSearch()
    {
        if (!IsSearchTarget)
        {
            return false;
        }

        // In the panel, the search text is only selected again.
        if (BodyViewer.IsKeyboardFocusWithin)
        {
            _connector.OpenFromSelection();
        }

        FindPanel.FocusQuery();
        return true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _connector.Connect(Search);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        var isPlainOrShift = e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift;
        if (Search is not { } search || !IsSearchTarget || !isPlainOrShift)
        {
            return;
        }

        var backwards = e.KeyModifiers == KeyModifiers.Shift;
        switch (e.Key)
        {
            case Key.F3:
                e.Handled = true;
                FindAgain(search, backwards);
                break;
            case Key.Enter when FindPanel.IsQueryFocused:
                e.Handled = true;
                Step(search, backwards);
                break;
            case Key.Escape when search.IsOpen && !backwards:
                e.Handled = true;
                search.Close();
                break;
        }
    }

    // With the panel closed, F3 opens it with the text searched last, at the cursor.
    private void FindAgain(ResponseSearchViewModel search, bool backwards)
    {
        if (search.IsOpen)
        {
            Step(search, backwards);
            return;
        }

        _connector.OpenAtCursor();
        // The match at the cursor is the next one. The previous one is the match before it.
        if (backwards)
        {
            search.SelectPrevious();
        }

        FindPanel.FocusQuery();
    }

    private static void Step(ResponseSearchViewModel search, bool backwards)
    {
        if (backwards)
        {
            search.SelectPrevious();
            return;
        }

        search.SelectNext();
    }
}
