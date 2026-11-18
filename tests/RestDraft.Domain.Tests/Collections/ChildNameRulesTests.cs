using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;

namespace RestDraft.Domain.Tests.Collections;

[TestFixture]
public class ChildNameRulesTests
{
    private static readonly Guid OrderId = Guid.NewGuid();

    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("Users", true)]
    public void ValidateNameRejectsBlankNames(string name, bool isValid)
    {
        var result = ChildNameRules.ValidateName(name);

        Assert.That(result.IsSuccess, Is.EqualTo(isValid));
    }

    [TestCase("Users", false)]
    [TestCase("users", false)]
    [TestCase("  Orders  ", false)]
    [TestCase("Admin", true)]
    public void ValidateChildNameRejectsSiblingNamesCaseInsensitively(string name, bool isValid)
    {
        var result = ChildNameRules.ValidateChildName(name, CreateParent(), null);

        Assert.That(result.IsSuccess, Is.EqualTo(isValid));
    }

    [Test]
    public void ValidateChildNameAllowsKeepingTheNameOfTheItemBeingRenamed()
    {
        var result = ChildNameRules.ValidateChildName("Orders", CreateParent(), OrderId);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void ValidateChildNameRejectsNameOfAnotherItemWhenRenaming()
    {
        var result = ChildNameRules.ValidateChildName("Users", CreateParent(), OrderId);

        Assert.That(result.Error?.Category, Is.EqualTo(ErrorCategory.Validation));
    }

    [Test]
    public void NextAvailableNameReturnsBaseNameWhenFree()
    {
        var name = ChildNameRules.NextAvailableName("New request", CreateParent());

        Assert.That(name, Is.EqualTo("New request"));
    }

    [Test]
    public void NextAvailableNameAddsFirstFreeSuffix()
    {
        var parent = new Folder(Guid.NewGuid(), "Parent", [], [new RequestDefinition { Id = Guid.NewGuid(), Name = "New request" }]);

        var name = ChildNameRules.NextAvailableName("New request", parent);

        Assert.That(name, Is.EqualTo("New request 2"));
    }

    [TestCase("List", "List", "List 2")]
    [TestCase("list", "List", "list 2")]
    [TestCase("List", " List ", "List 2")]
    [TestCase("List", "Other", "List")]
    public void NextAvailableNameAmongNamesIgnoresCaseAndSpaces(string baseName, string taken, string expected)
    {
        Assert.That(ChildNameRules.NextAvailableName(baseName, [taken]), Is.EqualTo(expected));
    }

    [Test]
    public void NextAvailableNameAmongNamesSkipsTakenSuffixes()
    {
        Assert.That(ChildNameRules.NextAvailableName("List", ["List", "list 2", "List 3"]), Is.EqualTo("List 4"));
    }

    [TestCase("API", " api ", true)]
    [TestCase("API", "apis", false)]
    public void IsSameNameIgnoresCaseAndSurroundingSpaces(string first, string second, bool expected)
    {
        Assert.That(ChildNameRules.IsSameName(first, second), Is.EqualTo(expected));
    }

    [Test]
    public void NewFolderNameSkipsTakenSuffixes()
    {
        var parent = new Folder(Guid.NewGuid(), "Parent", [new Folder(Guid.NewGuid(), "New folder", [], [])], []);
        var taken = new Folder(Guid.NewGuid(), "New folder 2", [], []);
        parent = parent with { Folders = [.. parent.Folders, taken] };

        var name = ChildNameRules.NewFolderName(parent);

        Assert.That(name, Is.EqualTo("New folder 3"));
    }

    private static Folder CreateParent()
    {
        var users = new Folder(Guid.NewGuid(), "Users", [], []);
        var orders = new RequestDefinition { Id = OrderId, Name = "Orders" };
        return new Folder(Guid.NewGuid(), string.Empty, [users], [orders]);
    }
}
