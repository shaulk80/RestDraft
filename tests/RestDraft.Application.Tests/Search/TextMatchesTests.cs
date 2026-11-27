using NUnit.Framework;
using RestDraft.Application.Search;

namespace RestDraft.Application.Tests.Search;

[TestFixture]
public class TextMatchesTests
{
    // Matches of two characters at 2, 6 and 10.
    private static readonly TextMatch[] Matches = [new TextMatch(2, 2), new TextMatch(6, 2), new TextMatch(10, 2)];

    [TestCase(0, 0)]
    [TestCase(2, 0)]
    [TestCase(3, 1)]
    [TestCase(10, 2)]
    [TestCase(11, 3)]
    public void FirstIndexFromGivesTheFirstMatchAtOrAfterTheOffset(int offset, int expected)
    {
        Assert.That(TextMatches.FirstIndexFrom(Matches, offset), Is.EqualTo(expected));
    }

    [TestCase(0, 100, new[] { 2, 6, 10 })]
    [TestCase(3, 7, new[] { 2, 6 })]
    [TestCase(4, 6, new int[0])]
    [TestCase(7, 10, new[] { 6 })]
    [TestCase(12, 100, new int[0])]
    [TestCase(0, 2, new int[0])]
    public void OverlappingGivesTheMatchesThatReachIntoTheRange(int start, int end, int[] expected)
    {
        var matches = TextMatches.Overlapping(Matches, start, end);

        Assert.That(matches.Select(match => match.Start), Is.EqualTo(expected));
    }

    [Test]
    public void NoMatchesGiveNothing()
    {
        Assert.That(TextMatches.FirstIndexFrom([], 5), Is.Zero);
        Assert.That(TextMatches.Overlapping([], 0, 5), Is.Empty);
    }
}
