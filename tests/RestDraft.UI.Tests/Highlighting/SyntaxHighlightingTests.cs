using Avalonia.Headless.NUnit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using NUnit.Framework;
using RestDraft.UI.Highlighting;
using RestDraft.UI.Services;

namespace RestDraft.UI.Tests.Highlighting;

[TestFixture]
public class SyntaxHighlightingTests
{
    private const string? NotColored = null;

    // In each case, every character of the first occurrence of the fragment must have the expected color.
    [TestCase("""{"a": 1}""", "\"a\"", "Key")]
    [TestCase("""{"a"   : 1}""", "\"a\"", "Key")]
    [TestCase("""{"a":"b","c":"d"}""", "\"b\"", "String")]
    [TestCase("""{"a":"b","c":"d"}""", "\"c\"", "Key")]
    [TestCase("""{"a":"b","c":"d"}""", "\"d\"", "String")]
    [TestCase("""{"a":{"b":[{"c":1}]},"d":2}""", "\"d\"", "Key")]
    [TestCase("""["x","y"]""", "\"y\"", "String")]
    [TestCase("""{"a\"b": 1}""", """
        "a\"b"
        """, "Key")]
    [TestCase("""{"a": "say \"hi\": no"}""", """
        "say \"hi\": no"
        """, "String")]
    [TestCase("""{"a": "back\\", "b": 1}""", "\"b\"", "Key")]
    [TestCase("""{"url": "https://host/a?b=1"}""", "?b=1", "String")]
    [TestCase("""{"a": "true 12 null"}""", "true 12 null", "String")]
    [TestCase("""{"a": "cut off""", "\"cut off", "String")]
    [TestCase("""{"a": true}""", "true", "Constant")]
    [TestCase("""{"a": false}""", "false", "Constant")]
    [TestCase("""{"a": null}""", "null", "Constant")]
    [TestCase("""{"a": 12345}""", "12345", "Number")]
    [TestCase("""{"a": -1.50e+3}""", "-1.50e+3", "Number")]
    [TestCase("""[1,2]""", "2", "Number")]
    [TestCase("""{"a": 1}""", "{", NotColored)]
    [TestCase("""{"a": 1}""", ":", NotColored)]
    [TestCase("""{"a": nullable}""", "nullable", NotColored)]
    [AvaloniaTest]
    public void JsonTokensAreColored(string text, string fragment, string? expected)
    {
        AssertColor(EditorLanguage.Json, text, fragment, expected);
    }

    [TestCase("""<book id="1">t</book>""", "<", "Delimiter")]
    [TestCase("""<book id="1">t</book>""", "book", "TagName")]
    [TestCase("""<book id="1">t</book>""", "id", "AttributeName")]
    [TestCase("""<book id="1">t</book>""", "\"1\"", "AttributeValue")]
    [TestCase("""<book id="1">t</book>""", "</", "Delimiter")]
    [TestCase("""<book id="1">text</book>""", "text", NotColored)]
    [TestCase("""<ns:a xmlns:ns='urn:x'/>""", "ns:a", "TagName")]
    [TestCase("""<ns:a xmlns:ns='urn:x'/>""", "xmlns:ns", "AttributeName")]
    [TestCase("""<ns:a xmlns:ns='urn:x'/>""", "'urn:x'", "AttributeValue")]
    [TestCase("""<ns:a xmlns:ns='urn:x'/>""", "/>", "Delimiter")]
    [TestCase("""<a b = "c">""", "b", "AttributeName")]
    [TestCase("""<a b="x > y">after</a>""", "> y", "AttributeValue")]
    [TestCase("""<a b="x > y">after</a>""", "after", NotColored)]
    [TestCase("""<?xml version="1.0"?>""", "xml", "TagName")]
    [TestCase("""<?xml version="1.0"?>""", "version", "AttributeName")]
    [TestCase("""<?xml version="1.0"?>""", "?>", "Delimiter")]
    [TestCase("""<!DOCTYPE note SYSTEM "note.dtd">""", "DOCTYPE", "TagName")]
    [TestCase("""<!DOCTYPE note SYSTEM "note.dtd">""", "\"note.dtd\"", "AttributeValue")]
    [TestCase("""<!-- <a b="c"> -->""", """<!-- <a b="c"> -->""", "Comment")]
    [TestCase("""<![CDATA[<raw> & text]]>""", "<raw> & text", "CData")]
    [TestCase("""<![CDATA[x]]>""", "<![CDATA[", "Delimiter")]
    [TestCase("""<![CDATA[x]]>""", "]]>", "Delimiter")]
    [TestCase("""<a>x &amp; y</a>""", "&amp;", "Entity")]
    [TestCase("""<a>&#x41;</a>""", "&#x41;", "Entity")]
    [TestCase("""<a b="&lt;"/>""", "&lt;", "Entity")]
    [TestCase("""<a>x & y</a>""", "&", "BrokenEntity")]
    [TestCase("<!-- one\ntwo <a> -->\n<b/>", "two <a>", "Comment")]
    [TestCase("<!-- one\ntwo -->\n<b/>", "b", "TagName")]
    [TestCase("<a\n  line=\"1\">", "line", "AttributeName")]
    [TestCase("<a\nline=\"1\">", "line", "AttributeName")]
    [TestCase("<a v=\"one\ntwo\">", "two\"", "AttributeValue")]
    [TestCase("<a x=\"1\" disabled>", "disabled", NotColored)]
    [TestCase("""<root><child/></root>""", "child", "TagName")]
    [TestCase("""<a b="1"></item>""", "item", "TagName")]
    [AvaloniaTest]
    public void XmlTokensAreColored(string text, string fragment, string? expected)
    {
        AssertColor(EditorLanguage.Xml, text, fragment, expected);
    }

    [TestCase(EditorLanguage.Json, false)]
    [TestCase(EditorLanguage.Json, true)]
    [TestCase(EditorLanguage.Xml, false)]
    [TestCase(EditorLanguage.Xml, true)]
    [AvaloniaTest]
    public void EveryNamedColorHasAValueInBothThemes(EditorLanguage language, bool dark)
    {
        var theme = dark ? SyntaxTheme.Dark : SyntaxTheme.Light;

        var definition = theme.DefinitionFor(language);

        Assert.That(definition, Is.Not.Null);
        Assert.That(definition!.NamedHighlightingColors, Is.Not.Empty);
        Assert.That(
            definition.NamedHighlightingColors.Where(color => color.Foreground is null).Select(color => color.Name),
            Is.Empty);
    }

    [AvaloniaTest]
    public void PlainTextHasNoDefinition()
    {
        Assert.That(SyntaxTheme.Light.DefinitionFor(EditorLanguage.PlainText), Is.Null);
    }

    private static void AssertColor(EditorLanguage language, string text, string fragment, string? expected)
    {
        var start = text.IndexOf(fragment, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "The fragment is not in the text.");
        var document = new TextDocument(text);
        using var highlighter = new DocumentHighlighter(document, SyntaxTheme.Light.DefinitionFor(language)!);

        var colors = Enumerable.Range(start, fragment.Length)
            .Where(offset => !char.IsWhiteSpace(text[offset]))
            .Select(offset => ColorNameAt(document, highlighter, offset))
            .Distinct()
            .ToList();

        string?[] expectedColors = [expected];
        Assert.That(colors, Is.EqualTo(expectedColors));
    }

    // Sections nest, and a later one lies inside an earlier one, so the last one that covers the offset is shown.
    private static string? ColorNameAt(TextDocument document, DocumentHighlighter highlighter, int offset)
    {
        var line = highlighter.HighlightLine(document.GetLineByOffset(offset).LineNumber);
        return line.Sections
            .LastOrDefault(section => section.Offset <= offset && offset < section.Offset + section.Length && section.Color.Foreground is not null)?
            .Color.Name;
    }
}
