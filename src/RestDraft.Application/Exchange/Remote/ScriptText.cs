using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RestDraft.Application.Exchange.Remote;

// Just enough of JavaScript to take links out of a script: comment lines, brackets and string literals. It does not
// parse the script. The text comes from a server, so every method goes through it once.
internal static partial class ScriptText
{
    public static string WithoutCommentLines(string script)
    {
        return CommentLine().Replace(script, string.Empty);
    }

    // The blocks that begin where the opener matches. The match has to end with the bracket of the block.
    public static ScriptBlocks TakeBlocks(string script, Regex opener)
    {
        var blocks = new List<string>();
        var rest = new StringBuilder();
        var copied = 0;
        foreach (Match match in opener.Matches(script))
        {
            if (match.Index < copied)
            {
                continue;
            }

            var start = match.Index + match.Length - 1;
            var end = BlockEnd(script, start);
            blocks.Add(script[start..end]);
            rest.Append(script, copied, match.Index - copied);
            copied = end;
        }

        rest.Append(script, copied, script.Length - copied);
        return new ScriptBlocks(blocks, rest.ToString());
    }

    // What a string literal stands for. Links only ever use \/, \uXXXX and escaped quotes.
    public static string Unescape(string literal)
    {
        return Escape().Replace(literal, match => match.Groups["code"].Success
            ? ((char)int.Parse(match.Groups["code"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString()
            : match.Groups["character"].Value);
    }

    // The index after the bracket that closes the one at start, or the end of the text when none does.
    private static int BlockEnd(string script, int start)
    {
        var depth = 0;
        for (var index = start; index < script.Length; index++)
        {
            switch (script[index])
            {
                case '"' or '\'' or '`':
                    index = StringEnd(script, index);
                    break;
                case '{' or '[':
                    depth++;
                    break;
                case '}' or ']':
                    depth--;
                    if (depth == 0)
                    {
                        return index + 1;
                    }

                    break;
            }
        }

        return script.Length;
    }

    // The index of the quote that closes the string. A quoted string cannot go past its line.
    private static int StringEnd(string script, int start)
    {
        var quote = script[start];
        for (var index = start + 1; index < script.Length; index++)
        {
            if (script[index] == '\\')
            {
                index++;
                continue;
            }

            if (script[index] == quote || (script[index] == '\n' && quote != '`'))
            {
                return index;
            }
        }

        return script.Length;
    }

    [GeneratedRegex(@"^[ \t]*//[^\n]*", RegexOptions.Multiline)]
    private static partial Regex CommentLine();

    [GeneratedRegex(@"\\(?:u(?<code>[0-9a-fA-F]{4})|(?<character>.))")]
    private static partial Regex Escape();
}
