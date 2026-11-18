using System.Windows.Input;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.UI.Commands;
using RestDraft.UI.Services;

namespace RestDraft.UI.ViewModels;

// The export window: which collection, and which of its folders and requests.
public sealed class ExportSelectionViewModel : ViewModelBase
{
    private readonly IReadOnlyList<Collection> _collections;
    private readonly RelayCommand _selectAllCommand;
    private readonly RelayCommand _clearAllCommand;
    private CollectionChoice? _selectedCollection;
    private string _summary = string.Empty;
    private bool _hasCredentials;
    private bool _canExport;

    public ExportSelectionViewModel(IReadOnlyList<Collection> collections, Guid? preselectedCollectionId)
    {
        _collections = collections;
        Collections = collections.Select(collection => new CollectionChoice(collection.Id, collection.Name)).ToList();
        _selectAllCommand = new RelayCommand(Tree.SelectAll, () => true);
        _clearAllCommand = new RelayCommand(Tree.ClearAll, () => true);
        Tree.Changed += OnTreeChanged;
        SelectedCollection = Collections.FirstOrDefault(choice => choice.Id == preselectedCollectionId) ?? (Collections.Count > 0 ? Collections[0] : null);
    }

    public IReadOnlyList<CollectionChoice> Collections { get; }

    public SelectionTreeViewModel Tree { get; } = new();

    public CollectionChoice? SelectedCollection
    {
        get => _selectedCollection;
        set
        {
            if (SetProperty(ref _selectedCollection, value) && value is not null)
            {
                Tree.Show(SelectableNodeFactory.FromCollection(CollectionOf(value)));
            }
        }
    }

    // For example "5 folders, 14 requests selected". A folder that is not ticked but holds a ticked request is counted too,
    // because it is written to the file.
    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public bool HasCredentials
    {
        get => _hasCredentials;
        private set => SetProperty(ref _hasCredentials, value);
    }

    public bool CanExport
    {
        get => _canExport;
        private set => SetProperty(ref _canExport, value);
    }

    public ICommand SelectAllCommand => _selectAllCommand;

    public ICommand ClearAllCommand => _clearAllCommand;

    // Null when nothing is selected or there is no collection.
    public ExportSelection? Selection()
    {
        if (SelectedCollection is not { } choice || !CanExport)
        {
            return null;
        }

        return new ExportSelection(choice.Id, choice.Name, Tree.CheckedIds());
    }

    private Collection CollectionOf(CollectionChoice choice)
    {
        return _collections.First(collection => collection.Id == choice.Id);
    }

    private void OnTreeChanged(object? sender, EventArgs e)
    {
        if (SelectedCollection is not { } choice)
        {
            return;
        }

        var selected = SelectionPruner.Prune(CollectionOf(choice), Tree.CheckedIds());
        var counts = TreeCounts.Of(selected.Root);
        Summary = $"{Plural(counts.Folders, "folder")}, {Plural(counts.Requests, "request")} selected";
        HasCredentials = AuthSecrets.HasCredentials(selected.Root);
        CanExport = !counts.IsEmpty;
    }

    private static string Plural(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
