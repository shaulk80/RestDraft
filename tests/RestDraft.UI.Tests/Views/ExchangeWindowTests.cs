using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.UI.Services;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

[TestFixture]
public class ExchangeWindowTests
{
    private Window? _owner;
    private Collection _shop = null!;
    private RequestDefinition _login = null!;

    [SetUp]
    public void SetUp()
    {
        _login = new RequestDefinition { Id = Guid.NewGuid(), Name = "Login" };
        var users = new Folder(Guid.NewGuid(), "Users", [], [_login]);
        _shop = new Collection(Guid.NewGuid(), "Shop", new Folder(Guid.NewGuid(), string.Empty, [users], []));
    }

    [TearDown]
    public void TearDown()
    {
        _owner?.Close();
    }

    [AvaloniaTest]
    public void ExportWindowWithItsTreeHasNamedControls()
    {
        var window = new ExportSelectionWindow { DataContext = new ExportSelectionViewModel([_shop], _shop.Id) };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(AccessibleNameChecker.FindUnnamed(window), Is.Empty);
        window.Close();
    }

    [AvaloniaTest]
    public void ImportWindowWithItsTreeHasNamedControls()
    {
        var window = new ImportPreviewWindow { DataContext = CreatePreview() };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(AccessibleNameChecker.FindUnnamed(window), Is.Empty);
        window.Close();
    }

    [AvaloniaTest]
    public async Task ExportButtonClosesTheWindowWithTheSelection()
    {
        var window = new ExportSelectionWindow { DataContext = new ExportSelectionViewModel([_shop], _shop.Id) };
        var result = ShowAsDialog<ExportSelection?>(window);

        Click(window, "Export");

        var selection = await result;
        Assert.That(selection?.CollectionId, Is.EqualTo(_shop.Id));
        Assert.That(selection?.TickedIds, Does.Contain(_login.Id));
    }

    [AvaloniaTest]
    public async Task CancelClosesTheExportWindowWithNothing()
    {
        var window = new ExportSelectionWindow { DataContext = new ExportSelectionViewModel([_shop], _shop.Id) };
        var result = ShowAsDialog<ExportSelection?>(window);

        Click(window, "Cancel");

        Assert.That(await result, Is.Null);
    }

    [AvaloniaTest]
    public async Task ImportButtonClosesTheWindowWithTheRequest()
    {
        var window = new ImportPreviewWindow { DataContext = CreatePreview() };
        var result = ShowAsDialog<ImportRequest?>(window);

        Click(window, "Import");

        var request = await result;
        Assert.That(request?.Target.IsNewCollection, Is.True);
        Assert.That(request?.TickedIds, Does.Contain(_login.Id));
    }

    [AvaloniaTest]
    public async Task CancelClosesTheImportWindowWithNothing()
    {
        var window = new ImportPreviewWindow { DataContext = CreatePreview() };
        var result = ShowAsDialog<ImportRequest?>(window);

        Click(window, "Cancel");

        Assert.That(await result, Is.Null);
    }

    [AvaloniaTest]
    public void ImportButtonIsDisabledWhenNothingIsTicked()
    {
        var viewModel = CreatePreview();
        var window = new ImportPreviewWindow { DataContext = viewModel };
        window.Show();

        viewModel.ClearAllCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.That(FindButton(window, "Import").IsEnabled, Is.False);
        window.Close();
    }

    [AvaloniaTest]
    [TestCase(null, false)]
    [TestCase("/api", true)]
    public void TheImportWindowShowsTheBaseUrlOnlyForADocumentThatHasOne(string? declared, bool isShown)
    {
        var window = new ImportPreviewWindow { DataContext = CreatePreview(declared) };

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.That(BaseUrlBox(window).IsEffectivelyVisible, Is.EqualTo(isShown));
        Assert.That(AccessibleNameChecker.FindUnnamed(window), Is.Empty);
        window.Close();
    }

    [AvaloniaTest]
    public async Task ClearingTheBaseUrlBoxDisablesImportAndTheTypedOneIsImported()
    {
        var window = new ImportPreviewWindow { DataContext = CreatePreview("/api") };
        var result = ShowAsDialog<ImportRequest?>(window);
        var box = BaseUrlBox(window);

        box.Text = string.Empty;
        Dispatcher.UIThread.RunJobs();
        var enabledWhenEmpty = FindButton(window, "Import").IsEnabled;
        box.Text = "http://localhost:5000";
        Dispatcher.UIThread.RunJobs();
        Click(window, "Import");

        Assert.That(enabledWhenEmpty, Is.False);
        Assert.That((await result)?.BaseUrl, Is.EqualTo("http://localhost:5000"));
    }

    [AvaloniaTest]
    public void SpaceTogglesTheSelectedNodeOfTheTree()
    {
        var viewModel = new ExportSelectionViewModel([_shop], _shop.Id);
        var window = new ExportSelectionWindow { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var tree = window.GetVisualDescendants().OfType<TreeView>().Single();
        var folder = viewModel.Tree.AllNodes().Single(node => node.Title == "Users");
        var row = tree.GetVisualDescendants().OfType<TreeViewItem>().First(item => item.DataContext == folder);
        tree.SelectedItem = folder;
        row.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);

        Assert.That(folder.IsChecked, Is.False);
        Assert.That(viewModel.CanExport, Is.False);
        window.Close();
    }

    [AvaloniaTest]
    public void ASingleClickOnTheTextSelectsTheRowAndLeavesTheCheckBoxAlone()
    {
        var (window, folder) = ShowExportWithFolder();

        Click(window, TextOf(window, "Users"), clicks: 1);

        Assert.That(folder.IsChecked, Is.True);
        Assert.That(folder.IsExpanded, Is.True);
        Assert.That(window.GetVisualDescendants().OfType<TreeView>().Single().SelectedItem, Is.SameAs(folder));
        window.Close();
    }

    [AvaloniaTest]
    public void ADoubleClickOnTheTextCollapsesTheNodeAndLeavesTheCheckBoxAlone()
    {
        var (window, folder) = ShowExportWithFolder();

        Click(window, TextOf(window, "Users"), clicks: 2);

        Assert.That(folder.IsExpanded, Is.False);
        Assert.That(folder.IsChecked, Is.True);
        window.Close();
    }

    [AvaloniaTest]
    public void ADoubleClickOnTheCheckBoxTogglesItTwiceAndLeavesTheNodeExpanded()
    {
        var (window, folder) = ShowExportWithFolder();

        Click(window, CheckBoxOf(window, "Users"), clicks: 2);

        Assert.That(folder.IsChecked, Is.True);
        Assert.That(folder.IsExpanded, Is.True);
        window.Close();
    }

    [AvaloniaTest]
    public void ASingleClickOnTheCheckBoxTogglesItAndLeavesTheNodeExpanded()
    {
        var (window, folder) = ShowExportWithFolder();

        Click(window, CheckBoxOf(window, "Users"), clicks: 1);

        Assert.That(folder.IsChecked, Is.False);
        Assert.That(folder.IsExpanded, Is.True);
        window.Close();
    }

    private ImportPreviewViewModel CreatePreview(string? baseUrl = null)
    {
        var document = new ExchangeDocument(_shop with { Id = Guid.NewGuid() }, [new ImportWarning("Left out.", "Users / Login")], baseUrl);
        return new ImportPreviewViewModel(document, [], null, _ => 0);
    }

    private static TextBox BaseUrlBox(Window window)
    {
        return window.GetVisualDescendants().OfType<TextBox>()
            .Single(box => AutomationProperties.GetName(box) == "Base URL of the requests");
    }

    // A dialog needs an owner window. The dialog is on screen when this returns.
    private Task<T> ShowAsDialog<T>(Window dialog)
    {
        _owner = new Window();
        _owner.Show();
        var result = dialog.ShowDialog<T>(_owner);
        Dispatcher.UIThread.RunJobs();
        return result;
    }

    // The box of a check box is centered in a cell that is taller than a line of text. A text block that fills the row draws its
    // text at the top, even though its bounds look centered, so the alignment itself is checked.
    [AvaloniaTest]
    public void TheTextOfARowIsCenteredVertically()
    {
        var (window, _) = ShowExportWithFolder();

        foreach (var text in new[] { "Login", "GET" })
        {
            Assert.That(TextOf(window, text).VerticalAlignment, Is.EqualTo(VerticalAlignment.Center), text);
        }

        window.Close();
    }

    private (Window Window, SelectableNodeViewModel Folder) ShowExportWithFolder()
    {
        var viewModel = new ExportSelectionViewModel([_shop], _shop.Id);
        var window = new ExportSelectionWindow { DataContext = viewModel };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel.Tree.AllNodes().Single(node => node.Title == "Users"));
    }

    private static Control TextOf(Window window, string text)
    {
        return window.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == text);
    }

    private static Control CheckBoxOf(Window window, string title)
    {
        return window.GetVisualDescendants().OfType<CheckBox>().First(box => ((SelectableNodeViewModel)box.DataContext!).Title == title);
    }

    // Presses and releases the left button in the middle of the control, as many times as asked, in quick succession.
    private static void Click(Window window, Control target, int clicks)
    {
        var point = Avalonia.VisualExtensions.TranslatePoint(target, new Avalonia.Point(target.Bounds.Width / 2, target.Bounds.Height / 2), window)!.Value;
        for (var click = 0; click < clicks; click++)
        {
            window.MouseDown(point, Avalonia.Input.MouseButton.Left);
            window.MouseUp(point, Avalonia.Input.MouseButton.Left);
        }

        Dispatcher.UIThread.RunJobs();
    }

    private static void Click(Window window, string text)
    {
        var button = FindButton(window, text);
        button.Command?.Execute(null);
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static Button FindButton(Window window, string text)
    {
        return window.GetVisualDescendants().OfType<Button>().Single(button => button.Content as string == text);
    }
}
