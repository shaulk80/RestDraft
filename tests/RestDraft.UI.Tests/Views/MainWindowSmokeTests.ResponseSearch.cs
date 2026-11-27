using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Responses;
using RestDraft.UI.Controls;
using RestDraft.UI.ViewModels;
using RestDraft.UI.Views;

namespace RestDraft.UI.Tests.Views;

// The search panel above the response body, driven with the keyboard and mouse.
public partial class MainWindowSmokeTests
{
    // "token" is at 0, 6 and 12.
    private const string ResponseText = "token Token token";
    private static readonly string[] CountersOfThreePresses = ["2 of 3", "3 of 3", "1 of 3"];

    [AvaloniaTest]
    public void CtrlFInTheResponseBodyOpensTheSearchAndNotTheFilter()
    {
        OpenWindowWithResponse(ResponseText);
        FocusResponseBody();

        PressCtrlF();

        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.True);
        Assert.That(FindPopup(), Is.Null, "the filter stays closed");
        Assert.That(FindQueryBox().IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void CtrlFOutsideTheResponseOpensTheFilter()
    {
        OpenWindowWithResponse(ResponseText);
        FocusUrlBox();

        PressCtrlF();

        Assert.That(FindPopup(), Is.Not.Null);
        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.False);
    }

    [AvaloniaTest]
    public void CtrlFInTheSearchPanelSelectsTheWholeSearchText()
    {
        OpenResponseSearch("token");
        var box = FindQueryBox();
        box.CaretIndex = 2;
        box.SelectionStart = 2;
        box.SelectionEnd = 2;

        PressCtrlF();

        Assert.That(box.SelectedText, Is.EqualTo("token"));
        Assert.That(FindPopup(), Is.Null);
    }

    [AvaloniaTest]
    public void ASelectionOfOneLineFillsTheSearchBoxAndIsTheCurrentMatch()
    {
        OpenWindowWithResponse(ResponseText);
        var editor = FocusResponseBody();
        editor.Select(6, 5);

        PressCtrlF();

        Assert.That(FindQueryBox().Text, Is.EqualTo("Token"));
        Assert.That(ResponseSearch.CounterText, Is.EqualTo("2 of 3"));
    }

    [AvaloniaTest]
    public void TypingSearchesAndKeepsTheFocusInTheBox()
    {
        OpenWindowWithResponse(ResponseText);
        FocusResponseBody();
        PressCtrlF();

        _window.KeyTextInput("tok");
        Dispatcher.UIThread.RunJobs();

        Assert.That(FindCounter("1 of 3").IsEffectivelyVisible, Is.True);
        Assert.That(FindQueryBox().IsFocused, Is.True);
    }

    [TestCase(PhysicalKey.F3, RawInputModifiers.None, "2 of 3")]
    [TestCase(PhysicalKey.F3, RawInputModifiers.Shift, "3 of 3")]
    [TestCase(PhysicalKey.Enter, RawInputModifiers.None, "2 of 3")]
    [TestCase(PhysicalKey.Enter, RawInputModifiers.Shift, "3 of 3")]
    [AvaloniaTest]
    public void KeysInTheSearchBoxStepThroughTheMatches(PhysicalKey key, RawInputModifiers modifiers, string expected)
    {
        OpenResponseSearch("token");

        Press(key, modifiers);

        Assert.That(ResponseSearch.CounterText, Is.EqualTo(expected));
        Assert.That(FindQueryBox().IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void F3InTheResponseBodyCyclesThroughTheMatches()
    {
        OpenResponseSearch("token");
        var editor = FocusResponseBody();
        var counters = new List<string>();

        for (var press = 0; press < 3; press++)
        {
            Press(PhysicalKey.F3, RawInputModifiers.None);
            counters.Add(ResponseSearch.CounterText);
        }

        Assert.That(counters, Is.EqualTo(CountersOfThreePresses));
        Assert.That(editor.IsKeyboardFocusWithin, Is.True);
    }

    // The cursor is after the first match, so the next match is the second one and the previous one is the first.
    [TestCase(RawInputModifiers.None, "2 of 3")]
    [TestCase(RawInputModifiers.Shift, "1 of 3")]
    [AvaloniaTest]
    public void F3WithTheSearchClosedOpensItWithTheLastTextAtTheCursor(RawInputModifiers modifiers, string expected)
    {
        OpenResponseSearch("token");
        Press(PhysicalKey.Escape, RawInputModifiers.None);
        var editor = FindResponseBody();
        editor.Select(0, 0);
        editor.CaretOffset = 5;

        Press(PhysicalKey.F3, modifiers);

        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.True);
        Assert.That(ResponseSearch.Query, Is.EqualTo("token"));
        Assert.That(ResponseSearch.CounterText, Is.EqualTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    [AvaloniaTest]
    public void EscapeClosesTheSearchAndSelectsTheCurrentMatch(bool fromBody)
    {
        OpenResponseSearch("token");
        Press(PhysicalKey.F3, RawInputModifiers.None);
        if (fromBody)
        {
            FocusResponseBody();
        }

        Press(PhysicalKey.Escape, RawInputModifiers.None);

        var editor = FindResponseBody();
        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.False);
        Assert.That(editor.SelectionStart, Is.EqualTo(6));
        Assert.That(editor.SelectedText, Is.EqualTo("Token"));
        Assert.That(editor.TextArea.IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void CloseButtonClosesTheSearchAndSelectsTheCurrentMatch()
    {
        OpenResponseSearch("token");
        var close = FindSearchPanel().GetVisualDescendants().OfType<Button>()
            .First(button => AutomationProperties.GetName(button) == "Close search");

        Click(close);

        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.False);
        Assert.That(FindResponseBody().SelectedText, Is.EqualTo("token"));
        Assert.That(FindResponseBody().TextArea.IsFocused, Is.True);
    }

    [AvaloniaTest]
    public void EscapeInTheBodyStillLeavesItWhileTheSearchIsClosed()
    {
        OpenWindowWithResponse(ResponseText);
        var editor = FocusResponseBody();

        Press(PhysicalKey.Escape, RawInputModifiers.None);

        Assert.That(editor.IsKeyboardFocusWithin, Is.False);
    }

    [AvaloniaTest]
    public void TheEditorHasNoFindBoxOfItsOwn()
    {
        OpenWindowWithResponse(ResponseText);
        FocusResponseBody();

        Press(PhysicalKey.H, RawInputModifiers.Control);
        Press(PhysicalKey.F3, RawInputModifiers.None);

        Assert.That(_window.GetVisualDescendants().OfType<SearchPanel>().Where(panel => panel.IsEffectivelyVisible), Is.Empty);
    }

    [AvaloniaTest]
    public void EachTabKeepsItsOwnSearch()
    {
        var other = new RequestDefinition { Id = Guid.NewGuid(), Name = "Other", Url = "https://example.com/other" };
        OpenWindow(other);
        var tabs = _viewModel.TabStrip.Tabs;
        tabs[0].Response.Show(CreateTextResponse(ResponseText));
        tabs[1].Response.Show(CreateTextResponse("plain words"));
        Dispatcher.UIThread.RunJobs();
        FocusResponseBody();
        PressCtrlF();
        _window.KeyTextInput("token");
        Press(PhysicalKey.F3, RawInputModifiers.None);

        SelectTab(1);
        var shownOnOther = FindSearchPanel().IsEffectivelyVisible;
        SelectTab(0);

        Assert.That(shownOnOther, Is.False);
        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.True);
        Assert.That(FindQueryBox().Text, Is.EqualTo("token"));
        Assert.That(FindCounter("2 of 3").IsEffectivelyVisible, Is.True);
    }

    // The focus is in the search box while the tab changes to one without a search, as with Ctrl+Tab.
    [AvaloniaTest]
    public void AHiddenSearchPanelTakesNoTyping()
    {
        var other = new RequestDefinition { Id = Guid.NewGuid(), Name = "Other", Url = "https://example.com/other" };
        OpenWindow(other);
        FocusResponseBody();
        PressCtrlF();

        SelectTab(1);
        _window.KeyTextInput("typed");
        Dispatcher.UIThread.RunJobs();

        Assert.That(FindSearchPanel().IsEffectivelyVisible, Is.False);
        Assert.That(FindSearchPanel().IsKeyboardFocusWithin, Is.False);
        Assert.That(ResponseSearch.Query, Is.Empty);
    }

    [AvaloniaTest]
    public void TheCurrentMatchIsScrolledIntoView()
    {
        var lines = string.Join('\n', Enumerable.Range(1, 400).Select(number => $"line {number}"));
        OpenWindowWithResponse($"{lines}\nneedle");
        var editor = FindResponseBody();
        FocusResponseBody();
        PressCtrlF();

        _window.KeyTextInput("needle");
        Dispatcher.UIThread.RunJobs();

        var textView = editor.TextArea.TextView;
        textView.EnsureVisualLines();
        Assert.That(textView.VisualLines[^1].LastDocumentLine.LineNumber, Is.EqualTo(401));
        Assert.That(textView.ScrollOffset.Y, Is.GreaterThan(0));
    }

    [TestCase(false)]
    [TestCase(true)]
    [AvaloniaTest]
    public void OnlyTheMatchesOnScreenArePainted(bool isOneLine)
    {
        var separator = isOneLine ? ' ' : '\n';
        OpenResponseSearch("word", string.Join(separator, Enumerable.Repeat("word", 5000)));
        var textView = FindResponseBody().TextArea.TextView;
        textView.EnsureVisualLines();

        var painted = SearchHighlightRenderer.Highlights(textView, ResponseSearch.Matches).ToList();

        Assert.That(ResponseSearch.Matches, Has.Count.EqualTo(5000));
        Assert.That(painted, Has.Count.InRange(1, 1000));
        Assert.That(painted[0].Match, Is.EqualTo(ResponseSearch.Matches[0]));
    }

    // The places are compared with those the editor itself computes, at the start and far down a text that wraps.
    [TestCase("word ", false)]
    [TestCase("word ", true)]
    [TestCase("a word that is long enough to wrap\n", false)]
    [TestCase("a word that is long enough to wrap\n", true)]
    [TestCase("\tword中文 word-", true)]
    [AvaloniaTest]
    public void MatchesArePaintedWhereTheirTextIs(string part, bool atTheEnd)
    {
        OpenResponseSearch("word", string.Concat(Enumerable.Repeat(part, 400)));
        if (atTheEnd)
        {
            ResponseSearch.SelectPrevious();
            Dispatcher.UIThread.RunJobs();
        }

        var textView = FindResponseBody().TextArea.TextView;
        textView.EnsureVisualLines();

        var painted = SearchHighlightRenderer.Highlights(textView, ResponseSearch.Matches).ToList();

        Assert.That(painted, Is.Not.Empty);
        Assert.That(painted.Select(highlight => highlight.Match), atTheEnd ? Does.Contain(ResponseSearch.Matches[^1]) : Does.Contain(ResponseSearch.Matches[0]));
        foreach (var match in painted.GroupBy(highlight => highlight.Match))
        {
            var expected = BackgroundGeometryBuilder.GetRectsForSegment(textView, new SimpleSegment(match.Key.Start, match.Key.Length));
            var areas = match.Select(highlight => highlight.Area).ToList();
            // A match that is half out of view is painted only in the row that is in view.
            Assert.That(areas, Is.SubsetOf(expected.ToList()).Using<Rect>(AreSamePlace), $"match at {match.Key.Start}");
        }
    }

    private static bool AreSamePlace(Rect first, Rect second)
    {
        return Math.Abs(first.X - second.X) < 0.5
            && Math.Abs(first.Y - second.Y) < 0.5
            && Math.Abs(first.Width - second.Width) < 0.5
            && Math.Abs(first.Height - second.Height) < 0.5;
    }

    [TestCase("nothing", true)]
    [TestCase("token", false)]
    [AvaloniaTest]
    public void ASearchWithoutAMatchMarksTheBox(string query, bool isMarked)
    {
        OpenResponseSearch(query);
        var box = FindQueryBox();
        var border = box.GetVisualDescendants().OfType<Border>().First(candidate => candidate.Name == "PART_BorderElement");
        box.TryFindResource("ErrorTextBrush", box.ActualThemeVariant, out var error);

        Assert.That(box.IsFocused, Is.True, "the focus style must not hide the mark");
        Assert.That(error, Is.InstanceOf<ISolidColorBrush>());
        Assert.That((border.BorderBrush as ISolidColorBrush)?.Color == ((ISolidColorBrush)error!).Color, Is.EqualTo(isMarked));
    }

    [AvaloniaTest]
    public void EveryControlOfTheSearchPanelHasAnAccessibleName()
    {
        OpenResponseSearch("token");

        var unnamed = AccessibleNameChecker.FindUnnamed(_window);

        Assert.That(unnamed, Is.Empty);
    }

    private ResponseSearchViewModel ResponseSearch => _viewModel.TabStrip.SelectedTab!.Response.Search;

    private static ResponseSnapshot CreateTextResponse(string body)
    {
        KeyValueEntry[] headers = [new KeyValueEntry("Content-Type", "text/plain", true)];
        return new ResponseSnapshot(200, "OK", headers, body, false, body.Length, null, new ResponseTiming(1, 2, 3));
    }

    private void OpenWindowWithResponse(string body)
    {
        OpenWindow();
        _viewModel.TabStrip.SelectedTab!.Response.Show(CreateTextResponse(body));
        Dispatcher.UIThread.RunJobs();
    }

    // Opens the search from the body and types the search text.
    private void OpenResponseSearch(string query, string body = ResponseText)
    {
        OpenWindowWithResponse(body);
        FocusResponseBody();
        PressCtrlF();
        _window.KeyTextInput(query);
        Dispatcher.UIThread.RunJobs();
    }

    private CodeEditor FindResponseBody()
    {
        return _window.GetVisualDescendants().OfType<CodeEditor>()
            .First(editor => AutomationProperties.GetName(editor) == "Response body");
    }

    private CodeEditor FocusResponseBody()
    {
        var editor = FindResponseBody();
        editor.TextArea.Focus();
        Dispatcher.UIThread.RunJobs();
        return editor;
    }

    private ResponseSearchView FindSearchPanel()
    {
        return _window.GetVisualDescendants().OfType<ResponseSearchView>().Single();
    }

    private TextBox FindQueryBox()
    {
        return FindSearchPanel().GetVisualDescendants().OfType<TextBox>().Single();
    }

    private TextBlock FindCounter(string text)
    {
        return FindSearchPanel().GetVisualDescendants().OfType<TextBlock>().Single(block => block.Text == text);
    }

    private void Press(PhysicalKey key, RawInputModifiers modifiers)
    {
        _window.KeyPressQwerty(key, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

    private void SelectTab(int index)
    {
        _viewModel.TabStrip.SelectedTab = _viewModel.TabStrip.Tabs[index];
        Dispatcher.UIThread.RunJobs();
    }
}
