namespace RestDraft.Application.Tests.Exchange.Remote;

// Pages and scripts as the common tools write them, shortened.
internal static class SwaggerPageSamples
{
    public const string SwaggerUiPage = """
        <!DOCTYPE html>
        <html lang="en">
          <head>
            <meta charset="UTF-8">
            <title>Swagger UI</title>
            <link rel="stylesheet" type="text/css" href="./swagger-ui.css" />
          </head>
          <body>
            <div id="swagger-ui"></div>
            <script src="./swagger-ui-bundle.js" charset="UTF-8"> </script>
            <script src="./swagger-ui-standalone-preset.js" charset="UTF-8"> </script>
            <script src="./swagger-initializer.js" charset="UTF-8"> </script>
          </body>
        </html>
        """;

    public const string SwaggerUiInitializer = """
        window.onload = function() {
          // the following lines will be replaced by docker/configurator, when it runs in a docker-container
          // url: "https://commented.example/openapi.json",
          window.ui = SwaggerUIBundle({
            url: "https://petstore.swagger.io/v2/swagger.json",
            dom_id: '#swagger-ui',
            deepLinking: true,
            presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
            layout: "StandaloneLayout"
          });
        };
        """;

    // The public sites of Swagger, such as petstore.swagger.io, as served in November 2026.
    public const string SwaggerSitesInitializer = """
        window.onload = function() {
            const defaultDefinitionUrl = "https://petstore.swagger.io/v2/swagger.json";
            const ossServices = `petstore3.swagger.io=https://petstore3.swagger.io/api/v3/openapi.json,petstore.swagger.io=https://petstore.swagger.io/v2/swagger.json,generator.swagger.io=https://generator.swagger.io/api/swagger.json`;
            const ossServicesTuples = ossServices.split(',').map(ossService => ossService.split('='))
            const ossServiceMatch = ossServicesTuples.find(([host]) => window.location.host.includes(host))
            const definitionURL = ossServiceMatch ? ossServiceMatch[1] : defaultDefinitionUrl;
              window.ui = SwaggerUIBundle({
                url: definitionURL,
                "dom_id": "#swagger-ui",
                layout: "StandaloneLayout",
                queryConfigEnabled: true,
                validatorUrl: "https://validator.swagger.io/validator",
              })
        };
        """;

    public const string SwashbuckleScript = """
        window.onload = () => {
            var configObject = JSON.parse('{"urls":[{"url":"v1/swagger.json","name":"My API V1"},{"name":"Ann\u0027s API","url":"/swagger/v2/swagger.json?a=1\u0026b=2"}],"deepLinking":false,"validatorUrl":null}');
            var oauthConfigObject = JSON.parse('{}');
            configObject.urls.forEach(function (item) {
                if (item.url.startsWith("http") || item.url.startsWith("/")) return;
                item.url = window.location.href.replace("index.html", item.url).split('#')[0];
            });
            configObject.dom_id = "#swagger-ui";
            const ui = SwaggerUIBundle(configObject);
        }
        """;

    public const string ExpressScript = """
        window.onload = function() {
          var url = window.location.search.match(/url=([^&]+)/);
          if (url && url.length > 1) {
            url = decodeURIComponent(url[1]);
          } else {
            url = window.location.origin;
          }
          var options = {
          "swaggerDoc": {
            "openapi": "3.0.0",
            "info": { "title": "Braces } in a text", "version": "1" },
            "servers": [{ "url": "http://localhost:3000" }],
            "paths": {}
          },
          "customOptions": {}
        };
          url = options.swaggerUrl || url
          var urls = options.swaggerUrls
          var spec1 = options.swaggerDoc
          var swaggerOptions = { spec: spec1, url: url, urls: urls, dom_id: '#swagger-ui' }
        }
        """;

    public const string ExpressDocument = """
        {
            "openapi": "3.0.0",
            "info": { "title": "Braces } in a text", "version": "1" },
            "servers": [{ "url": "http://localhost:3000" }],
            "paths": {}
          }
        """;

    public const string ExpressScriptWithAnAddress = """
        var options = {
          "swaggerUrl": "/api-docs.json",
          "customOptions": {}
        };
        """;

    public const string SpringdocInitializer = """
        window.onload = function() {
          window.ui = SwaggerUIBundle({
            url: "https://petstore.swagger.io/v2/swagger.json",
            dom_id: '#swagger-ui',
            layout: "StandaloneLayout" ,
            "configUrl" : "/v3/api-docs/swagger-config",
            "validatorUrl" : ""
          });
        };
        """;

    public const string SpringdocConfiguration = """
        {"configUrl":"/v3/api-docs/swagger-config","oauth2RedirectUrl":"http://localhost:8080/swagger-ui/oauth2-redirect.html",
        "urls":[{"url":"/v3/api-docs/orders","name":"orders"},{"url":"/v3/api-docs/users","name":"users"}],"validatorUrl":""}
        """;

    public const string FastApiPage = """
        <!DOCTYPE html>
        <html>
        <head>
        <link type="text/css" rel="stylesheet" href="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui.css">
        </head>
        <body>
        <div id="swagger-ui"></div>
        <script src="https://cdn.jsdelivr.net/npm/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
        <script>
        const ui = SwaggerUIBundle({
            url: '/openapi.json',
        "dom_id": "#swagger-ui",
        oauth2RedirectUrl: window.location.origin + '/docs/oauth2-redirect',
            })
        </script>
        </body>
        </html>
        """;

    public const string RedocPage = """
        <!DOCTYPE html>
        <html>
          <body>
            <!-- <redoc spec-url="/old.json"></redoc> -->
            <redoc spec-url='/openapi.json?a=1&amp;b=2'></redoc>
            <script src="https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js"></script>
          </body>
        </html>
        """;
}
