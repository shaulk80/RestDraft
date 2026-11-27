using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FakeItEasy;
using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Results;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

// "New request" in the tree: the name is edited at once, and the keyboard then goes on to the URL.
public partial class MainWindowSmokeTests
{
    private const string NewRequestName = "New request";

    [AvaloniaTest]
    public async Task NewRequestPutsTheFocusInItsNameBoxWithTheNameSelected()
    {
        OpenWindow();

        var created = await CreateRequestInTheTreeAsync();

        var box = FocusedTextBox();
        Assert.That(box.DataContext, Is.InstanceOf<RequestNodeViewModel>());
        Assert.That(((RequestNodeViewModel)box.DataContext!).Id, Is.EqualTo(created));
        Assert.That(box.SelectedText, Is.EqualTo(NewRequestName));
        Assert.That(_viewModel.TabStrip.SelectedTab?.RequestId, Is.EqualTo(created));
    }

    [TestCase(PhysicalKey.Enter, 1)]
    [TestCase(PhysicalKey.Escape, 0)]
    [AvaloniaTest]
    public async Task EnterAndEscapeInTheNewNameMoveTheFocusToTheUrlBox(PhysicalKey key, int expectedRenames)
    {
        OpenWindow();
        var created = await CreateRequestInTheTreeAsync();
        FocusedTextBox().Text = "Sign in";

        _window.KeyPressQwerty(key, RawInputModifiers.None);
        await SettleAsync();

        Assert.That(AutomationProperties.GetName(FocusedTextBox()), Is.EqualTo("Request URL"));
        A.CallTo(() => _fixtures.Requests.RenameAsync(created, "Sign in", A<CancellationToken>._))
            .MustHaveHappened(expectedRenames, Times.Exactly);
    }

    [AvaloniaTest]
    public async Task TheTabTakesTheNewName()
    {
        OpenWindow();
        await CreateRequestInTheTreeAsync();
        FocusedTextBox().Text = "Sign in";

        _window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        await SettleAsync();

        Assert.That(_viewModel.TabStrip.SelectedTab?.Name, Is.EqualTo("Sign in"));
        Assert.That(_viewModel.TabStrip.SelectedTab?.IsDirty, Is.False);
    }

    [AvaloniaTest]
    public async Task ClickingElsewhereSavesTheNewNameAndLeavesTheFocusThere()
    {
        OpenWindow();
        var created = await CreateRequestInTheTreeAsync();
        FocusedTextBox().Text = "Sign in";
        var methodBox = _window.GetVisualDescendants().OfType<ComboBox>()
            .First(candidate => AutomationProperties.GetName(candidate) == "HTTP method");

        methodBox.Focus();
        await SettleAsync();

        Assert.That(_window.FocusManager?.GetFocusedElement(), Is.SameAs(methodBox));
        A.CallTo(() => _fixtures.Requests.RenameAsync(created, "Sign in", A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [AvaloniaTest]
    public async Task NewRequestFromTheContextMenuPutsTheFocusInItsNameBox()
    {
        OpenWindow();

        var created = await CreateRequestInTheTreeAsync(byContextMenu: true);

        var box = FocusedTextBox();
        Assert.That((box.DataContext as RequestNodeViewModel)?.Id, Is.EqualTo(created));
        Assert.That(box.SelectedText, Is.EqualTo(NewRequestName));
    }

    [AvaloniaTest]
    public async Task TabFromTheNewNameSavesItAndKeepsAFocusedControl()
    {
        OpenWindow();
        var created = await CreateRequestInTheTreeAsync();
        FocusedTextBox().Text = "Sign in";

        _window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        await SettleAsync();

        A.CallTo(() => _fixtures.Requests.RenameAsync(created, "Sign in", A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        var focused = _window.FocusManager?.GetFocusedElement() as Control;
        Assert.That(focused, Is.Not.Null);
        Assert.That(TopLevel.GetTopLevel(focused), Is.Not.Null, "the focused control is still on screen");
        Assert.That(AutomationProperties.GetName(focused), Is.Not.EqualTo("Request URL"));
    }

    // Creates a request in the collection, with the toolbar button or the context menu. The stored requests answer
    // after a moment, as the database does, and a rename changes what the tree loads next.
    private async Task<Guid> CreateRequestInTheTreeAsync(bool byContextMenu = false)
    {
        var definition = new RequestDefinition { Id = Guid.NewGuid(), Name = NewRequestName };
        A.CallTo(() => _fixtures.Requests.CreateAsync(A<Guid>._, A<CancellationToken>._))
            .Invokes(() => SetRootRequest(definition))
            .Returns(Task.FromResult(Result<Guid>.Success(definition.Id)));
        A.CallTo(() => _fixtures.Requests.GetAsync(definition.Id, A<CancellationToken>._))
            .ReturnsLazily(async () =>
            {
                await Task.Delay(1);
                return Result<RequestDefinition>.Success(definition);
            });
        A.CallTo(() => _fixtures.Requests.RenameAsync(definition.Id, A<string>._, A<CancellationToken>._))
            .Invokes((Guid _, string name, CancellationToken _) => SetRootRequest(definition with { Name = name }))
            .Returns(Task.FromResult(Result.Success()));
        if (byContextMenu)
        {
            ClickMenuOfTheCollection(NewRequestName);
        }
        else
        {
            ClickToolbarButton(NewRequestName);
        }

        await SettleAsync();

        return definition.Id;
    }

    private void ClickToolbarButton(string name)
    {
        _viewModel.Sidebar.SelectedNode = _viewModel.Sidebar.Roots[0];
        Dispatcher.UIThread.RunJobs();
        Click(_window.GetVisualDescendants().OfType<Button>()
            .First(candidate => AutomationProperties.GetName(candidate) == name));
    }

    private void ClickMenuOfTheCollection(string header)
    {
        var collection = _viewModel.Sidebar.Roots[0];
        var label = _window.GetVisualDescendants().OfType<TextBlock>()
            .First(block => block.Text == collection.Name && ReferenceEquals(block.DataContext, collection));
        var center = label.TranslatePoint(new Point(label.Bounds.Width / 2, label.Bounds.Height / 2), _window)!.Value;
        _window.MouseDown(center, MouseButton.Right);
        _window.MouseUp(center, MouseButton.Right);
        Dispatcher.UIThread.RunJobs();
        var tree = _window.GetVisualDescendants().OfType<CollectionsTreeView>().Single();
        Click(tree.GetLogicalDescendants().OfType<MenuItem>().First(item => item.Header as string == header));
    }

    private void SetRootRequest(RequestDefinition request)
    {
        var root = _fixtures.Root with { Requests = [request] };
        _fixtures.SetCollections([_fixtures.Shop with { Root = root }]);
    }

    private TextBox FocusedTextBox()
    {
        var focused = _window.FocusManager?.GetFocusedElement();
        Assert.That(focused, Is.InstanceOf<TextBox>());
        return (TextBox)focused!;
    }

    private static async Task SettleAsync()
    {
        await Task.Delay(50);
        Dispatcher.UIThread.RunJobs();
    }
}
