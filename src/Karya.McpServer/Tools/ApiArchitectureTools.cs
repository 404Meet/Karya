using System.ComponentModel;
using System.Text.Json;
using Karya.McpServer.Models;
using Karya.McpServer.Services;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Tools;

[McpServerToolType]
public static class ApiArchitectureTools
{
    [McpServerTool(Name = "get_api_design_guidance")]
    [Description("Returns curated REST API design best practices and guidelines from authoritative sources " +
                 "(Stoplight, Microsoft Guidelines, Engineering Playbook). Use when designing a new API.")]
    public static string GetApiDesignGuidance(IApiArchitectureService service)
    {
        var resources = service.GetByCategory(ArchitectureCategory.DesignBestPractices);
        return SerializeResources(resources, "API design best practices");
    }

    [McpServerTool(Name = "get_architecture_patterns")]
    [Description("Returns curated resources for a specific API architecture pattern. " +
                 "Valid types: 'clean', 'ddd', 'microservices', 'n-layer', 'cqrs'.")]
    public static string GetArchitecturePatterns(
        IApiArchitectureService service,
        [Description("Architecture pattern type: 'clean', 'ddd', 'microservices', 'n-layer', 'cqrs'")] string patternType)
    {
        var resources = service.GetByTag(patternType.ToLowerInvariant());
        if (resources.Count == 0)
        {
            // Fall back to all architecture pattern resources
            resources = service.GetByCategory(ArchitectureCategory.ArchitecturePatterns);
        }
        return SerializeResources(resources, $"Architecture pattern: {patternType}");
    }

    [McpServerTool(Name = "get_code_generation_tools")]
    [Description("Returns recommended code generation tools for producing API clients or server stubs from an OpenAPI specification. " +
                 "Optionally filter by target programming language.")]
    public static string GetCodeGenerationTools(
        IApiArchitectureService service,
        [Description("Target language to filter tools by: 'csharp', 'java', 'python', 'typescript', 'go', 'ruby', 'rust', 'kotlin'. Leave empty for all.")] string? language)
    {
        IReadOnlyList<ArchitectureResource> resources;
        if (string.IsNullOrWhiteSpace(language))
        {
            resources = service.GetByCategory(ArchitectureCategory.CodeGeneration);
        }
        else
        {
            resources = service.GetByCategory(ArchitectureCategory.CodeGeneration)
                .Where(r =>
                    r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                    r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        return SerializeResources(resources, $"Code generation tools{(language is null ? "" : $" for {language}")}");
    }

    [McpServerTool(Name = "get_validation_library")]
    [Description("Returns recommended validation library for a given programming language. " +
                 "E.g. FluentValidation for C#, Pydantic for Python, Joi for JavaScript.")]
    public static string GetValidationLibrary(
        IApiArchitectureService service,
        [Description("Programming language: 'csharp', 'java', 'python', 'javascript', 'typescript', 'ruby', 'rust', 'go', 'kotlin'")] string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.Validation)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (resources.Count == 0)
            return JsonSerializer.Serialize(new
            {
                language,
                message = $"No specific validation library catalogued for '{language}'. Consider searching GitHub for '{language} validation library'.",
                resources = Array.Empty<object>()
            });

        return SerializeResources(resources, $"Validation libraries for {language}");
    }

    [McpServerTool(Name = "get_mapping_library")]
    [Description("Returns recommended object-mapping library for a given programming language. " +
                 "E.g. AutoMapper for C#, MapStruct for Java.")]
    public static string GetMappingLibrary(
        IApiArchitectureService service,
        [Description("Programming language: 'csharp', 'java', 'python', 'javascript', 'typescript'")] string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.Mapping)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (resources.Count == 0)
            return JsonSerializer.Serialize(new
            {
                language,
                message = $"No specific mapping library catalogued for '{language}'. Consider manual mapping or searching GitHub.",
                resources = Array.Empty<object>()
            });

        return SerializeResources(resources, $"Mapping libraries for {language}");
    }

    [McpServerTool(Name = "get_framework_guide")]
    [Description("Returns official framework implementation guide for building REST APIs in the specified language. " +
                 "E.g. ASP.NET Core for C#, FastAPI for Python, NestJS for TypeScript, Spring for Java.")]
    public static string GetFrameworkGuide(
        IApiArchitectureService service,
        [Description("Programming language: 'csharp', 'java', 'python', 'javascript', 'typescript', 'go', 'ruby', 'rust', 'kotlin'")] string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.FrameworkGuide)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (resources.Count == 0)
            return JsonSerializer.Serialize(new
            {
                language,
                message = $"No catalogued framework guide for '{language}'. Check the official docs for your chosen framework.",
                resources = Array.Empty<object>()
            });

        return SerializeResources(resources, $"Framework guides for {language}");
    }

    [McpServerTool(Name = "search_openapi_directory")]
    [Description("Searches the APIs-guru OpenAPI Directory (7000+ APIs) by provider name or keyword. " +
                 "Returns APIs that have published OpenAPI specifications you can use for code generation or reference.")]
    public static async Task<string> SearchOpenApiDirectory(
        IApiArchitectureService service,
        [Description("Keyword or provider name to search for, e.g. 'stripe', 'twilio', 'weather'")] string keyword,
        [Description("Maximum results to return (default 10, max 50)")] int maxResults,
        CancellationToken cancellationToken)
    {
        if (maxResults <= 0) maxResults = 10;
        try
        {
            var results = await service.SearchOpenApiDirectoryAsync(keyword, maxResults, cancellationToken);

            if (results.Count == 0)
                return JsonSerializer.Serialize(new { keyword, message = "No matching APIs found in the OpenAPI directory.", count = 0, results = Array.Empty<object>() });

            return JsonSerializer.Serialize(new
            {
                keyword,
                count = results.Count,
                results = results.Select(e => new
                {
                    provider = e.Provider,
                    apiName = e.ApiName,
                    title = e.Title,
                    description = e.Description,
                    latestVersion = e.LatestVersion,
                    swaggerUrl = e.SwaggerUrl,
                    infoUrl = e.InfoUrl
                })
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"OpenAPI directory search failed: {ex.Message}" });
        }
    }

    private static string SerializeResources(IReadOnlyList<ArchitectureResource> resources, string context) =>
        JsonSerializer.Serialize(new
        {
            context,
            count = resources.Count,
            resources = resources.Select(r => new
            {
                name = r.Name,
                url = r.Url,
                description = r.Description,
                category = r.Category.ToString(),
                languages = r.Languages,
                tags = r.Tags
            })
        });
}
