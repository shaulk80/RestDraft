using RestDraft.Application.Exchange;

namespace RestDraft.UI.Services;

// What the dialog of an import from an address ends with. TypedAddress is what the user typed, and is remembered;
// DownloadAddress is where the specification itself came from.
public sealed record ImportUrlResult(ExchangeDocument Document, Uri DownloadAddress, Uri TypedAddress);
