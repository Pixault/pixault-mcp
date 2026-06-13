using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp;

/// <summary>
/// MCP resources — read-only data the host can browse and attach to context directly,
/// complementing the model-invoked read tools. Documentation pages are served from the
/// embedded <c>Resources/*.md</c> files; image metadata is fetched live via the admin client.
/// </summary>
[McpServerResourceType]
public sealed class PixaultResources
{
    private const string ResourcePrefix = "Pixault.Mcp.Resources.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>An index of every documentation topic, each readable at <c>pixault://docs/{topic}</c>.</summary>
    [McpServerResource(UriTemplate = "pixault://docs", Name = "Pixault documentation index", MimeType = "text/markdown")]
    [Description("Index of all bundled Pixault documentation topics. Read pixault://docs/{topic} for a full page.")]
    public static TextResourceContents DocsIndex()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Pixault documentation");
        sb.AppendLine();
        sb.AppendLine("Read any topic below as `pixault://docs/{topic}`:");
        sb.AppendLine();
        foreach (var topic in DocTopics().Order())
            sb.AppendLine($"- `{topic}`");

        return Markdown("pixault://docs", sb.ToString());
    }

    /// <summary>A single documentation page by topic (the embedded markdown file's name without extension).</summary>
    [McpServerResource(UriTemplate = "pixault://docs/{topic}", Name = "Pixault documentation page", MimeType = "text/markdown")]
    [Description("Full content of a Pixault documentation page by topic (e.g. 'quick-start', 'api-upload', 'sdk-dotnet').")]
    public static TextResourceContents Doc(
        [Description("Documentation topic — a file name from the docs index, e.g. 'quick-start'")] string topic)
    {
        var content = LoadDoc(topic);
        if (content is null)
            throw new McpException($"Documentation topic '{topic}' not found. See pixault://docs for the list.");

        return Markdown($"pixault://docs/{topic}", content);
    }

    /// <summary>Schema.org metadata for an image, as JSON.</summary>
    [McpServerResource(UriTemplate = "pixault://{project}/{imageId}/metadata", Name = "Image metadata", MimeType = "application/json")]
    [Description("Schema.org metadata, dimensions, geo-location, folder, and tags for an image, as JSON.")]
    public static async Task<TextResourceContents> ImageMetadata(
        PixaultAdminClient client,
        [Description("Project identifier")] string project,
        [Description("Image ID")] string imageId)
    {
        var meta = await client.GetMetadataAsync(imageId, project);
        if (meta is null)
            throw new McpException($"Image '{imageId}' not found in project '{project}'.");

        return new TextResourceContents
        {
            Uri = $"pixault://{project}/{imageId}/metadata",
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(meta, JsonOptions)
        };
    }

    // ── Embedded-doc helpers ─────────────────────────────────────

    private static IEnumerable<string> DocTopics() =>
        Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".md", StringComparison.Ordinal))
            .Select(n => n[ResourcePrefix.Length..^".md".Length]);

    private static string? LoadDoc(string topic)
    {
        var resourceName = $"{ResourcePrefix}{topic}.md";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static TextResourceContents Markdown(string uri, string text) => new()
    {
        Uri = uri,
        MimeType = "text/markdown",
        Text = text
    };
}
