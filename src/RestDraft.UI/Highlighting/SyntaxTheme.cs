using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.Highlighting;
using RestDraft.UI.Services;

namespace RestDraft.UI.Highlighting;

// The highlighting of JSON and XML in the colors of the light or the dark theme. A definition is built when it is first
// used and then shared by all editors.
public sealed class SyntaxTheme
{
    private readonly Lazy<IHighlightingDefinition> _json;
    private readonly Lazy<IHighlightingDefinition> _xml;

    private SyntaxTheme(IReadOnlyDictionary<string, Color> colors)
    {
        _json = new Lazy<IHighlightingDefinition>(() => SyntaxDefinitionLoader.Load("Json", colors));
        _xml = new Lazy<IHighlightingDefinition>(() => SyntaxDefinitionLoader.Load("Xml", colors));
    }

    public static SyntaxTheme Light { get; } = new(new Dictionary<string, Color>
    {
        ["Key"] = Color.Parse("#0451A5"),
        ["String"] = Color.Parse("#A31515"),
        ["Number"] = Color.Parse("#098658"),
        ["Constant"] = Color.Parse("#0000FF"),
        ["Comment"] = Color.Parse("#008000"),
        ["Delimiter"] = Color.Parse("#800000"),
        ["TagName"] = Color.Parse("#800000"),
        ["AttributeName"] = Color.Parse("#E50000"),
        ["AttributeValue"] = Color.Parse("#0000FF"),
        ["CData"] = Color.Parse("#A31515"),
        ["Entity"] = Color.Parse("#0451A5"),
        ["BrokenEntity"] = Color.Parse("#CD3131"),
    });

    public static SyntaxTheme Dark { get; } = new(new Dictionary<string, Color>
    {
        ["Key"] = Color.Parse("#9CDCFE"),
        ["String"] = Color.Parse("#CE9178"),
        ["Number"] = Color.Parse("#B5CEA8"),
        ["Constant"] = Color.Parse("#569CD6"),
        ["Comment"] = Color.Parse("#6A9955"),
        ["Delimiter"] = Color.Parse("#808080"),
        ["TagName"] = Color.Parse("#569CD6"),
        ["AttributeName"] = Color.Parse("#9CDCFE"),
        ["AttributeValue"] = Color.Parse("#CE9178"),
        ["CData"] = Color.Parse("#CE9178"),
        ["Entity"] = Color.Parse("#569CD6"),
        ["BrokenEntity"] = Color.Parse("#F44747"),
    });

    public static SyntaxTheme For(ThemeVariant variant)
    {
        return variant == ThemeVariant.Dark ? Dark : Light;
    }

    // Nothing for plain text, which is shown without highlighting.
    public IHighlightingDefinition? DefinitionFor(EditorLanguage language)
    {
        return language switch
        {
            EditorLanguage.Json => _json.Value,
            EditorLanguage.Xml => _xml.Value,
            _ => null,
        };
    }
}
