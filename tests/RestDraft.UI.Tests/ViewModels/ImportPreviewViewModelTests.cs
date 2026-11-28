using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class ImportPreviewViewModelTests
{
    private RequestDefinition _fileList = null!;
    private RequestDefinition _fileCreate = null!;
    private RequestDefinition _minePing = null!;
    private Collection _file = null!;
    private Collection _mine = null!;
    private ExchangeDocument _document = null!;
    private IReadOnlyCollection<Guid> _asked = [];
    private int _tabsWithEdits;

    [SetUp]
    public void SetUp()
    {
        _fileList = new RequestDefinition { Id = Guid.NewGuid(), Name = "List", Url = "http://file/list" };
        _fileCreate = new RequestDefinition { Id = Guid.NewGuid(), Name = "Create" };
        var api = new Folder(Guid.NewGuid(), "API", [], [_fileList, _fileCreate]);
        _file = new Collection(Guid.NewGuid(), "Scratch", new Folder(Guid.NewGuid(), string.Empty, [api], []));
        _document = new ExchangeDocument(_file, [new ImportWarning("Left out.", "API / List"), new ImportWarning("Also left out.")]);
        _minePing = new RequestDefinition { Id = Guid.NewGuid(), Name = "list", Url = "http://mine/list" };
        _mine = new Collection(Guid.NewGuid(), "Mine", new Folder(Guid.NewGuid(), string.Empty, [new Folder(Guid.NewGuid(), "api", [], [_minePing])], []));
        _asked = [];
        _tabsWithEdits = 0;
    }

    [Test]
    public void StartsAsANewCollectionWithTheNameOfTheFileMadeUnique()
    {
        var sameName = new Collection(Guid.NewGuid(), "scratch", _mine.Root);

        var viewModel = Create([sameName]);

        Assert.That(viewModel.IsNewCollection, Is.True);
        Assert.That(viewModel.NewName, Is.EqualTo("Scratch 2"));
        Assert.That(viewModel.CanImport, Is.True);
        Assert.That(viewModel.Summary, Is.EqualTo("1 folder, 2 requests ticked: 2 new, 0 exist"));
    }

    [Test]
    public void WarningsAreListedWithTheirPath()
    {
        var viewModel = Create([]);

        Assert.That(viewModel.Warnings, Is.EqualTo(new[] { "API / List: Left out.", "Also left out." }));
        Assert.That(viewModel.WarningsHeader, Is.EqualTo("2 warnings"));
    }

    [Test]
    public void AnExistingTargetShowsWhichItemsExist()
    {
        var viewModel = Create([_mine]);

        viewModel.IsExistingCollection = true;

        Assert.That(StatusOf(viewModel, _fileList.Id), Is.EqualTo("Exists"));
        Assert.That(StatusOf(viewModel, _fileCreate.Id), Is.EqualTo("New"));
        Assert.That(viewModel.Summary, Is.EqualTo("1 folder, 2 requests ticked: 1 new, 1 exist"));
    }

    [Test]
    public void SwitchingBackToANewCollectionMakesEverythingNew()
    {
        var viewModel = Create([_mine]);
        viewModel.IsExistingCollection = true;

        viewModel.IsNewCollection = true;

        Assert.That(StatusOf(viewModel, _fileList.Id), Is.EqualTo("New"));
    }

    [Test]
    public void OverwriteCanBeSetOnlyForAnExistingTargetAndChangesTheStatus()
    {
        var viewModel = Create([_mine]);
        viewModel.Overwrite = true;

        var beforeSwitch = viewModel.CanOverwrite;
        viewModel.IsExistingCollection = true;

        Assert.That(beforeSwitch, Is.False);
        Assert.That(viewModel.CanOverwrite, Is.True);
        Assert.That(StatusOf(viewModel, _fileList.Id), Is.EqualTo("Exists, will be overwritten"));
        Assert.That(viewModel.Request()?.Overwrite, Is.True);
    }

    [Test]
    public void OverwriteIsNotPassedOnForANewCollection()
    {
        var viewModel = Create([_mine]);

        viewModel.Overwrite = true;

        Assert.That(viewModel.Request()?.Overwrite, Is.False);
    }

    [Test]
    public void UntickingAnItemTakesItOutOfTheCounts()
    {
        var viewModel = Create([]);

        viewModel.Tree.AllNodes().Single(node => node.Id == _fileList.Id).IsChecked = false;

        Assert.That(viewModel.Summary, Is.EqualTo("1 folder, 1 request ticked: 1 new, 0 exist"));
        Assert.That(viewModel.Request()?.TickedIds, Does.Contain(_fileCreate.Id).And.Not.Contain(_fileList.Id));
    }

    [Test]
    public void ImportIsDisabledWhenNothingIsTicked()
    {
        var viewModel = Create([]);

        viewModel.ClearAllCommand.Execute(null);

        Assert.That(viewModel.CanImport, Is.False);
        Assert.That(viewModel.Request(), Is.Null);
    }

    [Test]
    public void ImportIsDisabledWhenEverythingTickedExistsAndOverwriteIsOff()
    {
        var onlyList = new Collection(_file.Id, "Scratch", new Folder(_file.Root.Id, string.Empty, [new Folder(Guid.NewGuid(), "API", [], [_fileList])], []));
        var viewModel = new ImportPreviewViewModel(new ExchangeDocument(onlyList, []), [_mine], _mine.Id, CountTabs);

        viewModel.IsExistingCollection = true;

        Assert.That(viewModel.CanImport, Is.False);
        viewModel.Overwrite = true;
        Assert.That(viewModel.CanImport, Is.True);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ImportIsDisabledWhenTheNewNameIsEmpty(string name)
    {
        var viewModel = Create([]);

        viewModel.NewName = name;

        Assert.That(viewModel.CanImport, Is.False);
        Assert.That(viewModel.Summary, Does.Contain("must not be empty"));
    }

    [Test]
    public void TheCollectionSelectedInTheTreeIsPreselectedAsTheExistingTarget()
    {
        var other = new Collection(Guid.NewGuid(), "Other", _mine.Root);

        var viewModel = new ImportPreviewViewModel(_document, [other, _mine], _mine.Id, CountTabs);

        Assert.That(viewModel.SelectedExisting?.Name, Is.EqualTo("Mine"));
    }

    [Test]
    public void ThereIsNoExistingTargetWithoutCollections()
    {
        var viewModel = Create([]);

        Assert.That(viewModel.HasExistingCollections, Is.False);
    }

    [Test]
    public void OpenTabsWithEditsThatWillBeReplacedAreReported()
    {
        _tabsWithEdits = 2;
        var viewModel = Create([_mine]);
        viewModel.IsExistingCollection = true;
        viewModel.Overwrite = true;

        Assert.That(viewModel.TabsNotice, Is.EqualTo("2 open tabs have unsaved edits that will be replaced."));
        Assert.That(viewModel.Request()?.OpenTabsWithEdits, Is.EqualTo(2));
        Assert.That(_asked, Is.EqualTo(new[] { _minePing.Id }));
    }

    [Test]
    public void NoNoticeWhenNoTabIsAffected()
    {
        var viewModel = Create([_mine]);
        viewModel.IsExistingCollection = true;
        viewModel.Overwrite = true;

        Assert.That(viewModel.TabsNotice, Is.Empty);
    }

    [Test]
    public void ADocumentWithCompleteUrlsHasNoBaseUrl()
    {
        var viewModel = Create([]);

        Assert.That(viewModel.HasBaseUrl, Is.False);
        Assert.That(viewModel.Request()?.BaseUrl, Is.Null);
        Assert.That(viewModel.CanImport, Is.True);
    }

    [TestCase("https://api.example.com/v1/", "https://api.example.com/v1")]
    [TestCase("/api", "{{base_url}}/api")]
    [TestCase("", "{{base_url}}")]
    public void TheBaseUrlIsFilledInFromTheDocument(string declared, string expected)
    {
        var viewModel = CreateWithBaseUrl(declared);

        Assert.That(viewModel.HasBaseUrl, Is.True);
        Assert.That(viewModel.BaseUrl, Is.EqualTo(expected));
        Assert.That(viewModel.Request()?.BaseUrl, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void ImportIsDisabledWhileTheBaseUrlIsEmpty(string? typed)
    {
        var viewModel = CreateWithBaseUrl("/api");

        viewModel.BaseUrl = typed!;

        Assert.That(viewModel.CanImport, Is.False);
        Assert.That(viewModel.Request(), Is.Null);
        viewModel.BaseUrl = "  {{host}} ";
        Assert.That(viewModel.CanImport, Is.True);
        Assert.That(viewModel.Request()?.BaseUrl, Is.EqualTo("{{host}}"));
    }

    // A downloaded document without a server address, or with a relative one, takes the address it came from.
    [TestCase("", null, "http://api.test:8080")]
    [TestCase("/v2", null, "http://api.test:8080/v2")]
    [TestCase("https://other.test/v2", null, "https://other.test/v2")]
    [TestCase("/v2", "{{base_url}}", "{{base_url}}")]
    [TestCase("https://other.test/v2", "http://localhost:5000", "http://localhost:5000")]
    public void TheBaseUrlOfADownloadIsTheRememberedOneOrComesFromItsAddress(string declared, string? remembered, string expected)
    {
        var downloaded = new Uri("http://api.test:8080/swagger/v1/swagger.json");

        var viewModel = new ImportPreviewViewModel(_document with { BaseUrl = declared }, [], null, CountTabs, downloaded, remembered);

        Assert.That(viewModel.BaseUrl, Is.EqualTo(expected));
        Assert.That(viewModel.Request()?.BaseUrl, Is.EqualTo(expected));
    }

    [Test]
    public void ARememberedBaseUrlIsIgnoredForADocumentWithCompleteUrls()
    {
        var viewModel = new ImportPreviewViewModel(_document, [], null, CountTabs, new Uri("http://api.test/"), "http://localhost");

        Assert.That(viewModel.HasBaseUrl, Is.False);
        Assert.That(viewModel.Request()?.BaseUrl, Is.Null);
    }

    private ImportPreviewViewModel CreateWithBaseUrl(string declared)
    {
        return new ImportPreviewViewModel(_document with { BaseUrl = declared }, [], null, CountTabs);
    }

    private ImportPreviewViewModel Create(IReadOnlyList<Collection> existing)
    {
        return new ImportPreviewViewModel(_document, existing, null, CountTabs);
    }

    private int CountTabs(IReadOnlyCollection<Guid> requestIds)
    {
        _asked = requestIds;
        return _tabsWithEdits;
    }

    private static string StatusOf(ImportPreviewViewModel viewModel, Guid id)
    {
        return viewModel.Tree.AllNodes().Single(node => node.Id == id).StatusText;
    }
}
