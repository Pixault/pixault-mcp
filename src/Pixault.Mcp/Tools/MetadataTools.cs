using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class MetadataTools
{
    [McpServerTool, Description(
        "Get metadata for an image including dimensions, file size, Schema.org fields (name, description, keywords, author, copyright), and tags.")]
    public static async Task<string> GetImageMetadata(
        PixaultAdminClient client,
        [Description("The image ID to get metadata for")] string imageId)
    {
        var meta = await client.GetMetadataAsync(imageId);
        if (meta is null)
            return $"Image '{imageId}' not found.";

        var lines = new List<string>
        {
            $"Image: {meta.ImageId}",
            $"File: {meta.OriginalFileName}",
            $"Dimensions: {meta.Width}x{meta.Height}",
            $"Size: {meta.FormattedSize}",
            $"Type: {meta.ContentType}",
            $"Uploaded: {meta.UploadedAt:yyyy-MM-dd HH:mm:ss UTC}"
        };

        if (meta.IsVideo)
        {
            lines.Add($"Video: yes (duration: {meta.FormattedDuration})");
            if (meta.ThumbnailId is not null) lines.Add($"Thumbnail: {meta.ThumbnailId}");
        }

        if (meta.Name is not null) lines.Add($"Name: {meta.Name}");
        if (meta.Description is not null) lines.Add($"Description: {meta.Description}");
        if (meta.Caption is not null) lines.Add($"Caption: {meta.Caption}");
        if (meta.Category is not null) lines.Add($"Category: {meta.Category}");
        if (meta.Keywords is { Count: > 0 }) lines.Add($"Keywords: {string.Join(", ", meta.Keywords)}");
        if (meta.Author is not null) lines.Add($"Author: {meta.Author}");
        if (meta.CopyrightHolder is not null) lines.Add($"Copyright: {meta.CopyrightHolder} ({meta.CopyrightYear})");
        if (meta.License is not null) lines.Add($"License: {meta.License}");
        if (meta.Tags is { Count: > 0 }) lines.Add($"Tags: {string.Join(", ", meta.Tags.Select(t => $"{t.Key}={t.Value}"))}");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Update Schema.org metadata fields on an image. Supports name, description, caption, category, keywords, author, copyright, license, and custom tags.")]
    public static async Task<string> UpdateImageMetadata(
        PixaultAdminClient client,
        [Description("The image ID to update")] string imageId,
        [Description("Display name for the image")] string? name = null,
        [Description("Image description for SEO and accessibility")] string? description = null,
        [Description("Image caption")] string? caption = null,
        [Description("Category (e.g. 'product', 'hero', 'gallery')")] string? category = null,
        [Description("Comma-separated keywords")] string? keywords = null,
        [Description("Author name")] string? author = null,
        [Description("Copyright holder name")] string? copyrightHolder = null,
        [Description("Copyright year")] int? copyrightYear = null,
        [Description("License identifier (e.g. 'CC-BY-4.0')")] string? license = null)
    {
        var update = new MetadataUpdate
        {
            Name = name,
            Description = description,
            Caption = caption,
            Category = category,
            Keywords = keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Author = author,
            CopyrightHolder = copyrightHolder,
            CopyrightYear = copyrightYear,
            License = license
        };

        var result = await client.UpdateMetadataAsync(imageId, update);
        return result is not null
            ? $"Metadata updated for '{imageId}'."
            : $"Failed to update metadata for '{imageId}'.";
    }

    [McpServerTool, Description(
        "Get Schema.org JSON-LD structured data for an image. Useful for SEO and embedding in HTML pages.")]
    public static string GetJsonLdUrl(
        PixaultImageService imageService,
        [Description("Project identifier")] string project,
        [Description("Image ID")] string imageId)
    {
        // The JSON-LD endpoint follows the API pattern
        var baseUrl = imageService.For(project, imageId).Build();
        var apiBase = baseUrl.Split($"/{project}/")[0];
        return $"GET {apiBase}/api/{project}/{imageId}/metadata/jsonld";
    }
}
