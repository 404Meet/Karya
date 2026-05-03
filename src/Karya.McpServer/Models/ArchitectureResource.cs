namespace Karya.McpServer.Models;

public enum ArchitectureCategory
{
    DesignBestPractices,
    ArchitecturePatterns,
    OpenApiSpec,
    CodeGeneration,
    Validation,
    Mapping,
    FrameworkGuide,
    ApiDirectory
}

public sealed record ArchitectureResource(
    string Name,
    string Url,
    string Description,
    ArchitectureCategory Category,
    IReadOnlyList<string> Languages,
    IReadOnlyList<string> Tags
);
