# Karya

> **Karya** (Ka-arya / कार्य) — *work, service*
>
> A Service-as-a-Service MCP server that gives AI clients structured access to API discovery, architecture and framework guidance, built to make API research and integration planning a first-class, AI-native development workflow.

---

## What is Karya?

Karya is a [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server built in C#/.NET 10. It acts as an intelligent backend for AI clients (Claude, ChatGPT, or any MCP-compatible client) that need to:

- **Discover** public APIs by keyword, category, auth type, HTTPS, or CORS support
- **Research** existing APIs — documentation, auth setup, similar alternatives
- **Design** new APIs — architecture patterns, framework selection, DTOs, validation, mapping
- **Find** implementation resources — GitHub repos, NuGet, PyPI, npm, and Crates.io packages
- **Explore** 7,000+ OpenAPI specs from the APIs-guru directory

Instead of manually browsing docs and GitHub, an AI client can call Karya's tools directly and get structured, actionable responses in milliseconds.

---

## Capabilities

### 13 Tools

| Tool | Description |
|------|-------------|
| `search_public_apis` | Filter 1,500+ APIs by keyword, category, auth, HTTPS, CORS |
| `get_api_categories` | List all available API categories |
| `get_api_details` | Full API entry with auth setup hints |
| `suggest_similar_apis` | Keyword-scored recommendations based on a description |
| `scrape_api_documentation` | On-demand live scrape of an API documentation page |
| `find_implementation_resources` | GitHub repos + package registry results for any API and language |
| `get_api_design_guidance` | REST best practices from Stoplight, Microsoft Guidelines, and more |
| `get_architecture_patterns` | Curated resources for `clean`, `ddd`, `microservices`, `n-layer` |
| `get_code_generation_tools` | Code generators by language (NSwag, Kiota, OpenAPI Generator) |
| `get_validation_library` | Validation libraries by language (FluentValidation, Pydantic, etc.) |
| `get_mapping_library` | Mapping libraries by language (AutoMapper, MapStruct, etc.) |
| `get_framework_guide` | Framework guides by language (ASP.NET Core, FastAPI, NestJS, Spring) |
| `search_openapi_directory` | Search 7,000+ OpenAPI specs via APIs-guru |

### 7 Resources

| URI | Description |
|-----|-------------|
| `public-apis://categories` | All API categories as JSON |
| `public-apis://entry/{apiName}` | Single API entry by name |
| `public-apis://search/{query}` | Search results as JSON |
| `architecture://patterns/{type}` | Curated resources for a pattern type |
| `architecture://codegen/{language}` | Code generation tools for a language |
| `architecture://validation/{language}` | Validation libraries for a language |
| `architecture://frameworks/{language}` | Framework guides for a language |

### 4 Prompts

| Prompt | Description |
|--------|-------------|
| `api_implementation_guide` | Full integration guide for an API and language |
| `api_integration_quickstart` | Minimal copy-paste snippet to get started |
| `api_comparison` | Side-by-side comparison of multiple APIs |
| `api_architecture_design` | Recommends architecture pattern, framework, validation, and mapping stack for a new API |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10, ASP.NET Core |
| MCP SDK | `ModelContextProtocol` 1.2.0 + `ModelContextProtocol.AspNetCore` 1.2.0 |
| Transport | Stateless HTTP+SSE (`GET /sse` + `POST /message`) |
| Caching | `IMemoryCache` — 24h TTL, startup warm, SemaphoreSlim stampede guard |
| Resilience | Polly via `Microsoft.Extensions.Http.Resilience` — retry + circuit breaker |
| Scraping | AngleSharp 1.4.0 with per-host 1.5s rate limiting |
| Data Sources | GitHub README (1,500+ APIs), APIs-guru (7,000+ specs), GitHub Search API, NuGet/PyPI/npm/Crates.io |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- (Optional) Node.js — for MCP Inspector

### Run the server

```bash
git clone https://github.com/yourusername/Karya.git
cd Karya
dotnet run --project src/Karya.McpServer
```

Server starts at `http://localhost:5213`.

On startup, Karya automatically warms the in-memory cache by fetching the public-apis registry. If this fails (e.g., no internet), the server still starts and populates the cache lazily on the first tool call.

### Run tests

```bash
dotnet test
```

---

## Testing with MCP Inspector

The easiest way to explore all tools, resources, and prompts is the MCP Inspector — a browser-based UI for MCP servers:

```bash
npx @modelcontextprotocol/inspector http://localhost:5213/mcp
```

From the Inspector you can:
- Browse all 13 tools with their input schemas
- Call any tool and see the JSON response
- Read all 7 resources
- Preview all 4 prompts

---

## Connecting an AI Client

Karya uses standard HTTP+SSE MCP transport, compatible with any MCP client SDK across 9 languages (C#, Java, Python, JavaScript, TypeScript, Ruby, Rust, Go, Kotlin).

**Claude Desktop config (`claude_desktop_config.json`):**
```json
{
  "mcpServers": {
    "karya": {
      "url": "http://localhost:5213/mcp"
    }
  }
}
```

---

## Configuration

All settings live in `appsettings.json` under the `Karya` key:

```json
{
  "Karya": {
    "Cache": {
      "AllEntriesTtlHours": 24,
      "CategoriesTtlHours": 48,
      "GitHubSearchTtlMinutes": 120,
      "OpenApiDirectoryTtlHours": 12
    },
    "GitHub": {
      "Token": ""
    },
    "Scraper": {
      "PerHostDelayMs": 1500,
      "MaxCodeExamples": 5,
      "MaxCodeExampleLength": 1000
    }
  }
}
```

**GitHub Token** — optional but recommended. Without it, GitHub Search API requests are rate-limited to 10/min. Add a personal access token (no scopes needed) to increase this to 30/min.

---

## Project Structure

```
── Karya/
  ── src/
     ── Karya.McpServer/
         ── Program.cs               # DI wiring, HTTP clients, MCP server setup
         ── Models/                  # ApiEntry, ArchitectureResource, etc.
         ── Services/                # PublicApiService, WebScraperService,
                                     # ImplementationResourceService, ApiArchitectureService
         ── Tools/                   # 13 MCP tools across 5 files
         ── Resources/               # 7 MCP resources across 2 files
         ── Prompts/                 # 4 MCP prompts
         ── Infrastructure/          # CacheKeys, HttpClientNames, ArchitectureCatalog
  ── tests/
      ── Karya.McpServer.Tests/       # xUnit tests for services and tools
```

---

## Data Sources

| Source | What | Cache TTL |
|--------|------|-----------|
| [public-apis/public-apis](https://github.com/public-apis/public-apis) | 1,500+ public API entries parsed from README | 24 hours |
| [APIs-guru](https://apis.guru) | 7,000+ OpenAPI specs | 12 hours |
| [GitHub Search API](https://docs.github.com/en/rest/search) | Implementation repos per API + language | 2 hours |
| NuGet / PyPI / npm / Crates.io | Package results per API + language | 2 hours |

---

## License

MIT
