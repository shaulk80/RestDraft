using NUnit.Framework;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class DropPlannerTests
{
    private const string Root = "root";
    private const string Last = "last";
    private const string Nothing = "nothing";

    private ReorderTree _tree = null!;

    [SetUp]
    public async Task SetUp()
    {
        _tree = await ReorderTree.CreateAsync();
    }

    // Folders reorder among the folders. The folders come first, so "before" a request means the end of the folders.
    [TestCase("F1", "F2", DropPosition.After, Root, "F3")]
    [TestCase("F3", "F1", DropPosition.Before, Root, "F1")]
    [TestCase("F1", "F3", DropPosition.After, Root, Last)]
    [TestCase("F1", "R1", DropPosition.Before, Root, Last)]
    [TestCase("F1", "R3", DropPosition.After, Root, Last)]
    [TestCase("F1", "F2", DropPosition.Before, Nothing, Nothing)]
    [TestCase("F2", "F1", DropPosition.After, Nothing, Nothing)]
    [TestCase("F3", "R2", DropPosition.After, Nothing, Nothing)]
    [TestCase("F2", "F2", DropPosition.Before, Nothing, Nothing)]
    [TestCase("F3", "F3", DropPosition.After, Nothing, Nothing)]
    public void FoldersDroppedBesideASibling(string source, string target, DropPosition position, string folder, string before)
    {
        AssertPlan(source, target, position, folder, before);
    }

    // Requests reorder among the requests. "Before" a folder means the start of the requests.
    [TestCase("R3", "R1", DropPosition.Before, Root, "R1")]
    [TestCase("R1", "R2", DropPosition.After, Root, "R3")]
    [TestCase("R1", "R3", DropPosition.After, Root, Last)]
    [TestCase("R3", "F1", DropPosition.Before, Root, "R1")]
    [TestCase("R3", "F3", DropPosition.After, Root, "R1")]
    [TestCase("R1", "F1", DropPosition.Before, Nothing, Nothing)]
    [TestCase("R1", "R2", DropPosition.Before, Nothing, Nothing)]
    [TestCase("R2", "R1", DropPosition.After, Nothing, Nothing)]
    [TestCase("R3", "R3", DropPosition.After, Nothing, Nothing)]
    public void RequestsDroppedBesideASibling(string source, string target, DropPosition position, string folder, string before)
    {
        AssertPlan(source, target, position, folder, before);
    }

    // Beside a node in another folder, the node goes into that folder.
    [TestCase("R2", "N2", DropPosition.Before, "F1", "N2")]
    [TestCase("R2", "N2", DropPosition.After, "F1", Last)]
    [TestCase("R2", "N1", DropPosition.After, "F1", "N2")]
    [TestCase("N1", "N2", DropPosition.After, "F1", Last)]
    [TestCase("N1", "R2", DropPosition.After, Root, "R3")]
    [TestCase("N2", "F2", DropPosition.Before, Root, "R1")]
    public void NodesDroppedBesideANodeOfAnotherFolder(string source, string target, DropPosition position, string folder, string before)
    {
        AssertPlan(source, target, position, folder, before);
    }

    [TestCase("R1", "F2")]
    [TestCase("F1", "F3")]
    [TestCase("N1", "F1")]
    public void DroppingInsideAFolderAppendsToIt(string source, string target)
    {
        var plan = DropPlanner.Plan(_tree.Node(source), _tree.Node(target), DropPosition.Inside);

        Assert.That(plan, Is.EqualTo(new DropPlan(_tree.Node(target).Id, null, _tree.Node(target).Id)));
    }

    [TestCase(DropPosition.Before)]
    [TestCase(DropPosition.Inside)]
    [TestCase(DropPosition.After)]
    public void DroppingOnACollectionAppendsToItsRootFolder(DropPosition position)
    {
        var collection = _tree.Node("C");

        var plan = DropPlanner.Plan(_tree.Node("F2"), collection, position);

        Assert.That(plan, Is.EqualTo(new DropPlan(_tree.RootFolderId, null, collection.Id)));
    }

    [TestCase("F1", "F1", false)]
    [TestCase("F1", "N1", false)]
    [TestCase("F1", "F2", true)]
    [TestCase("F2", "F1", true)]
    [TestCase("R1", "F1", true)]
    [TestCase("N1", "N1", false)]
    public void ANodeCannotBeDroppedIntoItselfOrItsSubtree(string source, string target, bool expected)
    {
        Assert.That(DropPlanner.CanDrop(_tree.Node(source), _tree.Node(target)), Is.EqualTo(expected));
    }

    private void AssertPlan(string source, string target, DropPosition position, string folder, string before)
    {
        var plan = DropPlanner.Plan(_tree.Node(source), _tree.Node(target), position);

        if (folder == Nothing)
        {
            Assert.That(plan, Is.Null);
            return;
        }

        var folderId = folder == Root ? _tree.RootFolderId : _tree.Node(folder).Id;
        Guid? beforeId = before == Last ? null : _tree.Node(before).Id;
        var parent = _tree.Node(target).Parent!;
        Assert.That(plan, Is.EqualTo(new DropPlan(folderId, beforeId, parent.Id)));
    }
}
