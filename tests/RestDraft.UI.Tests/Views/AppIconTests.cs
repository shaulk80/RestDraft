using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

[TestFixture]
public class AppIconTests
{
    private Window? _window;

    [TearDown]
    public void TearDown()
    {
        _window?.Close();
    }

    [TestCaseSource(nameof(EveryWindow))]
    [AvaloniaTest]
    public void EveryWindowHasTheAppIcon(Func<Window> createWindow)
    {
        Show(createWindow());

        Assert.That(_window!.Icon, Is.Not.Null);
    }

    [AvaloniaTest]
    public void AboutDialogShowsTheAppIcon()
    {
        Show(new AboutWindow());

        var image = _window!.GetVisualDescendants().OfType<Image>().Single();

        Assert.That(image.Source, Is.Not.Null);
    }

    private static IEnumerable<TestCaseData> EveryWindow()
    {
        yield return Case<MainWindow>(() => new MainWindow());
        yield return Case<AboutWindow>(() => new AboutWindow());
        yield return Case<ConfirmationWindow>(() => new ConfirmationWindow());
        yield return Case<EnvironmentEditorWindow>(() => new EnvironmentEditorWindow());
        yield return Case<ExportSelectionWindow>(() => new ExportSelectionWindow());
        yield return Case<ImportPreviewWindow>(() => new ImportPreviewWindow());
        yield return Case<ImportUrlWindow>(() => new ImportUrlWindow());
        yield return Case<MasterPasswordSetupWindow>(() => new MasterPasswordSetupWindow());
        yield return Case<SaveChangesWindow>(() => new SaveChangesWindow());
        yield return Case<SaveDestinationWindow>(() => new SaveDestinationWindow());
        yield return Case<SettingsWindow>(() => new SettingsWindow());
        yield return Case<UnlockDialogWindow>(() => new UnlockDialogWindow());
    }

    // A factory instead of a Type, so the test creates the window without reflection.
    private static TestCaseData Case<TWindow>(Func<Window> createWindow)
        where TWindow : Window
    {
        return new TestCaseData(createWindow).SetArgDisplayNames(typeof(TWindow).Name);
    }

    private void Show(Window window)
    {
        _window = window;
        window.Show();
        Dispatcher.UIThread.RunJobs();
    }
}
