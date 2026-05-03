using System.ComponentModel;
using System.Text.Json;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Tools;

[McpServerToolType]
public static class ImplementationTools
{
    [McpServerTool(Name = "find_implementation_resources")]
    [Description("Searches GitHub and package registries for libraries, SDKs, connectors, and code examples " +
                 "that implement or integrate with a given API. " +
                 "Optionally filter by programming language.")]
    public static async Task<string> FindImplementationResources(
        IImplementationResourceService resourceService,
        [Description("The API or technology name to search for, e.g. 'Stripe', 'Twilio', 'OpenWeatherMap'")] string apiName,
        [Description("Programming language: 'csharp', 'java', 'python', 'javascript', 'typescript', 'ruby', 'rust', 'go', 'kotlin'. Leave empty for all languages.")] string? language,
        [Description("Maximum GitHub results to return (default 10, max 20)")] int maxGitHubResults,
        [Description("Whether to also search package registries (NuGet, PyPI, npm, Crates.io)")] bool includePackages,
        CancellationToken cancellationToken)
    {
        if (maxGitHubResults <= 0) maxGitHubResults = 10;

        try
        {
            var githubTask = resourceService.SearchGitHubAsync(apiName, language, maxGitHubResults, cancellationToken);

            List<Models.ImplementationResource> packageResults = [];
            if (includePackages)
            {
                var langs = string.IsNullOrWhiteSpace(language)
                    ? null
                    : (IReadOnlyList<string>)[language];
                packageResults = (await resourceService.SearchPackageRegistriesAsync(apiName, langs, cancellationToken)).ToList();
            }

            var githubResults = await githubTask;

            var allResults = githubResults
                .Concat(packageResults)
                .DistinctBy(r => r.Url)
                .OrderByDescending(r => r.Stars ?? 0)
                .Select(r => new
                {
                    name = r.Name,
                    url = r.Url,
                    description = r.Description,
                    kind = r.Kind.ToString(),
                    language = r.Language,
                    stars = r.Stars,
                    updatedAt = r.UpdatedAt
                })
                .ToList();

            return JsonSerializer.Serialize(new
            {
                query = apiName,
                language = language ?? "all",
                count = allResults.Count,
                results = allResults
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Search failed: {ex.Message}" });
        }
    }
}
