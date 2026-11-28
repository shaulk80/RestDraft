using RestDraft.UI.Services;

namespace RestDraft.UI.ViewModels;

public sealed class ConfirmationViewModel : ViewModelBase
{
    private readonly ConfirmationRequest _request;

    public ConfirmationViewModel(ConfirmationRequest request)
    {
        _request = request;
    }

    public string Title => _request.Title;

    public string Message => _request.Message;

    public string ConfirmText => _request.ConfirmText;
}
