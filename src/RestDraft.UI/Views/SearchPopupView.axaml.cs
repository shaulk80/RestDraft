using Avalonia.Controls;

namespace RestDraft.UI.Views;

public partial class SearchPopupView : UserControl
{
    public SearchPopupView()
    {
        InitializeComponent();
    }

    public void FocusText()
    {
        TextBox.Focus();
        TextBox.SelectAll();
    }
}
