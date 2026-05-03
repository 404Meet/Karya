using System.ComponentModel;
using System.Text.Json;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Tools;

[McpServerToolType]
public static class ApiDetailTools
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "a", "an", "the", "for", "with", "to", "and", "or", "in", "on",
        "of", "that", "is", "it", "be", "by", "at", "as", "from", "need",
        "want", "build", "create", "make", "something", "some", "use", "using"
    };

    [McpServerTool(Name = "get_api_details")]
    [Description("Returns full details for a specific API including documentation URL, auth setup hints, " +
                 "HTTPS/CORS status, and usage notes. Use the exact API name from search_public_apis results.")]
    public static async Task<string> GetApiDetails(
        IPublicApiService apiService,
        [Description("Exact API name as returned by search_public_apis, e.g. 'Dog Facts'")] string apiName,
        CancellationToken cancellationToken)
    {
        try
        {
            var entry = await apiService.GetByNameAsync(apiName, cancellationToken);
            if (entry is null)
                return JsonSerializer.Serialize(new
                {
                    error = $"API '{apiName}' not found",
                    suggestion = "Use search_public_apis to find the correct API name"
                });

            return JsonSerializer.Serialize(new
            {
                name = entry.API,
                description = entry.Description,
                category = entry.Category,
                documentationUrl = entry.Link,
                authType = entry.Auth,
                authSetupHint = GetAuthHint(entry.Auth),
                supportsHttps = entry.HTTPS,
                httpsNote = entry.HTTPS ? "Safe to use in production." : "Consider using only in trusted/internal networks.",
                corsStatus = entry.Cors,
                corsNote = entry.Cors.ToLowerInvariant() switch
                {
                    "yes" => "Supports CORS — can be called from browser JavaScript directly.",
                    "no" => "No CORS support — use a server-side proxy for browser-based apps.",
                    _ => "CORS support unknown — test before using in browser-based apps."
                }
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Failed to get API details: {ex.Message}" });
        }
    }

    [McpServerTool(Name = "suggest_similar_apis")]
    [Description("Given a natural language description of what you need, suggests matching public APIs. " +
                 "Better for open-ended discovery than search_public_apis. Uses keyword relevance scoring.")]
    public static async Task<string> SuggestSimilarApis(
        IPublicApiService apiService,
        [Description("Natural language description of what you need, e.g. 'weather forecasts for mobile apps' or 'send SMS text messages'")] string description,
        [Description("Maximum number of suggestions to return (default 5, max 15)")] int maxResults,
        CancellationToken cancellationToken)
    {
        maxResults = Math.Clamp(maxResults <= 0 ? 5 : maxResults, 1, 15);
        try
        {
            var all = await apiService.GetAllEntriesAsync(cancellationToken);

            var keywords = description
                .Split([' ', ',', '.', '!', '?', ';', ':', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !StopWords.Contains(w))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (keywords.Count == 0)
                return JsonSerializer.Serialize(new { message = "No meaningful keywords found. Try a more descriptive query.", suggestions = Array.Empty<object>() });

            var scored = all
                .Select(e =>
                {
                    var haystack = $"{e.API} {e.Description} {e.Category}";
                    var score = keywords.Sum(kw =>
                        haystack.Contains(kw, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
                    return (entry: e, score);
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Take(maxResults)
                .Select(x => new
                {
                    name = x.entry.API,
                    description = x.entry.Description,
                    category = x.entry.Category,
                    documentationUrl = x.entry.Link,
                    authType = x.entry.Auth,
                    relevanceScore = x.score
                })
                .ToList();

            if (scored.Count == 0)
                return JsonSerializer.Serialize(new { message = "No matching APIs found. Try different keywords.", suggestions = Array.Empty<object>() });

            return JsonSerializer.Serialize(new { count = scored.Count, keywords, suggestions = scored });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Suggestion failed: {ex.Message}" });
        }
    }

    private static string GetAuthHint(string auth) => auth.ToLowerInvariant() switch
    {
        "apikey" => "Pass the API key as a query parameter (?key=YOUR_KEY), Authorization: Bearer header, or X-API-Key header — check the specific API's docs for the exact field name.",
        "oauth" => "Requires OAuth 2.0. Obtain an access token via the provider's authorization endpoint and pass it as: Authorization: Bearer <access_token>",
        "x-mashape-key" => "Pass your RapidAPI key as the X-Mashape-Key header: X-Mashape-Key: YOUR_KEY",
        "user-agent" => "Set a descriptive User-Agent header identifying your application: User-Agent: MyApp/1.0",
        "" or "no" => "No authentication required — call the API directly without credentials.",
        _ => $"Auth type: {auth}. Check the API documentation for setup instructions."
    };
}
