using NUnit.Framework;
using RestDraft.Domain.Auth;
using RestDraft.Domain.Collections;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class ExportSelectionViewModelTests
{
    private Collection _shop = null!;
    private Collection _blog = null!;
    private RequestDefinition _login = null!;
    private RequestDefinition _logout = null!;

    [SetUp]
    public void SetUp()
    {
        _login = new RequestDefinition { Id = Guid.NewGuid(), Name = "Login" };
        _logout = new RequestDefinition { Id = Guid.NewGuid(), Name = "Logout" };
        var users = new Folder(Guid.NewGuid(), "Users", [], [_login, _logout]);
        var empty = new Folder(Guid.NewGuid(), "Empty", [], []);
        _shop = new Collection(Guid.NewGuid(), "Shop", new Folder(Guid.NewGuid(), string.Empty, [users, empty], []));
        _blog = new Collection(Guid.NewGuid(), "Blog", new Folder(Guid.NewGuid(), string.Empty, [], []));
    }

    [Test]
    public void StartsWithEverythingSelectedInThePreselectedCollection()
    {
        var viewModel = new ExportSelectionViewModel([_blog, _shop], _shop.Id);

        Assert.That(viewModel.SelectedCollection?.Name, Is.EqualTo("Shop"));
        Assert.That(viewModel.Summary, Is.EqualTo("2 folders, 2 requests selected"));
        Assert.That(viewModel.CanExport, Is.True);
    }

    [Test]
    public void TheFirstCollectionIsUsedWhenNoneIsPreselected()
    {
        var viewModel = new ExportSelectionViewModel([_blog, _shop], null);

        Assert.That(viewModel.SelectedCollection?.Name, Is.EqualTo("Blog"));
    }

    [Test]
    public void UntickingARequestUpdatesTheCountAndKeepsItsFolder()
    {
        var viewModel = new ExportSelectionViewModel([_shop], _shop.Id);
        FindNode(viewModel, _logout.Id).IsChecked = false;
        FindNode(viewModel, _login.Id).IsChecked = true;
        viewModel.Tree.AllNodes().Single(node => node.Title == "Empty").IsChecked = false;

        Assert.That(viewModel.Summary, Is.EqualTo("1 folder, 1 request selected"));
    }

    [Test]
    public void ExportIsDisabledWhileNothingIsSelected()
    {
        var viewModel = new ExportSelectionViewModel([_shop], _shop.Id);

        viewModel.ClearAllCommand.Execute(null);

        Assert.That(viewModel.CanExport, Is.False);
        Assert.That(viewModel.Selection(), Is.Null);
        Assert.That(viewModel.Summary, Is.EqualTo("0 folders, 0 requests selected"));
    }

    [Test]
    public void ChangingTheCollectionRebuildsTheTree()
    {
        var viewModel = new ExportSelectionViewModel([_shop, _blog], _shop.Id);

        viewModel.SelectedCollection = viewModel.Collections.Single(choice => choice.Name == "Blog");

        Assert.That(viewModel.Tree.Roots.Single().Title, Is.EqualTo("Blog"));
        Assert.That(viewModel.CanExport, Is.False);
    }

    [Test]
    public void TheSelectionHoldsTheCollectionAndTheTickedIds()
    {
        var viewModel = new ExportSelectionViewModel([_shop], _shop.Id);
        FindNode(viewModel, _logout.Id).IsChecked = false;

        var selection = viewModel.Selection();

        Assert.That(selection?.CollectionId, Is.EqualTo(_shop.Id));
        Assert.That(selection?.TickedIds, Does.Contain(_login.Id).And.Not.Contain(_logout.Id));
    }

    [TestCase(false, false)]
    [TestCase(true, true)]
    public void CredentialsInTheSelectionShowANotice(bool hasPassword, bool expectedNotice)
    {
        var secured = _login with { Auth = hasPassword ? new BasicAuth("user", "secret") : new NoAuth() };
        var folder = new Folder(Guid.NewGuid(), "Users", [], [secured]);
        var collection = new Collection(Guid.NewGuid(), "Secure", new Folder(Guid.NewGuid(), string.Empty, [folder], []));

        var viewModel = new ExportSelectionViewModel([collection], collection.Id);

        Assert.That(viewModel.HasCredentials, Is.EqualTo(expectedNotice));
    }

    [Test]
    public void TheNoticeGoesAwayWhenTheRequestWithCredentialsIsUnticked()
    {
        var secured = _login with { Auth = new BearerAuth("token") };
        var collection = new Collection(Guid.NewGuid(), "Secure", new Folder(Guid.NewGuid(), string.Empty, [], [secured, _logout]));
        var viewModel = new ExportSelectionViewModel([collection], collection.Id);

        FindNode(viewModel, secured.Id).IsChecked = false;

        Assert.That(viewModel.HasCredentials, Is.False);
    }

    private static SelectableNodeViewModel FindNode(ExportSelectionViewModel viewModel, Guid id)
    {
        return viewModel.Tree.AllNodes().Single(node => node.Id == id);
    }
}
