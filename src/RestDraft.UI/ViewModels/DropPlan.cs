namespace RestDraft.UI.ViewModels;

// What a drop asks of the tree: the folder that receives the node, the sibling it goes in front of (null appends),
// and the node whose row is opened so the moved node is in view.
public sealed record DropPlan(Guid TargetFolderId, Guid? BeforeNodeId, Guid ExpandId);
