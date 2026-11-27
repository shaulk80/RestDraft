using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;

namespace RestDraft.UI.Highlighting;

// The colorizer of the editor, with the limit on the colored part of a line.
public sealed class LimitedLineColorizer(IHighlightingDefinition definition) : HighlightingColorizer(definition)
{
    protected override IHighlighter CreateHighlighter(TextView textView, TextDocument document)
    {
        return new LimitedLineHighlighter(base.CreateHighlighter(textView, document));
    }
}
