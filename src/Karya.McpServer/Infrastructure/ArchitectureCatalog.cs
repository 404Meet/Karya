using Karya.McpServer.Models;

namespace Karya.McpServer.Infrastructure;

internal static class ArchitectureCatalog
{
    private static readonly IReadOnlyList<ArchitectureResource> _all = new List<ArchitectureResource>
    {
        // ── API Design & Best Practices ──────────────────────────────────────
        new(
            Name: "Stoplight API Design Guide",
            Url: "https://docs.stoplight.io/docs/api-best-practices/design/get-started-with-design",
            Description: "Foundational guide for API-first design principles: defining resources, HTTP verbs, status codes, and iterating on contracts before writing code.",
            Category: ArchitectureCategory.DesignBestPractices,
            Languages: ["all"],
            Tags: ["rest", "design", "api-first", "openapi", "stoplight"]
        ),
        new(
            Name: "Microsoft REST API Guidelines",
            Url: "https://github.com/microsoft/api-guidelines",
            Description: "Microsoft's comprehensive REST API design guidelines covering naming, versioning, error formats, pagination, and long-running operations.",
            Category: ArchitectureCategory.DesignBestPractices,
            Languages: ["all"],
            Tags: ["rest", "guidelines", "microsoft", "versioning", "pagination"]
        ),
        new(
            Name: "Code With Engineering Playbook – REST API Design Guidance",
            Url: "https://microsoft.github.io/code-with-engineering-playbook/design/design-patterns/rest-api-design-guidance/",
            Description: "Practical REST API design patterns from Microsoft engineering: URL structure, HTTP methods, versioning, error handling, design-first vs code-first.",
            Category: ArchitectureCategory.DesignBestPractices,
            Languages: ["all"],
            Tags: ["rest", "versioning", "error-handling", "design-first", "code-first", "microsoft"]
        ),

        // ── Architecture Patterns ────────────────────────────────────────────
        new(
            Name: "eShopOnWeb – .NET Reference Application",
            Url: "https://github.com/dotnet-architecture/eShopOnWeb",
            Description: "Microsoft reference application demonstrating Clean Architecture and Domain-Driven Design with ASP.NET Core 8.",
            Category: ArchitectureCategory.ArchitecturePatterns,
            Languages: ["csharp"],
            Tags: ["clean-architecture", "ddd", "aspnetcore", "ef-core", "cqrs", "reference"]
        ),
        new(
            Name: "Common Web Application Architectures (.NET)",
            Url: "https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures",
            Description: "Microsoft guide covering monolithic, N-Layer, and Clean Architecture patterns for ASP.NET Core web applications.",
            Category: ArchitectureCategory.ArchitecturePatterns,
            Languages: ["csharp"],
            Tags: ["clean-architecture", "n-layer", "monolith", "aspnetcore", "microsoft"]
        ),
        new(
            Name: "DDD-Oriented Microservice Design (.NET)",
            Url: "https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice",
            Description: "Deep-dive guide on designing DDD-oriented microservices with bounded contexts, aggregates, and CQRS patterns in .NET.",
            Category: ArchitectureCategory.ArchitecturePatterns,
            Languages: ["csharp"],
            Tags: ["ddd", "microservices", "cqrs", "bounded-context", "aggregates", "aspnetcore"]
        ),
        new(
            Name: "Clean Architecture Solution Template (Jason Taylor)",
            Url: "https://github.com/jasontaylordev/cleanarchitecture",
            Description: "Production-ready enterprise solution template implementing Clean Architecture with MediatR, AutoMapper, and FluentValidation on ASP.NET Core 10.",
            Category: ArchitectureCategory.ArchitecturePatterns,
            Languages: ["csharp"],
            Tags: ["clean-architecture", "mediatr", "automapper", "fluentvalidation", "cqrs", "template"]
        ),

        // ── OpenAPI Specification ────────────────────────────────────────────
        new(
            Name: "OpenAPI Specification 3.2.0",
            Url: "https://spec.openapis.org/oas/v3.2.0.html",
            Description: "The official authoritative OpenAPI 3.2.0 specification defining the standard format for describing REST APIs in JSON or YAML.",
            Category: ArchitectureCategory.OpenApiSpec,
            Languages: ["all"],
            Tags: ["openapi", "specification", "swagger", "yaml", "json-schema"]
        ),
        new(
            Name: "Swagger / OpenAPI Specification Overview",
            Url: "https://swagger.io/specification/",
            Description: "Swagger.io overview of the OpenAPI specification (OAS 3.1.1), including the standard structure for paths, components, security, and servers.",
            Category: ArchitectureCategory.OpenApiSpec,
            Languages: ["all"],
            Tags: ["openapi", "swagger", "specification", "rest"]
        ),

        // ── Code Generation ──────────────────────────────────────────────────
        new(
            Name: "OpenAPI Generator",
            Url: "https://openapi-generator.tech/",
            Description: "Generates client SDKs (50+ languages) and server stubs (40+ frameworks) from an OpenAPI 2.0 or 3.x specification.",
            Category: ArchitectureCategory.CodeGeneration,
            Languages: ["all"],
            Tags: ["codegen", "client-sdk", "server-stub", "openapi", "cli"]
        ),
        new(
            Name: "NSwag – .NET/TypeScript Code Generator",
            Url: "https://github.com/RicoSuter/NSwag/",
            Description: "Toolchain for generating C# and TypeScript API clients and ASP.NET Core controllers from an OpenAPI specification. Integrates with MSBuild and CLI.",
            Category: ArchitectureCategory.CodeGeneration,
            Languages: ["csharp", "typescript"],
            Tags: ["codegen", "client-sdk", "aspnetcore", "nswag", "msbuild"]
        ),
        new(
            Name: "Swagger Codegen",
            Url: "https://swagger.io/tools/swagger-codegen/",
            Description: "Generates server stubs and client SDKs from OpenAPI specifications across 40+ client languages and 20+ server frameworks.",
            Category: ArchitectureCategory.CodeGeneration,
            Languages: ["all"],
            Tags: ["codegen", "client-sdk", "server-stub", "openapi", "swagger"]
        ),
        new(
            Name: "Kiota – Microsoft API Client Generator",
            Url: "https://learn.microsoft.com/en-us/openapi/kiota/overview",
            Description: "Microsoft CLI tool for generating strongly-typed API clients from OpenAPI specs in C#, Go, Java, PHP, Python, Ruby, and TypeScript.",
            Category: ArchitectureCategory.CodeGeneration,
            Languages: ["csharp", "go", "java", "php", "python", "ruby", "typescript"],
            Tags: ["codegen", "client-sdk", "microsoft", "kiota", "strongly-typed"]
        ),

        // ── Validation Libraries ─────────────────────────────────────────────
        new(
            Name: "FluentValidation (.NET)",
            Url: "https://docs.fluentvalidation.net/en/latest/",
            Description: "Popular .NET library for building strongly-typed, fluent validation rules. Integrates with ASP.NET Core, MediatR, and dependency injection.",
            Category: ArchitectureCategory.Validation,
            Languages: ["csharp"],
            Tags: ["validation", "fluent", "dotnet", "aspnetcore", "rules"]
        ),
        new(
            Name: "Pydantic (Python)",
            Url: "https://pydantic.dev/docs/validation/latest/get-started/",
            Description: "Python data validation library using type hints. Core logic written in Rust for performance. Used by FastAPI for request/response validation.",
            Category: ArchitectureCategory.Validation,
            Languages: ["python"],
            Tags: ["validation", "python", "type-hints", "fastapi", "pydantic"]
        ),

        // ── Object Mapping Libraries ─────────────────────────────────────────
        new(
            Name: "AutoMapper (.NET)",
            Url: "https://automapper.io/",
            Description: "Convention-based object-to-object mapper for .NET 8+. Eliminates boilerplate DTO mapping code. Supports flattening, projections, and custom resolvers.",
            Category: ArchitectureCategory.Mapping,
            Languages: ["csharp"],
            Tags: ["mapping", "dto", "automapper", "dotnet", "conventions"]
        ),
        new(
            Name: "MapStruct (Java)",
            Url: "https://mapstruct.org/documentation/stable/reference/html/",
            Description: "Java annotation processor for generating compile-time bean mappers. Zero reflection overhead. Supports Maven, Gradle, and Spring injection.",
            Category: ArchitectureCategory.Mapping,
            Languages: ["java"],
            Tags: ["mapping", "dto", "java", "annotation-processor", "compile-time"]
        ),

        // ── Framework Guides ─────────────────────────────────────────────────
        new(
            Name: "ASP.NET Core Web API Tutorial",
            Url: "https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-10.0&tabs=visual-studio",
            Description: "Official step-by-step tutorial for building a CRUD REST API with ASP.NET Core 10, Entity Framework Core, and Swagger/OpenAPI.",
            Category: ArchitectureCategory.FrameworkGuide,
            Languages: ["csharp"],
            Tags: ["aspnetcore", "crud", "ef-core", "swagger", "rest", "tutorial"]
        ),
        new(
            Name: "Spring REST Tutorial",
            Url: "https://spring.io/guides/tutorials/rest",
            Description: "Official Spring Framework tutorial for building RESTful services with Spring Boot, covering hypermedia (HATEOAS) and content negotiation.",
            Category: ArchitectureCategory.FrameworkGuide,
            Languages: ["java"],
            Tags: ["spring", "spring-boot", "rest", "hateoas", "java", "tutorial"]
        ),
        new(
            Name: "FastAPI – Bigger Applications Guide",
            Url: "https://fastapi.tiangolo.com/tutorial/bigger-applications/",
            Description: "FastAPI guide for structuring larger Python applications using APIRouter, modular file layout, and shared dependencies across multiple modules.",
            Category: ArchitectureCategory.FrameworkGuide,
            Languages: ["python"],
            Tags: ["fastapi", "python", "modular", "apirouter", "async", "tutorial"]
        ),
        new(
            Name: "NestJS Modules Documentation",
            Url: "https://docs.nestjs.com/modules",
            Description: "NestJS official documentation on the module system: feature modules, shared modules, global modules, and dynamic modules for TypeScript APIs.",
            Category: ArchitectureCategory.FrameworkGuide,
            Languages: ["typescript"],
            Tags: ["nestjs", "typescript", "modules", "nodejs", "dependency-injection"]
        ),

        // ── API Directories ──────────────────────────────────────────────────
        new(
            Name: "RapidAPI Hub",
            Url: "https://docs.rapidapi.com/",
            Description: "API marketplace platform supporting REST and GraphQL. Provides API discovery, testing, monetization, and developer portal features.",
            Category: ArchitectureCategory.ApiDirectory,
            Languages: ["all"],
            Tags: ["discovery", "marketplace", "testing", "monetization", "rapidapi"]
        ),
        new(
            Name: "APIs-guru OpenAPI Directory",
            Url: "https://github.com/APIs-guru/openapi-directory",
            Description: "Community-maintained directory of 7000+ REST APIs with OpenAPI specifications. Programmatic access via https://api.apis.guru/v2/list.json.",
            Category: ArchitectureCategory.ApiDirectory,
            Languages: ["all"],
            Tags: ["openapi", "directory", "specifications", "community", "7000+"]
        ),
        new(
            Name: "GitHub OpenAPI Topic",
            Url: "https://github.com/topics/openapi",
            Description: "GitHub topic aggregating 7,998+ public repositories tagged with OpenAPI — tools, generators, validators, and framework integrations.",
            Category: ArchitectureCategory.ApiDirectory,
            Languages: ["all"],
            Tags: ["openapi", "github", "community", "tools", "open-source"]
        ),
        new(
            Name: "GitHub REST API Documentation",
            Url: "https://docs.github.com/en/rest",
            Description: "Comprehensive GitHub REST API reference covering authentication, rate limits, pagination, webhooks, and all resource endpoints.",
            Category: ArchitectureCategory.ApiDirectory,
            Languages: ["all"],
            Tags: ["github", "rest", "reference", "authentication", "rate-limiting"]
        ),
    };

    public static IReadOnlyList<ArchitectureResource> All => _all;

    public static IReadOnlyList<ArchitectureResource> ByCategory(ArchitectureCategory category) =>
        _all.Where(r => r.Category == category).ToList();

    public static IReadOnlyList<ArchitectureResource> ByLanguage(string language) =>
        _all.Where(r =>
            r.Languages.Contains("all", StringComparer.OrdinalIgnoreCase) ||
            r.Languages.Contains(language, StringComparer.OrdinalIgnoreCase))
        .ToList();

    public static IReadOnlyList<ArchitectureResource> ByTag(string tag) =>
        _all.Where(r => r.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<ArchitectureResource> Search(string keyword) =>
        _all.Where(r =>
            r.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            r.Tags.Any(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        .ToList();
}
