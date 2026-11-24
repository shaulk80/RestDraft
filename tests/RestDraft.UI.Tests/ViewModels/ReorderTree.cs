using RestDraft.Domain.Collections;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

// One collection "C": folders F1 (with the requests N1 and N2), F2 and F3, then the requests R1, R2 and R3.
internal sealed class ReorderTree
{
    private ReorderTree(TreeFixtures fixtures, CollectionsTreeViewModel sidebar, Guid rootFolderId)
    {
        Fixtures = fixtures;
        Sidebar = sidebar;
        RootFolderId = rootFolderId;
    }

    public TreeFixtures Fixtures { get; }

    public CollectionsTreeViewModel Sidebar { get; }

    public Guid RootFolderId { get; }

    public static async Task<ReorderTree> CreateAsync()
    {
        var first = new RequestDefinition { Id = Guid.NewGuid(), Name = "N1" };
        var second = new RequestDefinition { Id = Guid.NewGuid(), Name = "N2" };
        var f1 = new Folder(Guid.NewGuid(), "F1", [], [first, second]);
        var root = new Folder(
            Guid.NewGuid(),
            string.Empty,
            [f1, TreeFixtures.CreateFolder("F2"), TreeFixtures.CreateFolder("F3")],
            [Request("R1"), Request("R2"), Request("R3")]);
        var fixtures = new TreeFixtures();
        fixtures.SetCollections([new Collection(Guid.NewGuid(), "C", root)]);
        var sidebar = fixtures.CreateSidebar();
        await sidebar.ReloadAsync();
        return new ReorderTree(fixtures, sidebar, root.Id);
    }

    public TreeNodeViewModel Node(string name)
    {
        return Flatten(Sidebar.Roots).First(node => node.Name == name);
    }

    private static RequestDefinition Request(string name)
    {
        return new RequestDefinition { Id = Guid.NewGuid(), Name = name };
    }

    private static IEnumerable<TreeNodeViewModel> Flatten(IEnumerable<TreeNodeViewModel> nodes)
    {
        return nodes.SelectMany(node => new[] { node }.Concat(Flatten(node.Children)));
    }
}
