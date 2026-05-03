using System.Text.Json.Serialization;

namespace Karya.McpServer.Models;

public sealed record ApiEntry(
    [property: JsonPropertyName("API")] string API,
    [property: JsonPropertyName("Description")] string Description,
    [property: JsonPropertyName("Auth")] string Auth,
    [property: JsonPropertyName("HTTPS")] bool HTTPS,
    [property: JsonPropertyName("Cors")] string Cors,
    [property: JsonPropertyName("Link")] string Link,
    [property: JsonPropertyName("Category")] string Category
);
