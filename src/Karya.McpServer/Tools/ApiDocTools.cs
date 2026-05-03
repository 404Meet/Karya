using System.ComponentModel;
using System.Text.Json;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Tools;

[McpServerToolType]
public static class ApiDocTools
{
    [McpServerTool(Name = "scrape_api_documentation")]
    [Description("Fetches and parses an API documentation page to extract endpoints, auth methods, and code examples. " +
                 "IMPORTANT: Only call this when the user EXPLICITLY requests deep or scraped documentation. " +
                 "This makes an outbound HTTP request to the target site and may be slow. " +
                 "For basic API info, use get_api_details instead.")]
    public static async Task<string> ScrapeApiDocumentation(
        IPublicApiService apiService,
        IWebScraperService scraperService,
        [Description("The API name to look up documentation URL from the registry (e.g. 'Dog Facts')")] string apiName,
        [Description("Optional: override URL to scrape a specific page instead of the default documentation URL")] string? overrideUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            string targetUrl;
            if (!string.IsNullOrWhiteSpace(overrideUrl))
            {
                if (!Uri.TryCreate(overrideUrl, UriKind.Absolute, out _))
                    return JsonSerializer.Serialize(new { error = "overrideUrl is not a valid absolute URL" });
                targetUrl = overrideUrl;
            }
            else
            {
                var entry = await apiService.GetByNameAsync(apiName, cancellationToken);
                if (entry is null)
                    return JsonSerializer.Serialize(new
                    {
                        error = $"API '{apiName}' not found in registry",
                        suggestion = "Use search_public_apis to find the correct API name, or provide an overrideUrl"
                    });
                targetUrl = entry.Link;
            }

            var result = await scraperService.ScrapeAsync(targetUrl, cancellationToken);
            if (result is null)
                return JsonSerializer.Serialize(new
                {
                    error = "Failed to scrape documentation",
                    url = targetUrl,
                    hint = "The page may be unreachable, require JavaScript, or return non-HTML content"
                });

            return JsonSerializer.Serialize(new
            {
                url = result.Url,
                title = result.Title,
                summary = result.Summary,
                endpoints = result.Endpoints,
                authMethods = result.AuthMethods,
                codeExamples = result.CodeExamples,
                scrapedAt = result.ScrapedAt
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Scraping failed: {ex.Message}" });
        }
    }
}
