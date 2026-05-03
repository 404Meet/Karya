using System.Text.Json;
using Karya.McpServer.Infrastructure;
using Karya.McpServer.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Karya.McpServer.Services;

public sealed class ApiArchitectureService : IApiArchitectureService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ApiArchitectureService> _logger;
    private readonly SemaphoreSlim _directoryLock = new(1, 1);

    public ApiArchitectureService(
        IHttpClientFactory factory,
        IMemoryCache cache,
        ILogger<ApiArchitectureService> logger)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
    }

    public IReadOnlyList<ArchitectureResource> GetByCategory(ArchitectureCategory category) =>
        ArchitectureCatalog.ByCategory(category);

    public IReadOnlyList<ArchitectureResource> GetByLanguage(string language) =>
        ArchitectureCatalog.ByLanguage(language);

    public IReadOnlyList<ArchitectureResource> GetByTag(string tag) =>
        ArchitectureCatalog.ByTag(tag);

    public IReadOnlyList<ArchitectureResource> SearchCatalog(string keyword) =>
        ArchitectureCatalog.Search(keyword);

    public async Task<IReadOnlyList<OpenApiDirectoryEntry>> SearchOpenApiDirectoryAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        maxResults = Math.Clamp(maxResults, 1, 50);
        var all = await GetAllOpenApiDirectoryAsync(ct);

        return all
            .Where(e =>
                e.Provider.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                e.ApiName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                e.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }

    private async Task<IReadOnlyList<OpenApiDirectoryEntry>> GetAllOpenApiDirectoryAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKeys.OpenApiDirectory, out IReadOnlyList<OpenApiDirectoryEntry>? cached) && cached is not null)
            return cached;

        await _directoryLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(CacheKeys.OpenApiDirectory, out cached) && cached is not null)
                return cached;

            _logger.LogInformation("Fetching OpenAPI directory from apis.guru");
            var client = _factory.CreateClient(HttpClientNames.ApisGuru);
            var json = await client.GetStringAsync("list.json", ct);
            var entries = ParseApisGuruList(json);

            _cache.Set(CacheKeys.OpenApiDirectory, entries, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12),
                Size = entries.Count
            });
            _logger.LogInformation("Cached {Count} APIs from apis.guru directory", entries.Count);
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch OpenAPI directory from apis.guru");
            return [];
        }
        finally
        {
            _directoryLock.Release();
        }
    }

    private static IReadOnlyList<OpenApiDirectoryEntry> ParseApisGuruList(string json)
    {
        // apis.guru format: { "provider.com": { "apiName": { "versions": { "v1": { "info": {...}, "swaggerUrl": "..." } } } } }
        using var doc = JsonDocument.Parse(json);
        var results = new List<OpenApiDirectoryEntry>();

        foreach (var provider in doc.RootElement.EnumerateObject())
        {
            foreach (var api in provider.Value.EnumerateObject())
            {
                var versionsEl = api.Value.TryGetProperty("versions", out var v) ? v : api.Value;
                // Get the latest version entry
                JsonElement? latestVersion = null;
                string latestKey = "";
                foreach (var version in versionsEl.EnumerateObject())
                {
                    latestKey = version.Name;
                    latestVersion = version.Value;
                }

                if (latestVersion is null) continue;

                var versionEl = latestVersion.Value;
                var info = versionEl.TryGetProperty("info", out var infoEl) ? infoEl : (JsonElement?)null;
                var title = info?.TryGetProperty("title", out var titleEl) == true ? titleEl.GetString() ?? provider.Name : provider.Name;
                var description = info?.TryGetProperty("description", out var descEl) == true ? descEl.GetString() ?? "" : "";
                var swaggerUrl = versionEl.TryGetProperty("swaggerUrl", out var swEl) ? swEl.GetString() : null;
                var contactEmail = info?.TryGetProperty("contact", out var contactEl) == true
                    && contactEl.TryGetProperty("email", out var emailEl)
                    ? emailEl.GetString() : null;
                var infoUrl = info?.TryGetProperty("contact", out var ci) == true
                    && ci.TryGetProperty("url", out var cu)
                    ? cu.GetString() : null;

                results.Add(new OpenApiDirectoryEntry(
                    Provider: provider.Name,
                    ApiName: api.Name == "api" ? provider.Name : api.Name,
                    Title: title,
                    Description: description,
                    SwaggerUrl: swaggerUrl,
                    InfoUrl: infoUrl,
                    ContactEmail: contactEmail,
                    LatestVersion: latestKey
                ));
            }
        }

        return results;
    }
}
