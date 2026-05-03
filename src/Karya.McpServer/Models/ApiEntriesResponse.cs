using System.Text.Json.Serialization;

namespace Karya.McpServer.Models;

public sealed record ApiEntriesResponse(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("entries")] IReadOnlyList<ApiEntry> Entries
);
