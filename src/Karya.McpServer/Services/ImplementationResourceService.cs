using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Karya.McpServer.Infrastructure;
using Karya.McpServer.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Karya.McpServer.Services;

public sealed class ImplementationResourceService : IImplementationResourceService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ImplementationResourceService> _logger;
    private readonly string? _githubToken;

    private static readonly IReadOnlyList<string> AllLanguages =
        ["csharp", "java", "python", "javascript", "typescript", "ruby", "rust", "go", "kotlin"];

    public ImplementationResourceService(
        IHttpClientFactory factory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<ImplementationResourceService> logger)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
        _githubToken = configuration["Karya:GitHub:Token"];
    }

    public async Task<IReadOnlyList<ImplementationResource>> SearchGitHubAsync(
        string apiName,
        string? language,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        maxResults = Math.Clamp(maxResults, 1, 20);
        var cacheKey = CacheKeys.GitHubSearch(apiName, language);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ImplementationResource>? cached) && cached is not null)
            return cached;

        var client = _factory.CreateClient(HttpClientNames.GitHub);
        if (!string.IsNullOrEmpty(_githubToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubToken);

        var query = string.IsNullOrEmpty(language)
            ? $"{Uri.EscapeDataString(apiName)}+in:name,description"
            : $"{Uri.EscapeDataString(apiName)}+in:name,description+language:{language}";

        var url = $"search/repositories?q={query}&sort=stars&order=desc&per_page={maxResults}";

        GitHubSearchResponse? searchResult;
        try
        {
            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub search returned {Status} for query {Query}", (int)response.StatusCode, apiName);
                return [];
            }

            var remaining = response.Headers.TryGetValues("X-RateLimit-Remaining", out var vals)
                ? vals.FirstOrDefault() : null;
            if (int.TryParse(remaining, out var remainingCount) && remainingCount < 5)
                _logger.LogWarning("GitHub rate limit nearly exhausted: {Remaining} requests remaining", remainingCount);

            searchResult = await response.Content.ReadFromJsonAsync<GitHubSearchResponse>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub search failed for {ApiName}", apiName);
            return [];
        }

        var results = (searchResult?.Items ?? [])
            .Select(item => new ImplementationResource(
                Name: item.FullName,
                Url: item.HtmlUrl,
                Description: item.Description ?? string.Empty,
                Kind: ResourceKind.GitHubRepo,
                Language: item.Language,
                Stars: item.StargazersCount,
                UpdatedAt: item.UpdatedAt
            ))
            .ToList();

        _cache.Set(cacheKey, (IReadOnlyList<ImplementationResource>)results, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120),
            Size = results.Count + 1
        });

        return results;
    }

    public async Task<IReadOnlyList<ImplementationResource>> SearchPackageRegistriesAsync(
        string apiName,
        IReadOnlyList<string>? languages = null,
        CancellationToken ct = default)
    {
        var targets = languages is { Count: > 0 } ? languages : AllLanguages;
        var tasks = targets.Select(lang => SearchSingleRegistryAsync(apiName, lang, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<ImplementationResource>> SearchSingleRegistryAsync(
        string apiName, string language, CancellationToken ct)
    {
        var cacheKey = CacheKeys.PackageSearch(apiName, language);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ImplementationResource>? cached) && cached is not null)
            return cached;

        IReadOnlyList<ImplementationResource> results;
        try
        {
            results = language.ToLowerInvariant() switch
            {
                "csharp" => await SearchNuGetAsync(apiName, ct),
                "python" => await SearchPyPiAsync(apiName, ct),
                "javascript" or "typescript" => await SearchNpmAsync(apiName, ct),
                "rust" => await SearchCratesIoAsync(apiName, ct),
                _ => []
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Package search failed for {ApiName}/{Language}", apiName, language);
            results = [];
        }

        _cache.Set(cacheKey, results, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120),
            Size = results.Count + 1
        });
        return results;
    }

    private async Task<IReadOnlyList<ImplementationResource>> SearchNuGetAsync(string query, CancellationToken ct)
    {
        var client = _factory.CreateClient(HttpClientNames.GitHub); // reuse generic client
        var url = $"https://azuresearch-usnc.nuget.org/query?q={Uri.EscapeDataString(query)}&take=5";
        var json = await client.GetFromJsonAsync<JsonElement>(url, ct);
        return json.TryGetProperty("data", out var data)
            ? data.EnumerateArray()
                .Select(p => new ImplementationResource(
                    Name: p.GetProperty("id").GetString() ?? "",
                    Url: $"https://www.nuget.org/packages/{p.GetProperty("id").GetString()}",
                    Description: p.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Kind: ResourceKind.NuGetPackage,
                    Language: "csharp",
                    Stars: null,
                    UpdatedAt: null))
                .ToList()
            : [];
    }

    private async Task<IReadOnlyList<ImplementationResource>> SearchPyPiAsync(string query, CancellationToken ct)
    {
        var client = _factory.CreateClient(HttpClientNames.GitHub);
        var url = $"https://pypi.org/search/?q={Uri.EscapeDataString(query)}&format=json";
        // PyPI search doesn't provide a clean JSON API; use simple package lookup instead
        var packageUrl = $"https://pypi.org/pypi/{Uri.EscapeDataString(query.Replace(" ", "-").ToLowerInvariant())}/json";
        try
        {
            var json = await client.GetFromJsonAsync<JsonElement>(packageUrl, ct);
            var info = json.GetProperty("info");
            return [new ImplementationResource(
                Name: info.GetProperty("name").GetString() ?? query,
                Url: info.TryGetProperty("project_url", out var pu) ? pu.GetString() ?? "" : $"https://pypi.org/project/{query}",
                Description: info.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                Kind: ResourceKind.PypiPackage,
                Language: "python",
                Stars: null,
                UpdatedAt: null)];
        }
        catch
        {
            return [];
        }
    }

    private async Task<IReadOnlyList<ImplementationResource>> SearchNpmAsync(string query, CancellationToken ct)
    {
        var client = _factory.CreateClient(HttpClientNames.GitHub);
        var url = $"https://registry.npmjs.org/-/v1/search?text={Uri.EscapeDataString(query)}&size=5";
        var json = await client.GetFromJsonAsync<JsonElement>(url, ct);
        if (!json.TryGetProperty("objects", out var objects)) return [];
        return objects.EnumerateArray()
            .Select(obj =>
            {
                var pkg = obj.GetProperty("package");
                var name = pkg.GetProperty("name").GetString() ?? "";
                return new ImplementationResource(
                    Name: name,
                    Url: $"https://www.npmjs.com/package/{name}",
                    Description: pkg.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Kind: ResourceKind.NpmPackage,
                    Language: "javascript/typescript",
                    Stars: null,
                    UpdatedAt: null);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<ImplementationResource>> SearchCratesIoAsync(string query, CancellationToken ct)
    {
        var client = _factory.CreateClient(HttpClientNames.GitHub);
        var url = $"https://crates.io/api/v1/crates?q={Uri.EscapeDataString(query)}&per_page=5";
        var json = await client.GetFromJsonAsync<JsonElement>(url, ct);
        if (!json.TryGetProperty("crates", out var crates)) return [];
        return crates.EnumerateArray()
            .Select(c =>
            {
                var name = c.GetProperty("name").GetString() ?? "";
                return new ImplementationResource(
                    Name: name,
                    Url: $"https://crates.io/crates/{name}",
                    Description: c.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                    Kind: ResourceKind.CratesIo,
                    Language: "rust",
                    Stars: null,
                    UpdatedAt: null);
            })
            .ToList();
    }

    // ── GitHub JSON models ────────────────────────────────────────────────────

    private sealed record GitHubSearchResponse(
        [property: JsonPropertyName("items")] IReadOnlyList<GitHubRepo> Items
    );

    private sealed record GitHubRepo(
        [property: JsonPropertyName("full_name")] string FullName,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("stargazers_count")] int StargazersCount,
        [property: JsonPropertyName("language")] string? Language,
        [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt
    );
}
