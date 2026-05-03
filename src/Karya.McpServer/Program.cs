using Karya.McpServer.Infrastructure;
using Karya.McpServer.Services;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// ── Memory Cache ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache(options => options.SizeLimit = 50_000_000);

// ── Named HttpClients ─────────────────────────────────────────────────────────

// Raw GitHub content — serves the public-apis README markdown
builder.Services
    .AddHttpClient(HttpClientNames.PublicApis, client =>
    {
        client.BaseAddress = new Uri("https://raw.githubusercontent.com/");
        client.DefaultRequestHeaders.Add("Accept", "text/plain");
        client.DefaultRequestHeaders.Add("User-Agent", "Karya-MCP-Server/1.0");
        client.Timeout = TimeSpan.FromSeconds(20);
    })
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient(HttpClientNames.Scraper, client =>
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Karya-MCP-Scraper/1.0 (educational)");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddResilienceHandler("ScraperPipeline", pipeline =>
    {
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(3),
        });
        pipeline.AddTimeout(TimeSpan.FromSeconds(25));
    });

builder.Services
    .AddHttpClient(HttpClientNames.GitHub, client =>
    {
        client.BaseAddress = new Uri("https://api.github.com/");
        client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        client.DefaultRequestHeaders.Add("User-Agent", "Karya-MCP-Server/1.0");
        client.Timeout = TimeSpan.FromSeconds(20);
    })
    .AddStandardResilienceHandler();

builder.Services
    .AddHttpClient(HttpClientNames.ApisGuru, client =>
    {
        client.BaseAddress = new Uri("https://api.apis.guru/v2/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "Karya-MCP-Server/1.0");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<IPublicApiService, PublicApiService>();
builder.Services.AddSingleton<IWebScraperService, WebScraperService>();
builder.Services.AddSingleton<IImplementationResourceService, ImplementationResourceService>();
builder.Services.AddSingleton<IApiArchitectureService, ApiArchitectureService>();

// ── MCP Server ────────────────────────────────────────────────────────────────
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "Karya", Version = "1.0.0" };
    })
    .WithHttpTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly()
    .WithPromptsFromAssembly();

var app = builder.Build();

// ── Startup Cache Warm (non-fatal) ────────────────────────────────────────────
// Server starts regardless; cache warms lazily on first tool call if this fails
try
{
    var apiService = app.Services.GetRequiredService<IPublicApiService>();
    await apiService.WarmCacheAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Startup cache warm failed — cache will populate on first use");
}

app.MapGet("/", () => Results.Ok(new
{
    name = "Karya MCP Server",
    status = "running",
    mcpEndpoint = "/mcp",
    message = "This is an MCP server endpoint, not a browser UI. Connect with an MCP client or send protocol requests to /mcp."
}));

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapMcp("/mcp");

app.Run();