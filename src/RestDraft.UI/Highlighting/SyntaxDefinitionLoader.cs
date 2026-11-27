using System.Xml;
using Avalonia.Media;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace RestDraft.UI.Highlighting;

// Reads one of the highlighting definitions that are part of the program and gives its named colors their values.
public static class SyntaxDefinitionLoader
{
    public static IHighlightingDefinition Load(string name, IReadOnlyDictionary<string, Color> colors)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://RestDraft/Highlighting/{name}.xshd"));
        using var reader = XmlReader.Create(stream);
        var syntax = HighlightingLoader.LoadXshd(reader);
        foreach (var color in syntax.Elements.OfType<XshdColor>())
        {
            // A color without a value leaves the text in the color of the editor.
            if (color.Name is not null && colors.TryGetValue(color.Name, out var value))
            {
                color.Foreground = new SimpleHighlightingBrush(value);
            }
        }

        return HighlightingLoader.Load(syntax, HighlightingManager.Instance);
    }
}
