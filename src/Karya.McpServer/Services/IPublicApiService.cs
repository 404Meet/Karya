using Karya.McpServer.Models;

namespace Karya.McpServer.Services;

public interface IPublicApiService
{
    Task<IReadOnlyList<ApiEntry>> GetAllEntriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ApiEntry>> SearchAsync(
        string? keyword,
        string? category,
        string? authType,
        bool? httpsOnly,
        string? cors,
        CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

    Task<ApiEntry?> GetByNameAsync(string name, CancellationToken ct = default);

    Task WarmCacheAsync(CancellationToken ct = default);

    void InvalidateCache();
}
