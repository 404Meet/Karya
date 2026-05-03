using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Karya.McpServer.Prompts;

[McpServerPromptType]
public static class ApiPrompts
{
    [McpServerPrompt(Name = "api_implementation_guide")]
    [Description("Generates a structured implementation guide for integrating a specific API in a chosen language")]
    public static IEnumerable<ChatMessage> ApiImplementationGuide(
        [Description("The API name, e.g. 'OpenWeatherMap', 'Stripe'")] string apiName,
        [Description("Target language: csharp, java, python, javascript, typescript, ruby, rust, go, kotlin")] string language,
        [Description("Optional: specific feature or endpoint to focus on")] string? focus)
    {
        var focusClause = string.IsNullOrWhiteSpace(focus) ? string.Empty : $"\nFocus specifically on: {focus}.";
        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Create a complete implementation guide for integrating the '{apiName}' API using {language}.{focusClause}

                The guide must include:
                1. Prerequisites and package/dependency installation
                2. Authentication setup with working code
                3. A minimal working end-to-end code example
                4. Error handling patterns specific to this API
                5. Rate limiting and quota best practices
                6. Links to the official SDK, NuGet/npm/pip/crates package if available
                7. Any known gotchas or common integration issues
                """),
            new ChatMessage(ChatRole.Assistant,
                $"I'll create a detailed {language} implementation guide for {apiName}, covering setup through production best practices.")
        ];
    }

    [McpServerPrompt(Name = "api_integration_quickstart")]
    [Description("Generates a minimal working code snippet for quick API integration in any of the 9 supported languages")]
    public static IEnumerable<ChatMessage> ApiIntegrationQuickstart(
        [Description("The API name, e.g. 'OpenWeatherMap'")] string apiName,
        [Description("Target language: csharp, java, python, javascript, typescript, ruby, rust, go, kotlin")] string language,
        [Description("The specific API operation, e.g. 'get current weather', 'send an SMS', 'search repositories'")] string operation)
    {
        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Write a minimal, copy-paste ready {language} snippet to {operation} using the '{apiName}' API.

                Requirements:
                - Include all necessary imports/using directives
                - Show how to authenticate (use placeholder YOUR_API_KEY where needed)
                - Include basic error handling
                - Add inline comments explaining each important step
                - Keep it under 60 lines
                - Make it runnable as a complete standalone example
                """)
        ];
    }

    [McpServerPrompt(Name = "api_comparison")]
    [Description("Generates a structured comparison of multiple APIs for the same use case")]
    public static IEnumerable<ChatMessage> ApiComparison(
        [Description("Comma-separated list of API names to compare, e.g. 'OpenWeatherMap,WeatherAPI,Tomorrow.io'")] string apiNames,
        [Description("The use case or criterion for comparison, e.g. 'weather data for mobile apps'")] string useCase,
        [Description("Optional: target language for code examples")] string? language)
    {
        var apis = apiNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var codeClause = string.IsNullOrWhiteSpace(language)
            ? string.Empty
            : $"\nInclude a minimal {language} code snippet for each API.";
        var headerRow = string.Join(" | ", apis);
        var separatorRow = string.Join("|", Enumerable.Repeat("---|", apis.Length));

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Compare these APIs for the use case: "{useCase}"

                APIs: {string.Join(", ", apis)}

                Provide a structured side-by-side comparison:

                | Criterion       | {headerRow} |
                |-----------------|{separatorRow}
                | Auth type       |   |
                | Free tier       |   |
                | Rate limits     |   |
                | HTTPS           |   |
                | CORS support    |   |
                | SDK quality     |   |
                | Documentation   |   |
                | Response speed  |   |
                | Data accuracy   |   |

                Then provide a clear recommendation with reasoning for the "{useCase}" use case.{codeClause}
                """)
        ];
    }

    [McpServerPrompt(Name = "api_architecture_design")]
    [Description("Recommends an architecture pattern, framework, validation and mapping stack for building a new API given a set of requirements")]
    public static IEnumerable<ChatMessage> ApiArchitectureDesign(
        [Description("Description of the API requirements, e.g. 'e-commerce checkout REST API with high traffic and complex business rules'")] string requirements,
        [Description("Target implementation language: csharp, java, python, typescript, go")] string language,
        [Description("Scale expectation: 'small', 'medium', 'large', 'enterprise'")] string? scale,
        [Description("Optional: specific constraints, e.g. 'must use microservices', 'prefer minimal dependencies'")] string? constraints)
    {
        var scaleClause = string.IsNullOrWhiteSpace(scale) ? "" : $"\nExpected scale: {scale}.";
        var constraintClause = string.IsNullOrWhiteSpace(constraints) ? "" : $"\nConstraints: {constraints}.";

        return
        [
            new ChatMessage(ChatRole.User,
                $"""
                Design the architecture for a new REST API with these requirements:
                {requirements}{scaleClause}{constraintClause}

                Implementation language: {language}

                Provide:
                1. **Architecture pattern recommendation** (Clean Architecture, DDD, N-Layer, Microservices, etc.) with justification
                2. **Framework recommendation** for {language} with setup steps
                3. **Project structure** — a directory tree showing layers, folders, and key files
                4. **Validation strategy** — recommended library and where validation should occur
                5. **Object mapping strategy** — recommended library or approach for DTO mapping
                6. **API specification** — recommended OpenAPI tooling (spec-first vs code-first)
                7. **Key security considerations** — authentication, authorization, input validation, rate limiting
                8. **Code generation** — tools to generate clients or server stubs from the spec
                9. **Reference implementation** — link to a production-quality example project

                Be specific and opinionated. Explain trade-offs only when there are meaningful alternatives.
                """),
            new ChatMessage(ChatRole.Assistant,
                $"I'll design a {language} API architecture for your requirements, covering pattern selection through security and code generation.")
        ];
    }
}
