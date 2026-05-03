using System.Text.Json;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace Karya.McpServer.Resources;

[McpServerResourceType]
public sealed class PublicApiResources
{
    [McpServerResource(UriTemplate = "public-apis://categories", Name = "API Categories", MimeType = "application/json")]
    public static async Task<TextResourceContents> GetCategories(
        IPublicApiService apiService,
        CancellationToken cancellationToken)
    {
        var categories = await apiService.GetCategoriesAsync(cancellationToken);
        return new TextResourceContents
        {
            Uri = "public-apis://categories",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { count = categories.Count, categories })
        };
    }

    [McpServerResource(UriTemplate = "public-apis://entry/{apiName}", Name = "API Entry", MimeType = "application/json")]
    public static async Task<TextResourceContents> GetEntry(
        IPublicApiService apiService,
        string apiName,
        CancellationToken cancellationToken)
    {
        var entry = await apiService.GetByNameAsync(apiName, cancellationToken);
        var text = entry is null
            ? JsonSerializer.Serialize(new { error = $"API '{apiName}' not found" })
            : JsonSerializer.Serialize(entry);

        return new TextResourceContents
        {
            Uri = $"public-apis://entry/{Uri.EscapeDataString(apiName)}",
            MimeType = "application/json",
            Text = text
        };
    }

    [McpServerResource(UriTemplate = "public-apis://search/{query}", Name = "API Search Results", MimeType = "application/json")]
    public static async Task<TextResourceContents> SearchResults(
        IPublicApiService apiService,
        string query,
        CancellationToken cancellationToken)
    {
        var entries = await apiService.SearchAsync(query, null, null, null, null, cancellationToken);
        return new TextResourceContents
        {
            Uri = $"public-apis://search/{Uri.EscapeDataString(query)}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { query, count = entries.Count, entries })
        };
    }
}
