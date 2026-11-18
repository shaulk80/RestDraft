using RestDraft.Application.Exchange;

namespace RestDraft.UI.ViewModels;

// A node of a tree with check boxes, for the export and the import preview. Checking a node checks everything below it.
// A node whose children are only partly checked is neither (null). The logic has no Avalonia types, so it is tested directly.
public sealed class SelectableNodeViewModel : ViewModelBase
{
    private readonly List<SelectableNodeViewModel> _children = [];
    private bool? _isChecked = true;
    private bool _isExpanded = true;
    private string _statusText = string.Empty;

    public SelectableNodeViewModel(Guid id, string title, PlanNodeKind kind, string methodText = "")
    {
        Id = id;
        Title = title;
        Kind = kind;
        MethodText = methodText;
    }

    // Raised on the top node of the tree after the user changed a check box, once per change.
    public event EventHandler? CheckedChanged;

    public Guid Id { get; }

    public string Title { get; }

    public PlanNodeKind Kind { get; }

    // The HTTP method of a request, empty for the others.
    public string MethodText { get; }

    public bool IsCollection => Kind == PlanNodeKind.Collection;

    public bool IsFolder => Kind == PlanNodeKind.Folder;

    public bool IsRequest => Kind == PlanNodeKind.Request;

    public SelectableNodeViewModel? Parent { get; private set; }

    public IReadOnlyList<SelectableNodeViewModel> Children => _children;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    // What the import will do with the item: New, Exists, ...
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool? IsChecked
    {
        get => _isChecked;
        set
        {
            if (value is null || _isChecked == value)
            {
                return;
            }

            SetCheckedBelow(value.Value);
            Parent?.RefreshFromChildren();
            RaiseCheckedChanged();
        }
    }

    public void AddChild(SelectableNodeViewModel child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public IEnumerable<SelectableNodeViewModel> SelfAndDescendants()
    {
        return [this, .. _children.SelectMany(child => child.SelfAndDescendants())];
    }

    private void SetCheckedBelow(bool value)
    {
        SetChecked(value);
        foreach (var child in _children)
        {
            child.SetCheckedBelow(value);
        }
    }

    // A node with children shows what its children say. The change goes up to the top.
    private void RefreshFromChildren()
    {
        SetChecked(StateOfChildren());
        Parent?.RefreshFromChildren();
    }

    private bool? StateOfChildren()
    {
        if (_children.All(child => child.IsChecked == true))
        {
            return true;
        }

        if (_children.All(child => child.IsChecked == false))
        {
            return false;
        }

        return null;
    }

    private void SetChecked(bool? value)
    {
        SetProperty(ref _isChecked, value, nameof(IsChecked));
    }

    private void RaiseCheckedChanged()
    {
        var top = this;
        while (top.Parent is not null)
        {
            top = top.Parent;
        }

        top.CheckedChanged?.Invoke(this, EventArgs.Empty);
    }
}
