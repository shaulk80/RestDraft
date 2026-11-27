namespace RestDraft.Infrastructure.Exchange.OpenApi;

// The kinds of request body, the preferred one first.
internal enum OpenApiBodyKind
{
    Json,
    FormUrlEncoded,
    Multipart,
    Text,
    Unsupported
}
