using System.ComponentModel;
using System.Text.Json;
using Karya.McpServer.Models;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Tools;

[McpServerToolType]
public static class ApiDiscoveryTools
{
    [McpServerTool(Name = "search_public_apis")]
    [Description("Search the public-apis registry (1500+ free APIs) by keyword, category, or auth type. " +
                 "Returns a list of matching APIs with documentation links, auth type, and HTTPS/CORS status. " +
                 "Use get_api_categories first to see valid category names.")]
    public static async Task<string> SearchPublicApis(
        IPublicApiService apiService,
        [Description("Keyword to match against API name and description (optional)")] string? keyword,
        [Description("Category to filter by, e.g. 'Animals', 'Finance', 'Weather' (use get_api_categories for valid values)")] string? category,
        [Description("Auth type filter: 'apiKey', 'OAuth', 'No', or empty for any")] string? authType,
        [Description("If true, only return APIs that support HTTPS")] bool? httpsOnly,
        [Description("CORS filter: 'yes', 'no', or 'unknown'")] string? cors,
        CancellationToken cancellationToken)
    {
        try
        {
            var entries = await apiService.SearchAsync(keyword, category, authType, httpsOnly, cors, cancellationToken);

            if (entries.Count == 0)
                return JsonSerializer.Serialize(new { message = "No APIs found matching the criteria. Try broader search terms or check category names with get_api_categories.", count = 0, entries = Array.Empty<object>() });

            var results = entries.Select(ToSearchResult).ToList();
            return JsonSerializer.Serialize(new { count = results.Count, entries = results });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Search failed: {ex.Message}" });
        }
    }

    [McpServerTool(Name = "get_api_categories")]
    [Description("Returns all available API categories in the public-apis registry. " +
                 "Use the returned category names as the 'category' parameter in search_public_apis.")]
    public static async Task<string> GetApiCategories(
        IPublicApiService apiService,
        CancellationToken cancellationToken)
    {
        try
        {
            var categories = await apiService.GetCategoriesAsync(cancellationToken);
            return JsonSerializer.Serialize(new { count = categories.Count, categories });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to fetch categories: {ex.Message}" });
        }
    }

    private static object ToSearchResult(ApiEntry e) => new
    {
        name = e.API,
        description = e.Description,
        category = e.Category,
        documentationUrl = e.Link,
        authType = NormalizeAuth(e.Auth),
        supportsHttps = e.HTTPS,
        corsStatus = e.Cors
    };

    private static string NormalizeAuth(string auth) => auth.ToLowerInvariant() switch
    {
        "apikey" => "ApiKey",
        "oauth" => "OAuth",
        "x-mashape-key" => "X-Mashape-Key",
        "user-agent" => "User-Agent",
        "" or "no" => "None",
        _ => auth
    };
}
