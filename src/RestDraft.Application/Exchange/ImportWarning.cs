namespace RestDraft.Application.Exchange;

// Something in the file that RestDraft could not keep. Path names the item, such as "Ollama / Prompt".
public sealed record ImportWarning(string Message, string? Path = null);
