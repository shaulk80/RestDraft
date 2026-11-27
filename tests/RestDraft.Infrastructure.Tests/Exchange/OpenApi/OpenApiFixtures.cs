namespace RestDraft.Infrastructure.Tests.Exchange.OpenApi;

// One small API, written for Swagger 2.0 and OpenAPI 3.0 and 3.1, each in YAML and in JSON.
internal static class OpenApiFixtures
{
    public const string Swagger2Yaml = """
        swagger: "2.0"
        info:
          title: Pet API
          version: 1.0
        host: api.example.com
        basePath: /v1
        schemes: [https]
        consumes: [application/json]
        securityDefinitions:
          key: { type: apiKey, in: header, name: X-Api-Key }
          login: { type: basic }
        security:
          - key: []
        paths:
          /health:
            get:
              summary: Health
              security: []
              responses:
                200: { description: ok }
          /pets:
            get:
              tags: [pets]
              summary: List pets
              parameters:
                - { name: limit, in: query, type: integer, default: 10 }
                - { name: status, in: query, type: string, required: true }
                - { name: X-Trace, in: header, type: string, default: abc }
              responses:
                200: { description: ok }
            post:
              tags: [pets]
              summary: Add a pet
              security:
                - login: []
              parameters:
                - name: pet
                  in: body
                  schema:
                    $ref: '#/definitions/Pet'
              responses:
                200: { description: ok }
          /pets/{petId}:
            parameters:
              - { name: petId, in: path, required: true, type: integer }
            get:
              tags: [pets]
              operationId: getPet
              responses:
                200: { description: ok }
          /pets/{petId}/photo:
            post:
              tags: [pets]
              summary: Upload a photo
              consumes: [multipart/form-data]
              parameters:
                - { name: petId, in: path, required: true, type: integer }
                - { name: photo, in: formData, type: file }
                - { name: caption, in: formData, type: string, default: A photo }
              responses:
                200: { description: ok }
        definitions:
          Pet:
            type: object
            required: [name]
            properties:
              id: { type: integer, readOnly: true }
              name: { type: string, example: Rex }
              tags: { type: array, items: { type: string } }
              born: { type: string, format: date }
              owner: { $ref: '#/definitions/Owner' }
          Owner:
            type: object
            properties:
              email: { type: string, format: email }
              pets: { type: array, items: { $ref: '#/definitions/Pet' } }
        """;

    public const string Swagger2Json = """
        {
          "swagger": "2.0",
          "info": { "title": "Pet API", "version": "1.0" },
          "host": "api.example.com",
          "basePath": "/v1",
          "schemes": ["https"],
          "consumes": ["application/json"],
          "securityDefinitions": {
            "key": { "type": "apiKey", "in": "header", "name": "X-Api-Key" },
            "login": { "type": "basic" }
          },
          "security": [{ "key": [] }],
          "paths": {
            "/health": {
              "get": { "summary": "Health", "security": [], "responses": { "200": { "description": "ok" } } }
            },
            "/pets": {
              "get": {
                "tags": ["pets"],
                "summary": "List pets",
                "parameters": [
                  { "name": "limit", "in": "query", "type": "integer", "default": 10 },
                  { "name": "status", "in": "query", "type": "string", "required": true },
                  { "name": "X-Trace", "in": "header", "type": "string", "default": "abc" }
                ],
                "responses": { "200": { "description": "ok" } }
              },
              "post": {
                "tags": ["pets"],
                "summary": "Add a pet",
                "security": [{ "login": [] }],
                "parameters": [{ "name": "pet", "in": "body", "schema": { "$ref": "#/definitions/Pet" } }],
                "responses": { "200": { "description": "ok" } }
              }
            },
            "/pets/{petId}": {
              "parameters": [{ "name": "petId", "in": "path", "required": true, "type": "integer" }],
              "get": { "tags": ["pets"], "operationId": "getPet", "responses": { "200": { "description": "ok" } } }
            },
            "/pets/{petId}/photo": {
              "post": {
                "tags": ["pets"],
                "summary": "Upload a photo",
                "consumes": ["multipart/form-data"],
                "parameters": [
                  { "name": "petId", "in": "path", "required": true, "type": "integer" },
                  { "name": "photo", "in": "formData", "type": "file" },
                  { "name": "caption", "in": "formData", "type": "string", "default": "A photo" }
                ],
                "responses": { "200": { "description": "ok" } }
              }
            }
          },
          "definitions": {
            "Pet": {
              "type": "object",
              "required": ["name"],
              "properties": {
                "id": { "type": "integer", "readOnly": true },
                "name": { "type": "string", "example": "Rex" },
                "tags": { "type": "array", "items": { "type": "string" } },
                "born": { "type": "string", "format": "date" },
                "owner": { "$ref": "#/definitions/Owner" }
              }
            },
            "Owner": {
              "type": "object",
              "properties": {
                "email": { "type": "string", "format": "email" },
                "pets": { "type": "array", "items": { "$ref": "#/definitions/Pet" } }
              }
            }
          }
        }
        """;

    public const string OpenApi30Yaml = """
        openapi: 3.0.3
        info:
          title: Pet API
          version: 1.0
        servers:
          - url: https://api.example.com/v1
        security:
          - key: []
        paths:
          /health:
            get:
              summary: Health
              security: []
              responses:
                200: { description: ok }
          /pets:
            get:
              tags: [pets]
              summary: List pets
              parameters:
                - { name: limit, in: query, schema: { type: integer, default: 10 } }
                - { name: status, in: query, required: true, schema: { type: string } }
                - { name: X-Trace, in: header, schema: { type: string, default: abc } }
              responses:
                200: { description: ok }
            post:
              tags: [pets]
              summary: Add a pet
              security:
                - login: []
              requestBody:
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/Pet'
              responses:
                200: { description: ok }
          /pets/{petId}:
            parameters:
              - { name: petId, in: path, required: true, schema: { type: integer } }
            get:
              tags: [pets]
              operationId: getPet
              responses:
                200: { description: ok }
          /pets/{petId}/photo:
            post:
              tags: [pets]
              summary: Upload a photo
              parameters:
                - { name: petId, in: path, required: true, schema: { type: integer } }
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        photo: { type: string, format: binary }
                        caption: { type: string, default: A photo }
              responses:
                200: { description: ok }
        components:
          securitySchemes:
            key: { type: apiKey, in: header, name: X-Api-Key }
            login: { type: http, scheme: basic }
          schemas:
            Pet:
              type: object
              required: [name]
              properties:
                id: { type: integer, readOnly: true }
                name: { type: string, example: Rex }
                tags: { type: array, items: { type: string } }
                born: { type: string, format: date }
                owner: { $ref: '#/components/schemas/Owner' }
            Owner:
              type: object
              properties:
                email: { type: string, format: email }
                pets: { type: array, items: { $ref: '#/components/schemas/Pet' } }
        """;

    public const string OpenApi30Json = """
        {
          "openapi": "3.0.3",
          "info": { "title": "Pet API", "version": "1.0" },
          "servers": [{ "url": "https://api.example.com/v1" }],
          "security": [{ "key": [] }],
          "paths": {
            "/health": {
              "get": { "summary": "Health", "security": [], "responses": { "200": { "description": "ok" } } }
            },
            "/pets": {
              "get": {
                "tags": ["pets"],
                "summary": "List pets",
                "parameters": [
                  { "name": "limit", "in": "query", "schema": { "type": "integer", "default": 10 } },
                  { "name": "status", "in": "query", "required": true, "schema": { "type": "string" } },
                  { "name": "X-Trace", "in": "header", "schema": { "type": "string", "default": "abc" } }
                ],
                "responses": { "200": { "description": "ok" } }
              },
              "post": {
                "tags": ["pets"],
                "summary": "Add a pet",
                "security": [{ "login": [] }],
                "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } },
                "responses": { "200": { "description": "ok" } }
              }
            },
            "/pets/{petId}": {
              "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } }],
              "get": { "tags": ["pets"], "operationId": "getPet", "responses": { "200": { "description": "ok" } } }
            },
            "/pets/{petId}/photo": {
              "post": {
                "tags": ["pets"],
                "summary": "Upload a photo",
                "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } }],
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "photo": { "type": "string", "format": "binary" },
                          "caption": { "type": "string", "default": "A photo" }
                        }
                      }
                    }
                  }
                },
                "responses": { "200": { "description": "ok" } }
              }
            }
          },
          "components": {
            "securitySchemes": {
              "key": { "type": "apiKey", "in": "header", "name": "X-Api-Key" },
              "login": { "type": "http", "scheme": "basic" }
            },
            "schemas": {
              "Pet": {
                "type": "object",
                "required": ["name"],
                "properties": {
                  "id": { "type": "integer", "readOnly": true },
                  "name": { "type": "string", "example": "Rex" },
                  "tags": { "type": "array", "items": { "type": "string" } },
                  "born": { "type": "string", "format": "date" },
                  "owner": { "$ref": "#/components/schemas/Owner" }
                }
              },
              "Owner": {
                "type": "object",
                "properties": {
                  "email": { "type": "string", "format": "email" },
                  "pets": { "type": "array", "items": { "$ref": "#/components/schemas/Pet" } }
                }
              }
            }
          }
        }
        """;

    public const string OpenApi31Yaml = """
        openapi: 3.1.0
        info:
          title: Pet API
          version: 1.0
        servers:
          - url: https://{host}/v1
            variables:
              host: { default: api.example.com }
        security:
          - key: []
        paths:
          /health:
            get:
              summary: Health
              security: []
              responses:
                200: { description: ok }
          /pets:
            get:
              tags: [pets]
              summary: List pets
              parameters:
                - { name: limit, in: query, schema: { type: [integer, 'null'], default: 10 } }
                - { name: status, in: query, required: true, schema: { type: string } }
                - { name: X-Trace, in: header, schema: { type: string, default: abc } }
              responses:
                200: { description: ok }
            post:
              tags: [pets]
              summary: Add a pet
              security:
                - login: []
              requestBody:
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/Pet'
              responses:
                200: { description: ok }
          /pets/{petId}:
            parameters:
              - { name: petId, in: path, required: true, schema: { type: integer } }
            get:
              tags: [pets]
              operationId: getPet
              responses:
                200: { description: ok }
          /pets/{petId}/photo:
            post:
              tags: [pets]
              summary: Upload a photo
              parameters:
                - { name: petId, in: path, required: true, schema: { type: integer } }
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        photo: { type: string, contentMediaType: image/png }
                        caption: { type: string, default: A photo }
              responses:
                200: { description: ok }
        components:
          securitySchemes:
            key: { type: apiKey, in: header, name: X-Api-Key }
            login: { type: http, scheme: basic }
          schemas:
            Pet:
              type: object
              required: [name]
              properties:
                id: { type: integer, readOnly: true }
                name: { type: string, examples: [Rex, Fido] }
                tags: { type: array, items: { type: string } }
                born: { type: [string, 'null'], format: date }
                owner:
                  anyOf:
                    - { type: 'null' }
                    - { $ref: '#/components/schemas/Owner' }
            Owner:
              type: object
              properties:
                email: { type: string, format: email }
                pets: { type: array, items: { $ref: '#/components/schemas/Pet' } }
        """;

    public const string OpenApi31Json = """
        {
          "openapi": "3.1.0",
          "info": { "title": "Pet API", "version": "1.0" },
          "servers": [{ "url": "https://{host}/v1", "variables": { "host": { "default": "api.example.com" } } }],
          "security": [{ "key": [] }],
          "paths": {
            "/health": {
              "get": { "summary": "Health", "security": [], "responses": { "200": { "description": "ok" } } }
            },
            "/pets": {
              "get": {
                "tags": ["pets"],
                "summary": "List pets",
                "parameters": [
                  { "name": "limit", "in": "query", "schema": { "type": ["integer", "null"], "default": 10 } },
                  { "name": "status", "in": "query", "required": true, "schema": { "type": "string" } },
                  { "name": "X-Trace", "in": "header", "schema": { "type": "string", "default": "abc" } }
                ],
                "responses": { "200": { "description": "ok" } }
              },
              "post": {
                "tags": ["pets"],
                "summary": "Add a pet",
                "security": [{ "login": [] }],
                "requestBody": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } },
                "responses": { "200": { "description": "ok" } }
              }
            },
            "/pets/{petId}": {
              "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } }],
              "get": { "tags": ["pets"], "operationId": "getPet", "responses": { "200": { "description": "ok" } } }
            },
            "/pets/{petId}/photo": {
              "post": {
                "tags": ["pets"],
                "summary": "Upload a photo",
                "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer" } }],
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "photo": { "type": "string", "contentMediaType": "image/png" },
                          "caption": { "type": "string", "default": "A photo" }
                        }
                      }
                    }
                  }
                },
                "responses": { "200": { "description": "ok" } }
              }
            }
          },
          "components": {
            "securitySchemes": {
              "key": { "type": "apiKey", "in": "header", "name": "X-Api-Key" },
              "login": { "type": "http", "scheme": "basic" }
            },
            "schemas": {
              "Pet": {
                "type": "object",
                "required": ["name"],
                "properties": {
                  "id": { "type": "integer", "readOnly": true },
                  "name": { "type": "string", "examples": ["Rex", "Fido"] },
                  "tags": { "type": "array", "items": { "type": "string" } },
                  "born": { "type": ["string", "null"], "format": "date" },
                  "owner": { "anyOf": [{ "type": "null" }, { "$ref": "#/components/schemas/Owner" }] }
                }
              },
              "Owner": {
                "type": "object",
                "properties": {
                  "email": { "type": "string", "format": "email" },
                  "pets": { "type": "array", "items": { "$ref": "#/components/schemas/Pet" } }
                }
              }
            }
          }
        }
        """;
}
