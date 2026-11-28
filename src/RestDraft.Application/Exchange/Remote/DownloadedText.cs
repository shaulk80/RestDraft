namespace RestDraft.Application.Exchange.Remote;

// What a download gave. Address is the one the text came from, which a redirect may have changed.
public sealed record DownloadedText(Uri Address, string Text);
