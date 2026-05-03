using System.Text.Json;
using Karya.McpServer.Models;
using Karya.McpServer.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Resources;

[McpServerResourceType]
public sealed class ArchitectureResources
{
    [McpServerResource(UriTemplate = "architecture://patterns/{type}", Name = "Architecture Pattern Resources", MimeType = "application/json")]
    public static TextResourceContents GetPatternResources(
        IApiArchitectureService service,
        string type)
    {
        var resources = service.GetByTag(type.ToLowerInvariant());
        if (resources.Count == 0)
            resources = service.GetByCategory(ArchitectureCategory.ArchitecturePatterns);

        return new TextResourceContents
        {
            Uri = $"architecture://patterns/{Uri.EscapeDataString(type)}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { patternType = type, count = resources.Count, resources })
        };
    }

    [McpServerResource(UriTemplate = "architecture://codegen/{language}", Name = "Code Generation Tools", MimeType = "application/json")]
    public static TextResourceContents GetCodegenResources(
        IApiArchitectureService service,
        string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.CodeGeneration)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new TextResourceContents
        {
            Uri = $"architecture://codegen/{Uri.EscapeDataString(language)}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { language, count = resources.Count, resources })
        };
    }

    [McpServerResource(UriTemplate = "architecture://validation/{language}", Name = "Validation Libraries", MimeType = "application/json")]
    public static TextResourceContents GetValidationResources(
        IApiArchitectureService service,
        string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.Validation)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new TextResourceContents
        {
            Uri = $"architecture://validation/{Uri.EscapeDataString(language)}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { language, count = resources.Count, resources })
        };
    }

    [McpServerResource(UriTemplate = "architecture://frameworks/{language}", Name = "Framework Guides", MimeType = "application/json")]
    public static TextResourceContents GetFrameworkResources(
        IApiArchitectureService service,
        string language)
    {
        var resources = service.GetByCategory(ArchitectureCategory.FrameworkGuide)
            .Where(r =>
                r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
                r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new TextResourceContents
        {
            Uri = $"architecture://frameworks/{Uri.EscapeDataString(language)}",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(new { language, count = resources.Count, resources })
        };
    }
}
