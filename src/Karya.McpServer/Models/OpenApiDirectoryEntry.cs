using System.Text.Json.Serialization;

namespace Karya.McpServer.Models;

public sealed record OpenApiDirectoryEntry(
    string Provider,
    string ApiName,
    string Title,
    string Description,
    string? SwaggerUrl,
    string? InfoUrl,
    string? ContactEmail,
    string LatestVersion
);

public sealed record OpenApiDirectoryApiInfo(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("contact")] OpenApiContact? Contact,
    [property: JsonPropertyName("x-origin")] IReadOnlyList<OpenApiOrigin>? Origins
);

public sealed record OpenApiContact(
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("url")] string? Url
);

public sealed record OpenApiOrigin(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("format")] string? Format
);

public sealed record OpenApiDirectoryVersion(
    [property: JsonPropertyName("info")] OpenApiDirectoryApiInfo Info,
    [property: JsonPropertyName("swaggerUrl")] string? SwaggerUrl
);
