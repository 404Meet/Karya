namespace Karya.McpServer.Models;

public sealed record ApiSearchResult(
    string Name,
    string Description,
    string Category,
    string DocumentationUrl,
    string AuthType,
    bool SupportsHttps,
    string CorsStatus,
    IReadOnlyList<string> Tags
);
