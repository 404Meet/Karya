using System.Net;
using Karya.McpServer.Models;
using Karya.McpServer.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace Karya.McpServer.Tests.Services;

public sealed class PublicApiServiceTests : IDisposable
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1_000_000 });

    // Minimal README markdown matching the real format
    private const string SampleMarkdown = """
        # Public APIs

        ### Animals
        API | Description | Auth | HTTPS | CORS
        |:---|:---|:---|:---|:---|
        | [Dog Facts](https://dogapi.dog/) | Random dog facts | `apiKey` | Yes | Yes |
        | [Axolotl](https://theaxolotlapi.netlify.app/) | Axolotl pictures | No | Yes | No |

        ### Weather
        API | Description | Auth | HTTPS | CORS
        |:---|:---|:---|:---|:---|
        | [OpenWeatherMap](https://openweathermap.org/api) | Weather data | `apiKey` | Yes | No |
        """;

    private const string ReadmeUrl =
        "https://raw.githubusercontent.com/public-apis/public-apis/master/README.md";

    private IPublicApiService CreateService(MockHttpMessageHandler handler)
    {
        var client = handler.ToHttpClient();
        var mockFactory = new SingleClientFactory(client);
        return new PublicApiService(mockFactory, _cache, NullLogger<PublicApiService>.Instance);
    }

    [Fact]
    public async Task GetAllEntriesAsync_ReturnsEntries_OnSuccess()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var result = await service.GetAllEntriesAsync();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.API == "Dog Facts");
    }

    [Fact]
    public async Task GetAllEntriesAsync_UsesCacheOnSecondCall()
    {
        using var mock = new MockHttpMessageHandler();
        int callCount = 0;
        mock.When(ReadmeUrl).Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleMarkdown, System.Text.Encoding.UTF8, "text/plain")
            };
        });

        var service = CreateService(mock);
        await service.GetAllEntriesAsync();
        await service.GetAllEntriesAsync(); // second call should hit cache

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SearchAsync_FiltersByKeyword()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var result = await service.SearchAsync("dog", null, null, null, null);

        Assert.Single(result);
        Assert.Equal("Dog Facts", result[0].API);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCategory()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var result = await service.SearchAsync(null, "Weather", null, null, null);

        Assert.Single(result);
        Assert.Equal("Weather", result[0].Category);
    }

    [Fact]
    public async Task SearchAsync_FiltersByHttpsOnly()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var result = await service.SearchAsync(null, null, null, true, null);

        Assert.Equal(3, result.Count); // all three have HTTPS=Yes in sample
    }

    [Fact]
    public async Task SearchAsync_FiltersByCors()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var result = await service.SearchAsync(null, null, null, null, "yes");

        Assert.Single(result);
        Assert.Equal("Dog Facts", result[0].API);
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsDistinctSortedCategories()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var categories = await service.GetCategoriesAsync();

        Assert.Equal(2, categories.Count);
        Assert.Contains("Animals", categories);
        Assert.Contains("Weather", categories);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsEntry_CaseInsensitive()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var entry = await service.GetByNameAsync("openweathermap");

        Assert.NotNull(entry);
        Assert.Equal("OpenWeatherMap", entry.API);
    }

    [Fact]
    public async Task GetByNameAsync_ReturnsNull_WhenNotFound()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var entry = await service.GetByNameAsync("nonexistent api xyz");

        Assert.Null(entry);
    }

    [Fact]
    public async Task ParseReadme_ExtractsAuthCorrectly()
    {
        using var mock = new MockHttpMessageHandler();
        mock.When(ReadmeUrl).Respond("text/plain", SampleMarkdown);

        var service = CreateService(mock);
        var all = await service.GetAllEntriesAsync();

        var dogFacts = all.First(e => e.API == "Dog Facts");
        var axolotl = all.First(e => e.API == "Axolotl");

        Assert.Equal("apiKey", dogFacts.Auth);
        Assert.Equal("", axolotl.Auth); // "No" maps to empty string
    }

    [Fact]
    public async Task InvalidateCache_ForcesNewHttpFetchOnNextCall()
    {
        using var mock = new MockHttpMessageHandler();
        int callCount = 0;
        mock.When(ReadmeUrl).Respond(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleMarkdown, System.Text.Encoding.UTF8, "text/plain")
            };
        });

        var service = CreateService(mock);
        await service.GetAllEntriesAsync(); // populates cache
        service.InvalidateCache();
        await service.GetAllEntriesAsync(); // should make a new HTTP request

        Assert.Equal(2, callCount);
    }

    public void Dispose() => _cache.Dispose();

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }
}
