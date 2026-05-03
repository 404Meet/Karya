namespace Karya.McpServer.Models;

public sealed record ScrapedDocumentation(
    string Url,
    string Title,
    string Summary,
    IReadOnlyList<string> Endpoints,
    IReadOnlyList<string> AuthMethods,
    IReadOnlyList<string> CodeExamples,
    DateTimeOffset ScrapedAt
);
