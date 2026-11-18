using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;

namespace RestDraft.UI.ViewModels;

// Builds the nodes of a selection tree from a collection (export) or from the tree of an import plan (preview).
public static class SelectableNodeFactory
{
    public static SelectableNodeViewModel FromCollection(Collection collection)
    {
        var root = new SelectableNodeViewModel(collection.Id, collection.Name, PlanNodeKind.Collection);
        AddChildren(root, collection.Root);
        return root;
    }

    public static SelectableNodeViewModel FromPlan(PlanNode node)
    {
        var result = new SelectableNodeViewModel(node.Id, node.Name, node.Kind, node.Method?.ToString().ToUpperInvariant() ?? string.Empty);
        foreach (var child in node.Children)
        {
            result.AddChild(FromPlan(child));
        }

        return result;
    }

    private static void AddChildren(SelectableNodeViewModel parent, Folder folder)
    {
        foreach (var child in folder.Folders)
        {
            var node = new SelectableNodeViewModel(child.Id, child.Name, PlanNodeKind.Folder);
            AddChildren(node, child);
            parent.AddChild(node);
        }

        foreach (var request in folder.Requests)
        {
            parent.AddChild(new SelectableNodeViewModel(request.Id, request.Name, PlanNodeKind.Request, request.Method.ToString().ToUpperInvariant()));
        }
    }
}
