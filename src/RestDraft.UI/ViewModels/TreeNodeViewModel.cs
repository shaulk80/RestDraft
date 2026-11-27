using System.Windows.Input;
using RestDraft.Domain.Collections;
using RestDraft.UI.Commands;

namespace RestDraft.UI.ViewModels;

public abstract class TreeNodeViewModel : ViewModelBase
{
    private readonly CollectionsTreeViewModel _owner;
    private bool _isEditing;
    private string _editName = string.Empty;
    private bool _isExpanded;

    protected TreeNodeViewModel(CollectionsTreeViewModel owner, Guid id, string name)
    {
        _owner = owner;
        Id = id;
        Name = name;
        _isExpanded = owner.WasExpanded(id);
        RenameCommand = new RelayCommand(BeginRename, () => true);
        CommitRenameCommand = new AsyncRelayCommand(() => _owner.CommitRenameAsync(this), () => true);
        AcceptRenameCommand = new AsyncRelayCommand(() => _owner.AcceptRenameAsync(this), () => true);
        CancelRenameCommand = new RelayCommand(() => _owner.CancelRename(this), () => true);
        DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteAsync(this), () => true);
        NewFolderCommand = new AsyncRelayCommand(() => _owner.CreateFolderAsync(this), () => true);
        NewRequestCommand = new AsyncRelayCommand(() => _owner.CreateRequestAsync(this), () => true);
        DuplicateCommand = new AsyncRelayCommand(() => _owner.DuplicateAsync(this), () => true);
        OpenCommand = new RelayCommand(() => _owner.OpenRequest(Id), () => true);
        NewCollectionCommand = _owner.NewCollectionCommand;
    }

    public Guid Id { get; }

    public string Name { get; }

    public bool IsEditing
    {
        get => _isEditing;
        private set => SetProperty(ref _isEditing, value);
    }

    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public IReadOnlyList<TreeNodeViewModel> Children { get; private set; } = [];

    // The collection or folder that holds this node. A collection has none.
    public TreeNodeViewModel? Parent { get; private set; }

    // True while the rename box holds the first name of a request that was just created.
    public bool IsNamingNewRequest { get; private set; }

    public abstract bool CanContainChildren { get; }

    public virtual bool IsRequest => false;

    public virtual bool IsCollection => false;

    public virtual bool IsFolder => false;

    // The folder that a node dropped inside this one goes into, and in which a new folder or request is created.
    public abstract Guid DropFolderId { get; }

    public ICommand RenameCommand { get; }

    // Leaving the rename box.
    public ICommand CommitRenameCommand { get; }

    // Enter in the rename box.
    public ICommand AcceptRenameCommand { get; }

    // Escape in the rename box.
    public ICommand CancelRenameCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand NewFolderCommand { get; }

    public ICommand NewRequestCommand { get; }

    public ICommand DuplicateCommand { get; }

    public ICommand OpenCommand { get; }

    public ICommand NewCollectionCommand { get; }

    public void BeginRename()
    {
        StartRename(isNewRequest: false);
    }

    public void BeginRenameOfNewRequest()
    {
        StartRename(isNewRequest: true);
    }

    public void EndRename()
    {
        IsNamingNewRequest = false;
        IsEditing = false;
    }

    private void StartRename(bool isNewRequest)
    {
        IsNamingNewRequest = isNewRequest;
        EditName = Name;
        IsEditing = true;
    }

    protected void SetChildren(IReadOnlyList<TreeNodeViewModel> children)
    {
        Children = children;
        foreach (var child in children)
        {
            child.Parent = this;
        }
    }

    protected static IReadOnlyList<TreeNodeViewModel> BuildChildren(CollectionsTreeViewModel owner, Folder folder)
    {
        return [
            .. folder.Folders.Select(child => new FolderNodeViewModel(owner, child)),
            .. folder.Requests.Select(request => new RequestNodeViewModel(owner, request, folder.Id)),
        ];
    }
}
