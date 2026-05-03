using System.Text.RegularExpressions;
using Karya.McpServer.Infrastructure;
using Karya.McpServer.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Karya.McpServer.Services;

public sealed partial class PublicApiService : IPublicApiService
{
    private readonly IHttpClientFactory _factory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PublicApiService> _logger;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    // GitHub raw URL for the public-apis README (api.publicapis.org is defunct)
    private const string ReadmeUrl = "https://raw.githubusercontent.com/public-apis/public-apis/master/README.md";

    public PublicApiService(IHttpClientFactory factory, IMemoryCache cache, ILogger<PublicApiService> logger)
    {
        _factory = factory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ApiEntry>> GetAllEntriesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeys.AllEntries, out IReadOnlyList<ApiEntry>? cached) && cached is not null)
            return cached;

        await _fetchLock.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(CacheKeys.AllEntries, out cached) && cached is not null)
                return cached;

            _logger.LogInformation("Cache miss — fetching public-apis README from GitHub");
            var client = _factory.CreateClient(HttpClientNames.PublicApis);
            var markdown = await client.GetStringAsync(ReadmeUrl, ct);
            var entries = ParseReadme(markdown);

            if (entries.Count == 0)
            {
                _logger.LogWarning("README parse returned zero entries");
                return [];
            }

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                SlidingExpiration = TimeSpan.FromHours(6),
                Size = entries.Count
            };
            _cache.Set(CacheKeys.AllEntries, entries, options);
            _logger.LogInformation("Cached {Count} public API entries from GitHub README", entries.Count);
            return entries;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    public async Task<IReadOnlyList<ApiEntry>> SearchAsync(
        string? keyword,
        string? category,
        string? authType,
        bool? httpsOnly,
        string? cors,
        CancellationToken ct = default)
    {
        var all = await GetAllEntriesAsync(ct);

        return all
            .Where(e => keyword is null ||
                e.API.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Where(e => category is null ||
                e.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(e => authType is null ||
                e.Auth.Equals(authType, StringComparison.OrdinalIgnoreCase))
            .Where(e => httpsOnly is null || e.HTTPS == httpsOnly.Value)
            .Where(e => cors is null ||
                e.Cors.Equals(cors, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKeys.Categories, out IReadOnlyList<string>? cached) && cached is not null)
            return cached;

        var all = await GetAllEntriesAsync(ct);
        var categories = all.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();
        _cache.Set(CacheKeys.Categories, (IReadOnlyList<string>)categories, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(48),
            SlidingExpiration = TimeSpan.FromHours(12),
            Size = categories.Count
        });
        return categories;
    }

    public async Task<ApiEntry?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var all = await GetAllEntriesAsync(ct);
        return all.FirstOrDefault(e => e.API.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task WarmCacheAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Warming public APIs cache on startup");
        await GetAllEntriesAsync(ct);
        await GetCategoriesAsync(ct);
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKeys.AllEntries);
        _cache.Remove(CacheKeys.Categories);
        _logger.LogInformation("Public APIs cache invalidated");
    }

    // ── README markdown parser ────────────────────────────────────────────────

    private static IReadOnlyList<ApiEntry> ParseReadme(string markdown)
    {
        var entries = new List<ApiEntry>();
        var lines = markdown.Split('\n');

        string currentCategory = "";

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Category header: ### Animals  (triple-hash)
            if (line.StartsWith("### "))
            {
                currentCategory = line[4..].Trim();
                continue;
            }

            if (string.IsNullOrEmpty(currentCategory)) continue;

            // Skip header/separator rows
            if (!line.StartsWith('|')) continue;
            if (line.Contains(":---") || line.Contains("---|")) continue;
            if (line.Contains("API |") || line.Contains("| API ")) continue;

            // Parse data row: | [Name](URL) | Description | Auth | HTTPS | CORS |
            var cells = SplitRow(line);
            if (cells.Length < 5) continue;

            var (apiName, link) = ExtractLink(cells[0]);
            if (string.IsNullOrWhiteSpace(apiName)) continue;

            var description = cells[1].Trim();
            var auth = cells[2].Trim().Trim('`');
            var httpsStr = cells[3].Trim();
            var corsStr = cells[4].Trim();

            // Skip if this looks like a header row
            if (apiName.Equals("API", StringComparison.OrdinalIgnoreCase)) continue;

            var https = httpsStr.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                        httpsStr.Equals("true", StringComparison.OrdinalIgnoreCase);
            var cors = corsStr.ToLowerInvariant() switch
            {
                "yes" or "true" => "yes",
                "no" or "false" => "no",
                _ => "unknown"
            };

            entries.Add(new ApiEntry(
                API: apiName,
                Description: description,
                Auth: auth.Equals("No", StringComparison.OrdinalIgnoreCase) ? "" : auth,
                HTTPS: https,
                Cors: cors,
                Link: link,
                Category: currentCategory
            ));
        }

        return entries;
    }

    private static string[] SplitRow(string line)
    {
        // Strip leading/trailing pipes and split
        var trimmed = line.Trim().Trim('|');
        return trimmed.Split('|');
    }

    private static (string name, string url) ExtractLink(string cell)
    {
        // Matches [Name](URL) — may have extra spaces
        var match = LinkRegex().Match(cell);
        if (match.Success)
            return (match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());

        // Plain text (no link)
        var plain = cell.Trim();
        return (plain, string.Empty);
    }

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex LinkRegex();
}
