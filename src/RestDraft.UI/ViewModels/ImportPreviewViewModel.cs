using System.Windows.Input;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.UI.Commands;
using RestDraft.UI.Services;

namespace RestDraft.UI.ViewModels;

// The import preview: what the file holds, where it goes, and what happens to each item. Every change of the target, the
// overwrite option or a check box asks the planner again, so the tree always shows what Import will do.
public sealed class ImportPreviewViewModel : ViewModelBase
{
    private readonly ExchangeDocument _document;
    private readonly IReadOnlyList<Collection> _existing;
    private readonly Func<IReadOnlyCollection<Guid>, int> _countTabsWithEdits;
    private readonly RelayCommand _selectAllCommand;
    private readonly RelayCommand _clearAllCommand;
    private bool _isNewCollection = true;
    private string _newName;
    private string _baseUrl;
    private CollectionChoice? _selectedExisting;
    private bool _overwrite;
    private string _summary = string.Empty;
    private string _tabsNotice = string.Empty;
    private int _tabsWithEdits;
    private bool _canImport;

    // countTabsWithEdits gets the ids of the requests that the import replaces and says how many open tabs of them have
    // unsaved edits.
    public ImportPreviewViewModel(
        ExchangeDocument document,
        IReadOnlyList<Collection> existing,
        Guid? preselectedCollectionId,
        Func<IReadOnlyCollection<Guid>, int> countTabsWithEdits)
    {
        _document = document;
        _existing = existing;
        _countTabsWithEdits = countTabsWithEdits;
        ExistingCollections = existing.Select(collection => new CollectionChoice(collection.Id, collection.Name)).ToList();
        _selectedExisting = ExistingCollections.FirstOrDefault(choice => choice.Id == preselectedCollectionId) ?? (ExistingCollections.Count > 0 ? ExistingCollections[0] : null);
        _newName = ChildNameRules.NextAvailableName(document.Collection.Name, existing.Select(collection => collection.Name).ToList());
        _baseUrl = document.BaseUrl is null ? string.Empty : BaseUrlResolver.Resolve(document.BaseUrl, downloadAddress: null);
        Warnings = document.Warnings.Select(FormatWarning).ToList();
        _selectAllCommand = new RelayCommand(Tree.SelectAll, () => true);
        _clearAllCommand = new RelayCommand(Tree.ClearAll, () => true);
        Tree.Changed += OnInputChanged;
        Tree.Show(SelectableNodeFactory.FromPlan(PlanFor(CurrentTarget(), overwrite: false, new HashSet<Guid>())?.Tree ?? EmptyTree()));
    }

    public SelectionTreeViewModel Tree { get; } = new();

    public IReadOnlyList<CollectionChoice> ExistingCollections { get; }

    public bool HasExistingCollections => ExistingCollections.Count > 0;

    public IReadOnlyList<string> Warnings { get; }

    public bool HasWarnings => Warnings.Count > 0;

    public string WarningsHeader => Warnings.Count == 1 ? "1 warning" : $"{Warnings.Count} warnings";

    public bool IsNewCollection
    {
        get => _isNewCollection;
        set
        {
            if (SetProperty(ref _isNewCollection, value))
            {
                RaisePropertyChanged(nameof(IsExistingCollection));
                RaisePropertyChanged(nameof(CanOverwrite));
                OnInputChanged(this, EventArgs.Empty);
            }
        }
    }

    public bool IsExistingCollection
    {
        get => !_isNewCollection;
        set => IsNewCollection = !value;
    }

    public string NewName
    {
        get => _newName;
        set
        {
            if (SetProperty(ref _newName, value))
            {
                OnInputChanged(this, EventArgs.Empty);
            }
        }
    }

    // Only a document whose request URLs are paths, such as a specification, has a base URL.
    public bool HasBaseUrl => _document.BaseUrl is not null;

    // What the import puts in front of every request URL.
    public string BaseUrl
    {
        get => _baseUrl;
        set
        {
            if (SetProperty(ref _baseUrl, value ?? string.Empty))
            {
                OnInputChanged(this, EventArgs.Empty);
            }
        }
    }

    public CollectionChoice? SelectedExisting
    {
        get => _selectedExisting;
        set
        {
            if (SetProperty(ref _selectedExisting, value))
            {
                OnInputChanged(this, EventArgs.Empty);
            }
        }
    }

    public bool Overwrite
    {
        get => _overwrite;
        set
        {
            if (SetProperty(ref _overwrite, value))
            {
                OnInputChanged(this, EventArgs.Empty);
            }
        }
    }

    // Overwriting means something only when the target already has items.
    public bool CanOverwrite => IsExistingCollection;

    public string Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    // Says that open tabs with unsaved edits are replaced. Empty when no such tab is affected.
    public string TabsNotice
    {
        get => _tabsNotice;
        private set => SetProperty(ref _tabsNotice, value);
    }

    public bool CanImport
    {
        get => _canImport;
        private set => SetProperty(ref _canImport, value);
    }

    public ICommand SelectAllCommand => _selectAllCommand;

    public ICommand ClearAllCommand => _clearAllCommand;

    // Null when nothing would change or the base URL is missing.
    public ImportRequest? Request()
    {
        if (!CanImport)
        {
            return null;
        }

        var baseUrl = HasBaseUrl ? BaseUrl.Trim() : null;
        return new ImportRequest(CurrentTarget(), EffectiveOverwrite, Tree.CheckedIds(), _tabsWithEdits, baseUrl);
    }

    private bool IsBaseUrlMissing => HasBaseUrl && string.IsNullOrWhiteSpace(_baseUrl);

    private bool EffectiveOverwrite => Overwrite && CanOverwrite;

    private ImportTarget CurrentTarget()
    {
        if (!_isNewCollection && _selectedExisting is { } existing)
        {
            return ImportTarget.Existing(existing.Id);
        }

        return ImportTarget.NewCollection(_newName);
    }

    private ImportPlan? PlanFor(ImportTarget target, bool overwrite, IReadOnlySet<Guid> ticked)
    {
        return ImportPlanner.Plan(_existing, _document.Collection, target, overwrite, ticked).Value;
    }

    private static PlanNode EmptyTree()
    {
        return new PlanNode(Guid.Empty, string.Empty, PlanNodeKind.Collection, PlanStatus.New, null, []);
    }

    private void OnInputChanged(object? sender, EventArgs e)
    {
        var ticked = Tree.CheckedIds();
        var planned = ImportPlanner.Plan(_existing, _document.Collection, CurrentTarget(), EffectiveOverwrite, ticked);
        if (!planned.IsSuccess)
        {
            Summary = planned.Error!.Message;
            CanImport = false;
            TabsNotice = string.Empty;
            return;
        }

        var plan = planned.Value!;
        ShowStatuses(plan);
        ShowSummary(plan, ticked);
        _tabsWithEdits = _countTabsWithEdits(plan.OverwrittenRequestIds);
        TabsNotice = _tabsWithEdits == 0
            ? string.Empty
            : $"{(_tabsWithEdits == 1 ? "1 open tab has" : $"{_tabsWithEdits} open tabs have")} unsaved edits that will be replaced.";
        CanImport = plan.Counts.HasChanges && !IsBaseUrlMissing;
    }

    private void ShowStatuses(ImportPlan plan)
    {
        var statuses = Flatten(plan.Tree).ToDictionary(node => node.Id);
        foreach (var node in Tree.AllNodes().Where(node => !node.IsCollection))
        {
            node.StatusText = statuses.TryGetValue(node.Id, out var planNode) ? StatusText(planNode) : string.Empty;
        }
    }

    private string StatusText(PlanNode node)
    {
        if (node.Status == PlanStatus.New)
        {
            return "New";
        }

        return node.Kind == PlanNodeKind.Request && EffectiveOverwrite ? "Exists, will be overwritten" : "Exists";
    }

    private void ShowSummary(ImportPlan plan, IReadOnlySet<Guid> ticked)
    {
        var picked = TreeCounts.Of(SelectionPruner.Prune(_document.Collection, ticked).Root);
        var counts = plan.Counts;
        Summary = $"{Plural(picked.Folders, "folder")}, {Plural(picked.Requests, "request")} ticked: "
            + $"{counts.NewRequests} new, {counts.ExistingRequests} exist";
    }

    private static IEnumerable<PlanNode> Flatten(PlanNode node)
    {
        return [node, .. node.Children.SelectMany(Flatten)];
    }

    private static string FormatWarning(ImportWarning warning)
    {
        return string.IsNullOrEmpty(warning.Path) ? warning.Message : $"{warning.Path}: {warning.Message}";
    }

    private static string Plural(int count, string noun)
    {
        return count == 1 ? $"1 {noun}" : $"{count} {noun}s";
    }
}
