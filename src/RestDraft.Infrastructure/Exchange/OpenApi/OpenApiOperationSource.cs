using Microsoft.OpenApi;

namespace RestDraft.Infrastructure.Exchange.OpenApi;

// One operation of the specification and where it is. Label, such as "GET /pet/{petId}", names it in warnings.
internal sealed record OpenApiOperationSource(string Path, IOpenApiPathItem PathItem, OpenApiOperation Operation, string Label);
