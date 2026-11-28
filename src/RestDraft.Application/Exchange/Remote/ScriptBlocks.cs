namespace RestDraft.Application.Exchange.Remote;

// Blocks taken out of a script, and the script without them.
internal sealed record ScriptBlocks(IReadOnlyList<string> Blocks, string Rest);
