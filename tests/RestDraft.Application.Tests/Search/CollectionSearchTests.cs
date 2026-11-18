using NUnit.Framework;
using RestDraft.Application.Search;
using RestDraft.Domain.Bodies;
using RestDraft.Domain.Collections;

namespace RestDraft.Application.Tests.Search;

[TestFixture]
public class CollectionSearchTests
{
    private RequestDefinition _login = null!;
    private RequestDefinition _list = null!;
    private RequestDefinition _health = null!;
    private Folder _users = null!;
    private Folder _drafts = null!;
    private Collection _shop = null!;
    private Collection _ops = null!;
    private IReadOnlyList<Collection> _collections = null!;

    [SetUp]
    public void SetUp()
    {
        _login = new RequestDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Login",
            Url = "https://example.com/login",
            Method = RequestMethod.Post,
            Body = new RawBody("application/json", "{\"password\": \"hunter2\"}"),
        };
        _list = new RequestDefinition { Id = Guid.NewGuid(), Name = "List", Url = "https://example.com/accounts" };
        _health = new RequestDefinition { Id = Guid.NewGuid(), Name = "Health", Url = "https://ops.example.com/health" };
        _users = new Folder(Guid.NewGuid(), "Users", [], [_login, _list]);
        _drafts = new Folder(Guid.NewGuid(), "Drafts", [], []);
        _shop = new Collection(Guid.NewGuid(), "Shop", new Folder(Guid.NewGuid(), string.Empty, [_users, _drafts], []));
        _ops = new Collection(Guid.NewGuid(), "Operations", new Folder(Guid.NewGuid(), string.Empty, [], [_health]));
        _collections = [_shop, _ops];
    }

    [Test]
    public void InactiveFilterReturnsTheCollectionsAsTheyAre()
    {
        var result = CollectionSearch.Apply(_collections, RequestFilter.None, null);

        Assert.That(result, Is.SameAs(_collections));
    }

    [Test]
    public void RequestMatchKeepsItsFoldersAndCollectionAndDropsTheRest()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("login", null, false), null);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Shop" }));
        var folders = result[0].Root.Folders;
        Assert.That(folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Users" }));
        Assert.That(folders[0].Requests.Select(request => request.Name), Is.EqualTo(new[] { "Login" }));
    }

    [Test]
    public void FolderNameMatchKeepsTheWholeFolder()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("users", null, false), null);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Shop" }));
        Assert.That(result[0].Root.Folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Users" }));
        Assert.That(result[0].Root.Folders[0].Requests.Select(request => request.Name), Is.EqualTo(new[] { "Login", "List" }));
    }

    [Test]
    public void FolderWhoseNameMatchesStaysEvenWhenEmpty()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("drafts", null, false), null);

        Assert.That(result[0].Root.Folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Drafts" }));
    }

    [Test]
    public void CollectionNameMatchKeepsEverythingInIt()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("shop", null, false), null);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Shop" }));
        Assert.That(result[0].Root.Folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Users", "Drafts" }));
        Assert.That(result[0].Root.Folders[0].Requests, Has.Count.EqualTo(2));
    }

    [Test]
    public void RequestUrlMatches()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("ops.example", null, false), null);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Operations" }));
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public void BodyTextIsFoundOnlyWhenAsked(bool inBodies, int expectedCollections)
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("hunter2", null, inBodies), null);

        Assert.That(result, Has.Count.EqualTo(expectedCollections));
    }

    [Test]
    public void MethodAloneKeepsOnlyContainersWithARequestOfThatMethod()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter(string.Empty, RequestMethod.Post, false), null);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Shop" }));
        Assert.That(result[0].Root.Folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Users" }), "the empty folder has no POST request");
        Assert.That(result[0].Root.Folders[0].Requests.Select(request => request.Name), Is.EqualTo(new[] { "Login" }));
    }

    [Test]
    public void MethodNarrowsTheSubtreeOfAFolderWhoseNameMatches()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("users", RequestMethod.Post, false), null);

        Assert.That(result[0].Root.Folders[0].Requests.Select(request => request.Name), Is.EqualTo(new[] { "Login" }));
    }

    [Test]
    public void KeepIdStaysVisibleWithItsParentsEvenWhenItDoesNotMatch()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("health", null, false), _list.Id);

        Assert.That(result.Select(collection => collection.Name), Is.EqualTo(new[] { "Shop", "Operations" }));
        Assert.That(result[0].Root.Folders[0].Requests.Select(request => request.Name), Is.EqualTo(new[] { "List" }));
    }

    [Test]
    public void KeepIdOfAFolderKeepsTheEmptyFolder()
    {
        var result = CollectionSearch.Apply(_collections, new RequestFilter("health", null, false), _drafts.Id);

        Assert.That(result[0].Root.Folders.Select(folder => folder.Name), Is.EqualTo(new[] { "Drafts" }));
    }

    [Test]
    public void StoredCollectionsAreNotChanged()
    {
        CollectionSearch.Apply(_collections, new RequestFilter("login", null, false), null);

        Assert.That(_shop.Root.Folders, Has.Count.EqualTo(2));
        Assert.That(_users.Requests, Has.Count.EqualTo(2));
    }
}
