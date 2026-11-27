using Avalonia.Controls;

namespace RestDraft.UI.Views;

public partial class RequestBarView : UserControl
{
    public RequestBarView()
    {
        InitializeComponent();
    }

    public void FocusUrl()
    {
        UrlBox.Focus();
    }
}
