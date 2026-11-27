using NUnit.Framework;
using RestDraft.Domain.Collections;
using RestDraft.Domain.Responses;
using RestDraft.UI.ViewModels;

namespace RestDraft.UI.Tests.ViewModels;

[TestFixture]
public class ResponseSearchViewModelTests
{
    // "ab" is at 0, 3, 6 and 9, the third one in upper case and the last one inside a longer word.
    private const string Text = "ab ab AB abc";

    [TestCase(0, "1 of 4")]
    [TestCase(1, "2 of 4")]
    [TestCase(3, "2 of 4")]
    [TestCase(9, "4 of 4")]
    [TestCase(10, "1 of 4")]
    public void CurrentMatchIsTheFirstOneAtOrAfterTheCursor(int cursorOffset, string expectedCounter)
    {
        var search = CreateSearch(Text);
        search.Query = "ab";

        search.Open(string.Empty, cursorOffset);

        Assert.That(search.CounterText, Is.EqualTo(expectedCounter));
    }

    [TestCase(1, "2 of 4")]
    [TestCase(3, "4 of 4")]
    [TestCase(4, "1 of 4")]
    [TestCase(-1, "4 of 4")]
    [TestCase(-4, "1 of 4")]
    public void NextAndPreviousWrapAround(int steps, string expectedCounter)
    {
        var search = OpenSearch("ab");

        for (var step = 0; step < Math.Abs(steps); step++)
        {
            var command = steps > 0 ? search.NextCommand : search.PreviousCommand;
            command.Execute(null);
        }

        Assert.That(search.CounterText, Is.EqualTo(expectedCounter));
    }

    [TestCase(false, false, "1 of 4")]
    [TestCase(true, false, "1 of 3")]
    [TestCase(false, true, "1 of 3")]
    [TestCase(true, true, "1 of 2")]
    public void OptionsSearchAgain(bool matchCase, bool wholeWord, string expectedCounter)
    {
        var search = OpenSearch("ab");

        search.MatchCase = matchCase;
        search.WholeWord = wholeWord;

        Assert.That(search.CounterText, Is.EqualTo(expectedCounter));
    }

    [Test]
    public void OptionsAreOffAtFirst()
    {
        var search = new ResponseSearchViewModel();

        Assert.That(search.MatchCase, Is.False);
        Assert.That(search.WholeWord, Is.False);
        Assert.That(search.IsOpen, Is.False);
    }

    [Test]
    public void ALongerSearchTextStaysAtTheMatchThatWasCurrent()
    {
        var search = OpenSearch("ab");
        search.SelectNext();
        search.SelectNext();
        search.SelectNext();

        search.Query = "abc";

        Assert.That(search.CurrentMatch?.Start, Is.EqualTo(9));
        search.Query = "ab";
        Assert.That(search.CurrentMatch?.Start, Is.EqualTo(9));
    }

    [Test]
    public void ATextWithoutAMatchKeepsThePlaceForTheNextOne()
    {
        var search = OpenSearch("ab");
        search.SelectNext();

        search.Query = "abx";
        search.Query = "ab";

        Assert.That(search.CurrentMatch?.Start, Is.EqualTo(3));
    }

    [TestCase("x", "0 of 0", true)]
    [TestCase("", "", false)]
    [TestCase("ab", "1 of 4", false)]
    public void NoMatchIsShownAndMarked(string query, string expectedCounter, bool expectedNoMatch)
    {
        var search = OpenSearch(query);

        Assert.That(search.CounterText, Is.EqualTo(expectedCounter));
        Assert.That(search.HasNoMatch, Is.EqualTo(expectedNoMatch));
        Assert.That(search.NextCommand.CanExecute(null), Is.EqualTo(search.Matches.Count > 0));
        Assert.That(search.PreviousCommand.CanExecute(null), Is.EqualTo(search.Matches.Count > 0));
    }

    [TestCase("abc", "abc")]
    [TestCase("", "last")]
    [TestCase("ab\nab", "last")]
    [TestCase("ab\r\n", "last")]
    public void OnlyASelectionOfOneLineBecomesTheSearchText(string selection, string expected)
    {
        var search = OpenSearch("last");
        search.Close();

        search.Open(selection, 0);

        Assert.That(search.Query, Is.EqualTo(expected));
    }

    [Test]
    public void NewTextIsSearchedFromItsStart()
    {
        var search = OpenSearch("ab");
        search.SelectNext();

        search.SetText("ab ab", false);

        Assert.That(search.CounterText, Is.EqualTo("1 of 2"));
    }

    [Test]
    public void TheSameTextKeepsTheCurrentMatch()
    {
        var search = OpenSearch("ab");
        search.SelectNext();

        search.SetText(Text, false);

        Assert.That(search.CounterText, Is.EqualTo("2 of 4"));
    }

    [TestCase(true, "Only the preview is searched.")]
    [TestCase(false, "")]
    public void ATruncatedPreviewIsStated(bool isTruncated, string expected)
    {
        var search = new ResponseSearchViewModel();

        search.SetText(Text, isTruncated);

        Assert.That(search.TruncationNote, Is.EqualTo(expected));
    }

    [Test]
    public void ClosingRemovesTheMatchesAndReportsTheCurrentOne()
    {
        var search = OpenSearch("ab");
        search.SelectNext();
        SearchClosedEventArgs? closed = null;
        search.Closed += (_, e) => closed = e;

        search.CloseCommand.Execute(null);

        Assert.That(search.IsOpen, Is.False);
        Assert.That(search.Matches, Is.Empty);
        Assert.That(closed?.Match?.Start, Is.EqualTo(3));
        Assert.That(search.Query, Is.EqualTo("ab"), "kept for the next time");
    }

    [Test]
    public void NothingIsSearchedWhileThePanelIsClosed()
    {
        var search = CreateSearch(Text);

        search.Query = "ab";

        Assert.That(search.Matches, Is.Empty);
    }

    [Test]
    public void ResultsChangedIsRaisedWhenTheCurrentMatchMoves()
    {
        var search = OpenSearch("ab");
        var raised = 0;
        search.ResultsChanged += (_, _) => raised++;

        search.SelectNext();

        Assert.That(raised, Is.EqualTo(1));
    }

    [Test]
    public void ResponseSearchesTheShownTextAndSearchesAgainForPrettyAndRaw()
    {
        var response = new ResponseViewModel(RequestTabServicesFactory.CreateFormatting());
        response.Search.Open(string.Empty, 0);
        response.Search.Query = " ";

        response.Show(CreateSnapshot("{\"a\":1}", false));
        var pretty = response.Search.Matches.Count;
        response.IsRawView = true;
        var raw = response.Search.Matches.Count;
        response.IsPrettyView = true;

        Assert.That(pretty, Is.GreaterThan(0));
        Assert.That(raw, Is.Zero);
        Assert.That(response.Search.Matches, Has.Count.EqualTo(pretty));
    }

    [Test]
    public void ResponseSearchesEachNewResponse()
    {
        var response = new ResponseViewModel(RequestTabServicesFactory.CreateFormatting());
        response.Search.Open(string.Empty, 0);
        response.Search.Query = "token";
        response.Show(CreateSnapshot("token", true));
        Assert.That(response.Search.CounterText, Is.EqualTo("1 of 1"));
        Assert.That(response.Search.TruncationNote, Is.Not.Empty);

        response.Show(CreateSnapshot("token token", false));

        Assert.That(response.Search.CounterText, Is.EqualTo("1 of 2"));
        Assert.That(response.Search.TruncationNote, Is.Empty);
        response.Clear();
        Assert.That(response.Search.CounterText, Is.EqualTo("0 of 0"));
    }

    private static ResponseSearchViewModel CreateSearch(string text)
    {
        var search = new ResponseSearchViewModel();
        search.SetText(text, false);
        return search;
    }

    private static ResponseSearchViewModel OpenSearch(string query)
    {
        var search = CreateSearch(Text);
        search.Open(string.Empty, 0);
        search.Query = query;
        return search;
    }

    private static ResponseSnapshot CreateSnapshot(string body, bool isTruncated)
    {
        KeyValueEntry[] headers = [new KeyValueEntry("Content-Type", "application/json", true)];
        return new ResponseSnapshot(200, "OK", headers, body, isTruncated, body.Length, null, new ResponseTiming(1, 2, 3));
    }
}
