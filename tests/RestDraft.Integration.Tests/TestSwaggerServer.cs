using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RestDraft.Integration.Tests;

// A service with a Swagger UI page as Swashbuckle writes it. Everything on it wants the X-Token header.
public sealed class TestSwaggerServer : IAsyncDisposable
{
    public const string TokenHeader = "X-Token";
    public const string Token = "open-sesame";

    private const string Page = """
        <!DOCTYPE html>
        <html>
          <body>
            <div id="swagger-ui"></div>
            <script src="./swagger-ui-bundle.js"></script>
            <script src="index.js"></script>
          </body>
        </html>
        """;

    private const string Script = """
        window.onload = () => {
            var configObject = JSON.parse('{"urls":[{"url":"v1/swagger.json","name":"Shop V1"}]}');
            SwaggerUIBundle(configObject);
        }
        """;

    private const string Specification = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Shop", "version": "v1" },
          "paths": { "/items": { "get": { "summary": "List items" } } }
        }
        """;

    private readonly WebApplication _app;

    public TestSwaggerServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            if (context.Request.Headers[TokenHeader] != Token)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next(context);
        });
        _app.MapGet("/swagger", () => Results.Redirect("swagger/index.html"));
        _app.MapGet("/swagger/index.html", () => Results.Text(Page, "text/html"));
        _app.MapGet("/swagger/index.js", () => Results.Text(Script, "text/javascript"));
        _app.MapGet("/swagger/v1/swagger.json", () => Results.Text(Specification, "application/json"));
    }

    public Uri BaseAddress { get; private set; } = new("http://127.0.0.1");

    public async Task StartAsync()
    {
        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        BaseAddress = new Uri(addresses!.Addresses.First());
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
