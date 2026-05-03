using Karya.McpServer.Models;

namespace Karya.McpServer.Services;

public interface IImplementationResourceService
{
    Task<IReadOnlyList<ImplementationResource>> SearchGitHubAsync(
        string apiName,
        string? language,
        int maxResults = 10,
        CancellationToken ct = default);

    Task<IReadOnlyList<ImplementationResource>> SearchPackageRegistriesAsync(
        string apiName,
        IReadOnlyList<string>? languages = null,
        CancellationToken ct = default);
}
