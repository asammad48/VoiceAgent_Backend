using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace VoiceAgent.Api;

public sealed class WebSocketDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        AddWebSocketPath(swaggerDoc, "/api/voice/web-stream", "Web voice stream WebSocket endpoint.");
        AddWebSocketPath(swaggerDoc, "/api/voice/phone-stream", "Phone voice stream WebSocket endpoint.");
    }

    private static void AddWebSocketPath(OpenApiDocument doc, string path, string summary)
    {
        if (doc.Paths.ContainsKey(path))
        {
            return;
        }

        doc.Paths[path] = new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Summary = summary,
                    Description = "Upgrade this request to WebSocket. Use the `ws://` or `wss://` scheme with this path. First text frame: { \"type\": \"session\", \"callSessionId\": \"GUID\" }.",
                    Tags = new List<OpenApiTag> { new() { Name = "Voice WebSocket" } },
                    Responses =
                    {
                        ["101"] = new OpenApiResponse { Description = "Switching Protocols (WebSocket upgrade)." },
                        ["400"] = new OpenApiResponse { Description = "Bad request (not a WebSocket upgrade request)." }
                    }
                }
            }
        };
    }
}
