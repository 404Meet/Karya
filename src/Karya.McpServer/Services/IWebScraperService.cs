using Karya.McpServer.Models;

namespace Karya.McpServer.Services;

public interface IWebScraperService
{
    Task<ScrapedDocumentation?> ScrapeAsync(string url, CancellationToken ct = default);
}
