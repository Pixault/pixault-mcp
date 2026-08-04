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

        if (meta.IsEps) lines.Add("EPS: yes (vector source — see EPS tools for derived assets)");
        if (meta.Folder is not null) lines.Add($"Folder: {meta.Folder}");
        if (meta.Name is not null) lines.Add($"Name: {meta.Name}");
        if (meta.Description is not null) lines.Add($"Description: {meta.Description}");
        if (meta.Caption is not null) lines.Add($"Caption: {meta.Caption}");
        if (meta.Category is not null) lines.Add($"Category: {meta.Category}");
        if (meta.Keywords is { Count: > 0 }) lines.Add($"Keywords: {string.Join(", ", meta.Keywords)}");
        if (meta.Author is not null) lines.Add($"Author: {meta.Author}");
        if (meta.CopyrightHolder is not null) lines.Add($"Copyright: {meta.CopyrightHolder} ({meta.CopyrightYear})");
        if (meta.License is not null) lines.Add($"License: {meta.License}");
        if (meta.DateCreated is { } dc) lines.Add($"Date Created: {dc:yyyy-MM-dd}");
        if (meta.DatePublished is { } dp) lines.Add($"Date Published: {dp:yyyy-MM-dd}");
        if (meta.RepresentativeOfPage == true) lines.Add("Representative of page: yes");
        if (meta.LocationName is not null) lines.Add($"Location: {meta.LocationName}");
        if (meta.LocationLatitude is { } lat && meta.LocationLongitude is { } lon)
            lines.Add($"Coordinates: {lat}, {lon}");
        if (meta.Tags is { Count: > 0 }) lines.Add($"Tags: {string.Join(", ", meta.Tags.Select(t => $"{t.Key}={t.Value}"))}");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Update Schema.org metadata fields on an image. Supports name, description, caption, category, folder, " +
        "keywords, author, copyright, license, creation/publish dates, geo-location, page-representative flag, " +
        "and custom tags. Only the fields you pass are changed.")]
    public static async Task<string> UpdateImageMetadata(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to update")] string imageId,
        [Description("Display name for the image")] string? name = null,
        [Description("Image description for SEO and accessibility")] string? description = null,
        [Description("Image caption")] string? caption = null,
        [Description("Category (e.g. 'product', 'hero', 'gallery')")] string? category = null,
        [Description("Folder path to file the image under (e.g. 'products/hero')")] string? folder = null,
        [Description("Comma-separated keywords")] string? keywords = null,
        [Description("Author name")] string? author = null,
        [Description("Copyright holder name")] string? copyrightHolder = null,
        [Description("Copyright year")] int? copyrightYear = null,
        [Description("License identifier (e.g. 'CC-BY-4.0')")] string? license = null,
        [Description("Creation date in ISO 8601 (e.g. '2026-01-15')")] string? dateCreated = null,
        [Description("Publish date in ISO 8601 (e.g. '2026-01-15')")] string? datePublished = null,
        [Description("Marks this image as representative of its page (Schema.org representativeOfPage)")] bool? representativeOfPage = null,
        [Description("Geo latitude where the image was taken")] double? locationLatitude = null,
        [Description("Geo longitude where the image was taken")] double? locationLongitude = null,
        [Description("Human-readable location name")] string? locationName = null,
        [Description("Comma-separated custom tags as key=value pairs (e.g. 'sku=A100,season=fall')")] string? tags = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        var update = new MetadataUpdate
        {
            Name = name,
            Description = description,
            Caption = caption,
            Category = category,
            Folder = folder,
            Keywords = keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Author = author,
            CopyrightHolder = copyrightHolder,
            CopyrightYear = copyrightYear,
            License = license,
            DateCreated = ParseDate(dateCreated),
            DatePublished = ParseDate(datePublished),
            RepresentativeOfPage = representativeOfPage,
            LocationLatitude = locationLatitude,
            LocationLongitude = locationLongitude,
            LocationName = locationName,
            Tags = ParseTags(tags)
        };

        var result = await client.UpdateMetadataAsync(imageId, update);
        return result is not null
            ? $"Metadata updated for '{imageId}'."
            : $"Failed to update metadata for '{imageId}'.";
    }

    [McpServerTool, Description(
        "Strip EXIF metadata (camera, GPS, timestamps) from an image's stored original. Useful for privacy " +
        "before publishing. Returns the updated metadata. This rewrites the original asset and cannot be undone.")]
    public static async Task<string> StripExif(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to strip EXIF data from")] string imageId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        var result = await client.StripExifAsync(imageId, project);
        return result is not null
            ? $"EXIF data stripped from '{imageId}'."
            : $"Failed to strip EXIF from '{imageId}'.";
    }

    [McpServerTool, Description(
        "Move an image into a folder (or to the project root). Metadata-only move — it re-files the image without " +
        "moving stored bytes or changing its delivery URL. Pass an empty string to move to the root.")]
    public static async Task<string> MoveImage(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to move")] string imageId,
        [Description("Destination folder path (e.g. 'products/hero'), or empty string for the root")] string folder)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        var result = await client.UpdateMetadataAsync(imageId, new MetadataUpdate { Folder = folder });
        return result is not null
            ? $"Moved '{imageId}' to {(string.IsNullOrEmpty(folder) ? "the root" : $"folder '{folder}'")}."
            : $"Failed to move '{imageId}'.";
    }

    [McpServerTool, Description(
        "Rename an image's display name (Schema.org name). NOTE: this changes the human-facing display name only — " +
        "it does NOT change the image's addressable publicId or its delivery URL. To change the URL slug, re-upload " +
        "the image with overwrite.")]
    public static async Task<string> RenameImage(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to rename")] string imageId,
        [Description("The new display name")] string name)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        var result = await client.UpdateMetadataAsync(imageId, new MetadataUpdate { Name = name });
        return result is not null
            ? $"Renamed '{imageId}' display name to '{name}'."
            : $"Failed to rename '{imageId}'.";
    }

    [McpServerTool, Description(
        "Add keyword tags to an image (Schema.org keywords). Appends to existing tags by default; set replace=true " +
        "to overwrite them entirely. Tags are searchable via list_images (keyword filter) and list_tags.")]
    public static async Task<string> TagImage(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to tag")] string imageId,
        [Description("Comma-separated keyword tags (e.g. 'summer,beach,promo')")] string tags,
        [Description("Replace all existing tags instead of appending")] bool replace = false)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        var incoming = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (incoming.Count == 0) return "No tags provided.";

        List<string> final;
        if (replace)
        {
            final = incoming;
        }
        else
        {
            var meta = await client.GetMetadataAsync(imageId);
            if (meta is null) return $"Image '{imageId}' not found.";
            final = (meta.Keywords ?? []).Concat(incoming)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        var result = await client.UpdateMetadataAsync(imageId, new MetadataUpdate { Keywords = final });
        return result is not null
            ? $"{(replace ? "Set" : "Added")} tags on '{imageId}'. Current tags: {string.Join(", ", final)}."
            : $"Failed to tag '{imageId}'.";
    }

    [McpServerTool, Description(
        "List all distinct keyword tags used across a project, with the number of images carrying each. Useful for " +
        "discovering the existing tag vocabulary before filtering or organizing.")]
    public static async Task<string> ListTags(
        PixaultAdminClient client,
        [Description("Project identifier (uses the default project if not specified)")] string? project = null)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await client.ListImagesAsync(limit: 100, cursor: cursor, project: project);
            foreach (var img in page.Images)
                if (img.Keywords is { Count: > 0 })
                    foreach (var k in img.Keywords)
                        counts[k] = counts.GetValueOrDefault(k) + 1;
            cursor = page.NextCursor;
        } while (cursor is not null && ++pages < 100);

        if (counts.Count == 0)
            return $"No tags found in project '{project ?? "(default)"}'.";

        var lines = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"  {kv.Key} ({kv.Value})");
        return $"Tags in project '{project ?? "(default)"}':\n" + string.Join("\n", lines);
    }

    private static DateTimeOffset? ParseDate(string? value) =>
        DateTimeOffset.TryParse(value, out var d) ? d : null;

    private static Dictionary<string, string>? ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return null;

        var result = new Dictionary<string, string>();
        foreach (var pair in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx > 0)
                result[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
        }
        return result.Count > 0 ? result : null;
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

    [McpServerTool, Description(
        "Fetch the Schema.org JSON-LD structured-data document for an image. Returns the JSON-LD body itself " +
        "(ready to embed in a <script type=\"application/ld+json\"> tag), not just the endpoint URL.")]
    public static async Task<string> GetJsonLd(
        IHttpClientFactory httpFactory,
        [Description("Project identifier")] string project,
        [Description("Image ID")] string imageId)
    {
        var http = httpFactory.CreateClient("PixaultApi");
        var response = await http.GetAsync($"api/{project}/{imageId}/metadata/jsonld");
        if (!response.IsSuccessStatusCode)
            return $"Failed to fetch JSON-LD for '{imageId}' ({(int)response.StatusCode} {response.ReasonPhrase}).";

        return await response.Content.ReadAsStringAsync();
    }
}
