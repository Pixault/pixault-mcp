using System.ComponentModel;
using System.Reflection;
using System.Text;
using ModelContextProtocol.Server;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class DocTools
{
    private static readonly Dictionary<string, string> TopicIndex = new(StringComparer.OrdinalIgnoreCase)
    {
        ["overview"] = "overview.md",
        ["quick-start"] = "quick-start.md",
        ["getting started"] = "quick-start.md",
        ["architecture"] = "architecture-overview.md",
        ["image delivery"] = "api-image-delivery.md",
        ["delivery api"] = "api-image-delivery.md",
        ["cdn"] = "api-image-delivery.md",
        ["upload"] = "api-upload.md",
        ["upload api"] = "api-upload.md",
        ["metadata"] = "api-metadata.md",
        ["metadata api"] = "api-metadata.md",
        ["schema.org"] = "api-metadata.md",
        ["json-ld"] = "api-metadata.md",
        ["transforms"] = "api-transforms.md",
        ["named transforms"] = "api-transforms.md",
        ["presets"] = "api-transforms.md",
        ["billing"] = "billing-and-plans.md",
        ["pricing"] = "billing-and-plans.md",
        ["plans"] = "billing-and-plans.md",
        ["dotnet"] = "sdk-dotnet.md",
        [".net"] = "sdk-dotnet.md",
        ["c#"] = "sdk-dotnet.md",
        ["javascript"] = "sdk-javascript.md",
        ["js"] = "sdk-javascript.md",
        ["typescript"] = "sdk-javascript.md",
        ["python"] = "sdk-python.md",
        ["php"] = "sdk-php.md",
        ["blazor"] = "integration-blazor.md",
        ["react"] = "integration-react.md",
        ["vue"] = "integration-vue.md",
        ["wordpress"] = "integration-wordpress.md",
        ["oqtane"] = "integration-oqtane.md",
        ["mcp"] = "integration-mcp.md",
        ["model context protocol"] = "integration-mcp.md",
        ["ai agent"] = "integration-ai-agent.md",
        ["chat"] = "integration-ai-agent.md",
        ["pixaultchat"] = "integration-ai-agent.md",
        ["plugins"] = "api-plugins.md",
        ["marketplace"] = "api-plugins.md",
        ["background removal"] = "api-plugins.md",
        ["smart crop"] = "api-plugins.md",
        ["filters"] = "api-plugins.md",
        ["watermark templates"] = "api-plugins.md"
    };

    [McpServerTool, Description(
        "Search Pixault documentation by topic. Returns the full content of the matching documentation page. " +
        "Available topics: overview, quick-start, architecture, image delivery, upload, metadata, transforms, " +
        "billing, plugins, dotnet, javascript, python, php, blazor, react, vue, wordpress, oqtane, mcp, ai agent.")]
    public static string SearchDocs(
        [Description("Topic to search for (e.g. 'upload', 'transforms', 'blazor', 'quick-start')")] string query)
    {
        // Try exact match first
        if (TopicIndex.TryGetValue(query, out var fileName))
            return LoadDocContent(fileName, query);

        // Try partial match
        var match = TopicIndex.Keys
            .FirstOrDefault(k => k.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return LoadDocContent(TopicIndex[match], query);

        // Try searching across all docs for the query string
        var results = SearchAllDocs(query);
        if (results.Count > 0)
            return FormatSearchResults(query, results);

        return $"No documentation found for '{query}'. Available topics:\n" +
               string.Join(", ", TopicIndex.Keys.Distinct().Order());
    }

    [McpServerTool, Description(
        "List all available Pixault documentation topics with brief descriptions.")]
    public static string ListDocTopics()
    {
        return """
            Pixault Documentation Topics:

            Getting Started:
            - overview: Product overview and key features
            - quick-start: Getting started guide with first upload
            - architecture: System architecture and design decisions

            API Reference:
            - api-image-delivery: Image transformation and CDN delivery endpoints
            - api-upload: File upload endpoint (images and videos)
            - api-metadata: Schema.org metadata management
            - api-transforms: Named transform presets

            SDKs:
            - sdk-dotnet: .NET/C# client SDK
            - sdk-javascript: JavaScript/TypeScript SDK
            - sdk-python: Python SDK
            - sdk-php: PHP SDK

            Integrations:
            - integration-blazor: Blazor component library
            - integration-react: React integration
            - integration-vue: Vue.js integration
            - integration-wordpress: WordPress plugin
            - integration-oqtane: Oqtane Blazor CMS module
            - integration-mcp: MCP server for AI assistants
            - integration-ai-agent: Embedded AI agent and PixaultChat

            Billing:
            - billing-and-plans: Pricing plans and usage billing
            """;
    }

    private static string LoadDocContent(string fileName, string query)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Pixault.Mcp.Resources.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return $"Documentation file '{fileName}' not found in embedded resources.";

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = reader.ReadToEnd();

        return $"# Documentation: {query}\n\n{content}";
    }

    private static List<(string topic, string snippet)> SearchAllDocs(string query)
    {
        var results = new List<(string topic, string snippet)>();
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var (topic, fileName) in TopicIndex.DistinctBy(kv => kv.Value))
        {
            var resourceName = $"Pixault.Mcp.Resources.{fileName}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) continue;

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = reader.ReadToEnd();

            if (!content.Contains(query, StringComparison.OrdinalIgnoreCase))
                continue;

            // Extract snippet around the match
            var idx = content.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            var start = Math.Max(0, idx - 100);
            var end = Math.Min(content.Length, idx + query.Length + 100);
            var snippet = content[start..end].ReplaceLineEndings(" ").Trim();

            if (start > 0) snippet = "..." + snippet;
            if (end < content.Length) snippet += "...";

            results.Add((topic, snippet));
        }

        return results;
    }

    private static string FormatSearchResults(string query, List<(string topic, string snippet)> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found '{query}' in {results.Count} topic(s):");
        sb.AppendLine();

        foreach (var (topic, snippet) in results)
        {
            sb.AppendLine($"## {topic}");
            sb.AppendLine(snippet);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
