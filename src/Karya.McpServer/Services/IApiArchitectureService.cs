using Karya.McpServer.Models;

namespace Karya.McpServer.Services;

public interface IApiArchitectureService
{
    IReadOnlyList<ArchitectureResource> GetByCategory(ArchitectureCategory category);

    IReadOnlyList<ArchitectureResource> GetByLanguage(string language);

    IReadOnlyList<ArchitectureResource> GetByTag(string tag);

    IReadOnlyList<ArchitectureResource> SearchCatalog(string keyword);

    Task<IReadOnlyList<OpenApiDirectoryEntry>> SearchOpenApiDirectoryAsync(
        string keyword,
        int maxResults = 10,
        CancellationToken ct = default);
}
