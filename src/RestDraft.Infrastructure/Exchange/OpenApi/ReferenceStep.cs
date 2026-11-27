using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// One reference and what it leads to, which may be another reference.
internal sealed record ReferenceStep(BaseOpenApiReference Reference, object? Target);
