using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class SelectableNodeViewModelTests
{
    private SelectableNodeViewModel _root = null!;
    private SelectableNodeViewModel _folder = null!;
    private SelectableNodeViewModel _first = null!;
    private SelectableNodeViewModel _second = null!;
    private SelectableNodeViewModel _loose = null!;
    private int _changes;

    [SetUp]
    public void SetUp()
    {
        _root = new SelectableNodeViewModel(Guid.NewGuid(), "Shop", PlanNodeKind.Collection);
        _folder = new SelectableNodeViewModel(Guid.NewGuid(), "Users", PlanNodeKind.Folder);
        _first = new SelectableNodeViewModel(Guid.NewGuid(), "Login", PlanNodeKind.Request, "GET");
        _second = new SelectableNodeViewModel(Guid.NewGuid(), "Logout", PlanNodeKind.Request, "POST");
        _loose = new SelectableNodeViewModel(Guid.NewGuid(), "Ping", PlanNodeKind.Request, "GET");
        _folder.AddChild(_first);
        _folder.AddChild(_second);
        _root.AddChild(_folder);
        _root.AddChild(_loose);
        _changes = 0;
        _root.CheckedChanged += (_, _) => _changes++;
    }

    [Test]
    public void EverythingIsCheckedAtTheStart()
    {
        Assert.That(_root.SelfAndDescendants().Select(node => node.IsChecked), Is.All.EqualTo(true));
    }

    [Test]
    public void UncheckingAParentUnchecksAllBelowIt()
    {
        _folder.IsChecked = false;

        Assert.That(new[] { _folder.IsChecked, _first.IsChecked, _second.IsChecked }, Is.All.EqualTo(false));
        Assert.That(_loose.IsChecked, Is.True);
        Assert.That(_root.IsChecked, Is.Null);
    }

    [Test]
    public void UncheckingOneChildMakesTheParentsPartlyChecked()
    {
        _first.IsChecked = false;

        Assert.That(_folder.IsChecked, Is.Null);
        Assert.That(_root.IsChecked, Is.Null);
        Assert.That(_second.IsChecked, Is.True);
    }

    [Test]
    public void CheckingTheLastUncheckedChildChecksTheParents()
    {
        _first.IsChecked = false;

        _first.IsChecked = true;

        Assert.That(_folder.IsChecked, Is.True);
        Assert.That(_root.IsChecked, Is.True);
    }

    [Test]
    public void UncheckingEveryChildUnchecksTheParent()
    {
        _first.IsChecked = false;
        _second.IsChecked = false;

        Assert.That(_folder.IsChecked, Is.False);
        Assert.That(_root.IsChecked, Is.Null);
    }

    [Test]
    public void CheckingAPartlyCheckedParentChecksAllBelowIt()
    {
        _first.IsChecked = false;

        _folder.IsChecked = true;

        Assert.That(new[] { _first.IsChecked, _second.IsChecked }, Is.All.EqualTo(true));
        Assert.That(_root.IsChecked, Is.True);
    }

    [Test]
    public void TheChangeIsReportedOnceOnTheTopNode()
    {
        _root.IsChecked = false;
        _first.IsChecked = true;

        Assert.That(_changes, Is.EqualTo(2));
    }

    [Test]
    public void SettingTheValueItAlreadyHasReportsNothing()
    {
        _first.IsChecked = true;

        Assert.That(_changes, Is.EqualTo(0));
    }

    [Test]
    public void ASettingToNullIsIgnored()
    {
        _first.IsChecked = null;

        Assert.That(_first.IsChecked, Is.True);
    }

    [Test]
    public void OnlyNodesCheckedAsAWholeAreReportedAsChecked()
    {
        var tree = new SelectionTreeViewModel();
        tree.Show(_root);
        _first.IsChecked = false;

        var ids = tree.CheckedIds();

        Assert.That(ids, Is.EquivalentTo(new[] { _second.Id, _loose.Id }));
    }

    [Test]
    public void ClearAllAndSelectAllReachEveryNode()
    {
        var tree = new SelectionTreeViewModel();
        tree.Show(_root);

        tree.ClearAll();
        var afterClear = tree.CheckedIds();
        tree.SelectAll();

        Assert.That(afterClear, Is.Empty);
        Assert.That(tree.CheckedIds(), Has.Count.EqualTo(5));
    }
}
