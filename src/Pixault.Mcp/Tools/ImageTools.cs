using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class ImageTools
{
    [McpServerTool, Description(
        "Upload an image or video to Pixault. Accepts file path and uploads it to the specified project. " +
        "Returns the new image ID and CDN URL. Supported formats: JPEG, PNG, WebP, GIF, AVIF, SVG, MP4, WebM, MOV.")]
    public static async Task<string> UploadImage(
        PixaultUploadClient client,
        [Description("Project identifier (e.g. 'barber', 'tattoo')")] string project,
        [Description("Absolute path to the file to upload")] string filePath)
    {
        if (!File.Exists(filePath))
            return $"Error: File not found at '{filePath}'";

        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        await using var stream = File.OpenRead(filePath);
        var response = await client.UploadAsync(project, fileName, stream, contentType);

        return $"Uploaded successfully.\n- Image ID: {response.ImageId}\n- URL: {response.Url}";
    }

    [McpServerTool, Description(
        "List and search images for a project. Supports filtering by text search, category, keyword, author, and media type.")]
    public static async Task<string> ListImages(
        PixaultAdminClient client,
        [Description("Maximum number of images to return (default 20, max 50)")] int limit = 20,
        [Description("Pagination cursor from a previous response")] string? cursor = null,
        [Description("Free-text search across image name, filename, and ID")] string? search = null,
        [Description("Filter by category (exact match, case-insensitive)")] string? category = null,
        [Description("Filter by keyword tag (case-insensitive)")] string? keyword = null,
        [Description("Filter by author/creator name (case-insensitive)")] string? author = null,
        [Description("Filter to videos only (true) or images only (false)")] bool? isVideo = null)
    {
        var response = await client.ListImagesAsync(
            Math.Min(limit, 50), cursor,
            search: search, category: category, keyword: keyword, author: author, isVideo: isVideo);

        if (response.Images.Count == 0)
            return "No images found.";

        var lines = new List<string> { $"Found {response.TotalCount} images (showing {response.Images.Count}):", "" };

        foreach (var img in response.Images)
        {
            var badge = img.IsVideo ? " [VIDEO]" : img.IsSvg ? " [SVG]" : "";
            lines.Add($"- {img.ImageId}{badge}: {img.OriginalFileName} ({img.Width}x{img.Height}, {img.FormattedSize})");
        }

        if (response.NextCursor is not null)
            lines.Add($"\nNext cursor: {response.NextCursor}");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description("Delete an image and all its cached variants from Pixault.")]
    public static async Task<string> DeleteImage(
        PixaultAdminClient client,
        [Description("The image ID to delete (e.g. 'img_01JKABC')")] string imageId)
    {
        await client.DeleteImageAsync(imageId);
        return $"Image '{imageId}' deleted successfully.";
    }

    [McpServerTool, Description(
        "Generate a Pixault CDN URL for an image with transformations. " +
        "Supports width, height, fit mode, quality, blur, watermark, format, and named transforms.")]
    public static string BuildImageUrl(
        PixaultImageService imageService,
        [Description("Project identifier")] string project,
        [Description("Image ID")] string imageId,
        [Description("Target width in pixels")] int? width = null,
        [Description("Target height in pixels")] int? height = null,
        [Description("Fit mode: cover, contain, fill, pad")] string? fit = null,
        [Description("Quality 1-100 (default 85)")] int? quality = null,
        [Description("Blur radius 1-100")] int? blur = null,
        [Description("Output format: webp, jpeg, png, avif")] string? format = null,
        [Description("Named transform preset to apply")] string? transform = null)
    {
        var builder = imageService.For(project, imageId);

        if (transform is not null) builder.Transform(transform);
        if (width.HasValue) builder.Width(width.Value);
        if (height.HasValue) builder.Height(height.Value);
        if (fit is not null) builder.Fit(ParseFitMode(fit));
        if (quality.HasValue) builder.Quality(quality.Value);
        if (blur.HasValue) builder.Blur(blur.Value);
        if (format is not null) builder.Format(format);

        return builder.Build();
    }

    private static FitMode ParseFitMode(string fit) => fit.ToLowerInvariant() switch
    {
        "cover" => FitMode.Cover,
        "contain" => FitMode.Contain,
        "fill" => FitMode.Fill,
        "pad" => FitMode.Pad,
        _ => FitMode.Cover
    };

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".avif" => "image/avif",
            ".svg" => "image/svg+xml",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }
}
