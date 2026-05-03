namespace Karya.McpServer.Models;

public enum ResourceKind
{
    GitHubRepo,
    OfficialDocs,
    Tutorial,
    NuGetPackage,
    PypiPackage,
    NpmPackage,
    CratesIo,
    MavenPackage
}

public sealed record ImplementationResource(
    string Name,
    string Url,
    string Description,
    ResourceKind Kind,
    string? Language,
    int? Stars,
    DateTimeOffset? UpdatedAt
);
