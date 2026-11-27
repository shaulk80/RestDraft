using NUnit.Framework;
using RestDraft.Application.Search;

namespace RestDraft.Application.Tests.Search;

[TestFixture]
public class TextMatchFinderTests
{
    private static readonly TextSearchOptions IgnoreCase = new(MatchCase: false, WholeWord: false);
    private static readonly TextSearchOptions MatchCase = new(MatchCase: true, WholeWord: false);
    private static readonly TextSearchOptions WholeWord = new(MatchCase: false, WholeWord: true);

    [TestCase("token Token TOKEN", "token", new[] { 0, 6, 12 })]
    [TestCase("token at the end token", "token", new[] { 0, 17 })]
    [TestCase("aaaaa", "aa", new[] { 0, 2 })]
    [TestCase("abababa", "aba", new[] { 0, 4 })]
    [TestCase("one\r\ntwo", "two", new[] { 5 })]
    [TestCase("a b", " ", new[] { 1 })]
    [TestCase("abc", "abcd", new int[0])]
    [TestCase("abc", "x", new int[0])]
    [TestCase("", "x", new int[0])]
    [TestCase("abc", "", new int[0])]
    public void FindsMatchesThatDoNotOverlap(string text, string query, int[] expected)
    {
        var matches = TextMatchFinder.Find(text, query, IgnoreCase);

        Assert.That(matches.Select(match => match.Start), Is.EqualTo(expected));
        Assert.That(matches.Select(match => match.Length), Is.All.EqualTo(query.Length));
    }

    [TestCase("token Token TOKEN", "Token", new[] { 6 })]
    [TestCase("token", "TOKEN", new int[0])]
    public void MatchCaseKeepsTheMatchesWithTheSameLetters(string text, string query, int[] expected)
    {
        var matches = TextMatchFinder.Find(text, query, MatchCase);

        Assert.That(matches.Select(match => match.Start), Is.EqualTo(expected));
    }

    [TestCase("id", "id", new[] { 0 })]
    [TestCase("id userid idx id_2 2id id", "id", new[] { 0, 23 })]
    [TestCase("{\"id\":1,\"id.x\":2}", "id", new[] { 2, 9 })]
    [TestCase("x(id)y", "(id)", new[] { 1 })]
    [TestCase("x\"id\"", "\"id\"", new[] { 1 })]
    [TestCase("a-b-c", "-b-", new[] { 1 })]
    [TestCase("id. xid.", "id.", new[] { 0 })]
    [TestCase(".id .idx", ".id", new[] { 0 })]
    [TestCase("aaa aa", "aa", new[] { 4 })]
    [TestCase("xa a a", "a a", new[] { 3 })]
    [TestCase("éid idé id", "id", new[] { 8 })]
    [TestCase("𝑥id id", "id", new[] { 5 })]
    [TestCase("id𝑥 id", "id", new[] { 5 })]
    [TestCase("😀id😀", "id", new[] { 2 })]
    public void WholeWordLeavesOutMatchesInsideALongerWord(string text, string query, int[] expected)
    {
        var matches = TextMatchFinder.Find(text, query, WholeWord);

        Assert.That(matches.Select(match => match.Start), Is.EqualTo(expected));
    }

    [Test]
    public void BothOptionsApplyTogether()
    {
        var matches = TextMatchFinder.Find("Id id Idle", "Id", new TextSearchOptions(MatchCase: true, WholeWord: true));

        Assert.That(matches, Is.EqualTo(new[] { new TextMatch(0, 2) }));
    }
}
