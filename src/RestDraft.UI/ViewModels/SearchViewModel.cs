using System.Windows.Input;
using RestDraft.Application.Search;
using RestDraft.Domain.Collections;
using RestDraft.UI.Commands;

namespace RestDraft.UI.ViewModels;

// The controls of the search popup, for one list (the collections tree or the history). The boxes are edited freely.
// Search applies them: only then does the list change, and Filter and IsActive say what is applied.
public sealed class SearchViewModel : ViewModelBase
{
    public const string AnyMethod = "Any method";

    private readonly Func<Task> _applyToList;
    private readonly AsyncRelayCommand _searchCommand;
    private readonly AsyncRelayCommand _clearCommand;
    private string _text = string.Empty;
    private string _selectedMethod = AnyMethod;
    private bool _searchInBodies;
    private RequestFilter _filter = RequestFilter.None;

    public SearchViewModel(Func<Task> applyToList)
    {
        _applyToList = applyToList;
        _searchCommand = new AsyncRelayCommand(ApplyAsync, () => true);
        _clearCommand = new AsyncRelayCommand(ClearAsync, () => IsActive);
    }

    // Raised after the list has taken the new filter, so the popup can close.
    public event EventHandler? Applied;

    public IReadOnlyList<string> MethodChoices { get; } = [AnyMethod, .. Enum.GetNames<RequestMethod>()];

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public string SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public bool SearchInBodies
    {
        get => _searchInBodies;
        set => SetProperty(ref _searchInBodies, value);
    }

    // The applied filter, not the text in the boxes.
    public RequestFilter Filter
    {
        get => _filter;
        private set
        {
            if (SetProperty(ref _filter, value))
            {
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(Summary));
                _clearCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsActive => Filter.IsActive;

    // The tooltip of the button that opens the popup.
    public string Summary => IsActive ? $"Filter on: {Describe(Filter)}" : "Filter";

    public ICommand SearchCommand => _searchCommand;

    // Needs an applied filter. It empties the boxes and shows the whole list again.
    public ICommand ClearCommand => _clearCommand;

    public async Task ApplyAsync()
    {
        Filter = CreateFilter();
        await _applyToList();
        Applied?.Invoke(this, EventArgs.Empty);
    }

    private async Task ClearAsync()
    {
        Text = string.Empty;
        SelectedMethod = AnyMethod;
        SearchInBodies = false;
        await ApplyAsync();
    }

    private RequestFilter CreateFilter()
    {
        var method = SelectedMethod == AnyMethod ? (RequestMethod?)null : Enum.Parse<RequestMethod>(SelectedMethod);
        return new RequestFilter(Text.Trim(), method, SearchInBodies);
    }

    private static string Describe(RequestFilter filter)
    {
        var parts = new List<string>();
        if (filter.Text.Length > 0)
        {
            parts.Add($"\"{filter.Text}\"");
        }

        if (filter.Method is { } method)
        {
            parts.Add(method.ToString().ToUpperInvariant());
        }

        if (filter.InBodies && filter.Text.Length > 0)
        {
            parts.Add("also in bodies");
        }

        return string.Join(", ", parts);
    }
}
