namespace RestDraft.UI.ViewModels;

// The tree with check boxes that the export and the import preview show: one root (the collection) and what is under it.
public sealed class SelectionTreeViewModel : ViewModelBase
{
    private SelectableNodeViewModel? _root;

    public IReadOnlyList<SelectableNodeViewModel> Roots => _root is null ? [] : [_root];

    // Raised after the user changed a check box, and after Select all and Clear all.
    public event EventHandler? Changed;

    public void Show(SelectableNodeViewModel root)
    {
        if (_root is not null)
        {
            _root.CheckedChanged -= OnCheckedChanged;
        }

        _root = root;
        root.CheckedChanged += OnCheckedChanged;
        RaisePropertyChanged(nameof(Roots));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<SelectableNodeViewModel> AllNodes()
    {
        return _root?.SelfAndDescendants() ?? [];
    }

    // The nodes that are checked as a whole. A node that is only partly checked is not one of them; what is checked
    // below it brings it along.
    public IReadOnlySet<Guid> CheckedIds()
    {
        return AllNodes().Where(node => node.IsChecked == true).Select(node => node.Id).ToHashSet();
    }

    public void SelectAll()
    {
        if (_root is not null)
        {
            _root.IsChecked = true;
        }
    }

    public void ClearAll()
    {
        if (_root is not null)
        {
            _root.IsChecked = false;
        }
    }

    private void OnCheckedChanged(object? sender, EventArgs e)
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
