using NUnit.Framework;
using RestDraft.Application.Exchange;
using RestDraft.UI.Services;

namespace RestDraft.UI.Tests.Services;

[TestFixture]
public class ExchangeMessagesTests
{
    [TestCase(9, 3, 2, "Imported 9 new and 3 overwritten requests into 'Scratch Pad'; 2 warnings.")]
    [TestCase(1, 0, 1, "Imported 1 new request into 'Scratch Pad'; 1 warning.")]
    [TestCase(4, 0, 0, "Imported 4 new requests into 'Scratch Pad'.")]
    public void ImportMessageSaysWhatWasDone(int added, int overwritten, int warnings, string expected)
    {
        var outcome = new ImportOutcome(Guid.NewGuid(), "Scratch Pad", new ImportCounts(1, added, overwritten, 0), [], warnings);

        Assert.That(ExchangeMessages.Imported(outcome), Is.EqualTo(expected));
    }

    [TestCase(14, "Exported 14 requests to C:\\out.yaml.")]
    [TestCase(1, "Exported 1 request to C:\\out.yaml.")]
    public void ExportMessageNamesTheFile(int requests, string expected)
    {
        Assert.That(ExchangeMessages.Exported(new ExportOutcome("C:\\out.yaml", 2, requests)), Is.EqualTo(expected));
    }

    [TestCase("Scratch Pad", "Scratch Pad")]
    [TestCase("a/b/c", "a_b_c")]
    [TestCase("   ", "collection")]
    public void SuggestedFileNameHasNoInvalidCharacters(string name, string expected)
    {
        Assert.That(ExchangeMessages.SuggestedFileName(name), Is.EqualTo(expected));
    }
}
