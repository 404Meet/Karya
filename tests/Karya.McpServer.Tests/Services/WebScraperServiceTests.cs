using System.Net;
using Karya.McpServer.Infrastructure;
using Karya.McpServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace Karya.McpServer.Tests.Services;

public sealed class WebScraperServiceTests
{
    private static readonly string SampleHtml = """
        <!DOCTYPE html>
        <html>
        <head><title>Dog Facts API Documentation</title></head>
        <body>
          <main>
            <h1>Dog Facts API</h1>
            <p>This is a comprehensive API for retrieving random dog facts. Use it to build fun apps.</p>
            <h2>Authentication</h2>
            <p>Pass your API Key as the Authorization: Bearer header.</p>
            <h2>Endpoints</h2>
            <p>GET /v1/facts - Returns a random dog fact</p>
            <p>GET /v1/breeds - Returns all dog breeds</p>
            <pre>
        curl -H "Authorization: Bearer YOUR_API_KEY" https://dogapi.dog/api/v1/facts
            </pre>
          </main>
        </body>
        </html>
        """;

    private IWebScraperService CreateService(MockHttpMessageHandler handler)
    {
        var client = handler.ToHttpClient();
        var mockFactory = new SingleClientFactory(client);
        return new WebScraperService(mockFactory, NullLogger<WebScraperService>.Instance);
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsDocumentation_OnSuccess()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When("https://dogapi.dog/docs")
            .Respond("text/html", SampleHtml);

        var service = CreateService(mock);
        var result = await service.ScrapeAsync("https://dogapi.dog/docs");

        Assert.NotNull(result);
        Assert.Equal("https://dogapi.dog/docs", result.Url);
        Assert.Contains("Dog Facts", result.Title);
        Assert.NotEmpty(result.Summary);
    }

    [Fact]
    public async Task ScrapeAsync_ExtractsAuthMethods()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When("https://dogapi.dog/docs")
            .Respond("text/html", SampleHtml);

        var service = CreateService(mock);
        var result = await service.ScrapeAsync("https://dogapi.dog/docs");

        Assert.NotNull(result);
        Assert.Contains("Bearer", result.AuthMethods);
    }

    [Fact]
    public async Task ScrapeAsync_ExtractsCodeExamples()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When("https://dogapi.dog/docs")
            .Respond("text/html", SampleHtml);

        var service = CreateService(mock);
        var result = await service.ScrapeAsync("https://dogapi.dog/docs");

        Assert.NotNull(result);
        Assert.NotEmpty(result.CodeExamples);
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsNull_OnHttpError()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When("https://example.com/notfound")
            .Respond(HttpStatusCode.NotFound);

        var service = CreateService(mock);
        var result = await service.ScrapeAsync("https://example.com/notfound");

        Assert.Null(result);
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsNull_OnNonHtmlContent()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When("https://example.com/api.json")
            .Respond("application/json", """{"key": "value"}""");

        var service = CreateService(mock);
        var result = await service.ScrapeAsync("https://example.com/api.json");

        Assert.Null(result);
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsNull_OnInvalidUrl()
    {
        using var mock = new MockHttpMessageHandler();
        var service = CreateService(mock);
        var result = await service.ScrapeAsync("not-a-valid-url");

        Assert.Null(result);
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
