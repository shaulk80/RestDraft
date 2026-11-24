using NUnit.Framework;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class DropZonesTests
{
    private ReorderTree _tree = null!;

    [SetUp]
    public async Task SetUp()
    {
        _tree = await ReorderTree.CreateAsync();
    }

    [TestCase("R1", 0.0, DropPosition.Before)]
    [TestCase("R1", 0.49, DropPosition.Before)]
    [TestCase("R1", 0.5, DropPosition.After)]
    [TestCase("R1", 1.0, DropPosition.After)]
    public void ARequestIsSplitInHalves(string name, double fraction, DropPosition expected)
    {
        Assert.That(DropZones.Resolve(_tree.Node(name), fraction * 32, 32), Is.EqualTo(expected));
    }

    // F2 and F3 have no children. F1 has two requests and is collapsed until the test opens it.
    [TestCase("F2", 0.0, false, DropPosition.Before)]
    [TestCase("F2", 0.24, false, DropPosition.Before)]
    [TestCase("F2", 0.25, false, DropPosition.Inside)]
    [TestCase("F2", 0.5, false, DropPosition.Inside)]
    [TestCase("F2", 0.75, false, DropPosition.Inside)]
    [TestCase("F2", 0.76, false, DropPosition.After)]
    [TestCase("F2", 1.0, false, DropPosition.After)]
    [TestCase("F1", 0.9, false, DropPosition.After)]
    [TestCase("F1", 0.9, true, DropPosition.Inside)]
    [TestCase("F1", 0.1, true, DropPosition.Before)]
    public void AFolderHasEdgesAndAMiddle(string name, double fraction, bool expanded, DropPosition expected)
    {
        var folder = _tree.Node(name);
        folder.IsExpanded = expanded;

        Assert.That(DropZones.Resolve(folder, fraction * 32, 32), Is.EqualTo(expected));
    }

    [TestCase(0.0)]
    [TestCase(0.5)]
    [TestCase(1.0)]
    public void ACollectionIsAlwaysInside(double fraction)
    {
        Assert.That(DropZones.Resolve(_tree.Node("C"), fraction * 32, 32), Is.EqualTo(DropPosition.Inside));
    }

    [Test]
    public void ARowWithoutHeightIsInside()
    {
        Assert.That(DropZones.Resolve(_tree.Node("R1"), 5, 0), Is.EqualTo(DropPosition.Inside));
    }
}
