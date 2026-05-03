using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html.Parser;
using Karya.McpServer.Infrastructure;
using Karya.McpServer.Models;

namespace Karya.McpServer.Services;

public sealed partial class WebScraperService : IWebScraperService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<WebScraperService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _hostLocks = new();

    private const int PerHostDelayMs = 1500;
    private const int MaxCodeExamples = 5;
    private const int MaxCodeExampleLength = 1000;

    public WebScraperService(IHttpClientFactory factory, ILogger<WebScraperService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<ScrapedDocumentation?> ScrapeAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid URL provided for scraping: {Url}", url);
            return null;
        }

        var hostLock = _hostLocks.GetOrAdd(uri.Host, _ => new SemaphoreSlim(1, 1));
        await hostLock.WaitAsync(ct);
        try
        {
            return await ScrapeInternalAsync(url, ct);
        }
        finally
        {
            // Enforce polite delay before releasing lock so next caller waits
            await Task.Delay(PerHostDelayMs, CancellationToken.None);
            hostLock.Release();
        }
    }

    private async Task<ScrapedDocumentation?> ScrapeInternalAsync(string url, CancellationToken ct)
    {
        _logger.LogInformation("Scraping {Url}", url);
        var client = _factory.CreateClient(HttpClientNames.Scraper);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Url}", url);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HTTP {Status} for {Url}", (int)response.StatusCode, url);
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Non-HTML content type '{ContentType}' for {Url}", contentType, url);
            return null;
        }

        var html = await response.Content.ReadAsStringAsync(ct);
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(html, ct);

        var title = document.Title
            ?? document.QuerySelector("h1")?.TextContent?.Trim()
            ?? url;

        var mainContent = document.QuerySelector("main, article, [role='main'], .content, #content")
            ?? document.Body;

        var summary = mainContent?
            .QuerySelectorAll("p")
            .Select(p => p.TextContent?.Trim())
            .FirstOrDefault(t => t?.Length > 50)
            ?? string.Empty;

        var bodyText = document.Body?.TextContent ?? string.Empty;
        var endpoints = ExtractEndpoints(bodyText);
        var authMethods = ExtractAuthMethods(bodyText);
        var codeExamples = document
            .QuerySelectorAll("pre, code")
            .Select(el => el.TextContent?.Trim() ?? string.Empty)
            .Where(t => t.Length > 10)
            .Distinct()
            .Take(MaxCodeExamples)
            .Select(t => t.Length > MaxCodeExampleLength ? t[..MaxCodeExampleLength] + "..." : t)
            .ToList();

        return new ScrapedDocumentation(
            Url: url,
            Title: title.Trim(),
            Summary: summary,
            Endpoints: endpoints,
            AuthMethods: authMethods,
            CodeExamples: codeExamples,
            ScrapedAt: DateTimeOffset.UtcNow
        );
    }

    private static IReadOnlyList<string> ExtractEndpoints(string text)
    {
        var matches = EndpointRegex().Matches(text);
        return matches
            .Select(m => m.Value)
            .Distinct()
            .Take(20)
            .ToList();
    }

    private static IReadOnlyList<string> ExtractAuthMethods(string text)
    {
        var methods = new List<string>();
        string[] patterns = ["Bearer", "API Key", "OAuth", "Basic Auth", "X-API-Key", "JWT", "HMAC"];
        foreach (var pattern in patterns)
        {
            if (text.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                methods.Add(pattern);
        }
        return methods;
    }

    [GeneratedRegex(@"(?:GET|POST|PUT|PATCH|DELETE)\s+(/[a-zA-Z0-9/_\-{}?.=&]+)|(/(?:v\d+|api)/[a-zA-Z0-9/_\-{}]+)")]
    private static partial Regex EndpointRegex();
}
