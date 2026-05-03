namespace Karya.McpServer.Infrastructure;

internal static class CacheKeys
{
    public const string AllEntries = "publicapis:entries:all";
    public const string Categories = "publicapis:categories";
    public const string OpenApiDirectory = "apisGuru:directory";

    public static string GitHubSearch(string apiName, string? language) =>
        $"github:search:{apiName.ToLowerInvariant()}:{language?.ToLowerInvariant() ?? "all"}";

    public static string PackageSearch(string apiName, string language) =>
        $"packages:search:{apiName.ToLowerInvariant()}:{language.ToLowerInvariant()}";
}
