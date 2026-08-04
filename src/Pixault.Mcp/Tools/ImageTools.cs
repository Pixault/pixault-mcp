using System.ComponentModel;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class ImageTools
{
    [McpServerTool, Description(
        "Download a zip archive of one or more images' original files to a local path UNDER the current working " +
        "directory. Provide comma-separated image IDs; the archive is written to the given path (default " +
        "./pixault-archive.zip) and its full path + size are returned. Requires write permission (PIXAULT_ALLOW_WRITE) " +
        "since it exports original bytes and writes to disk.")]
    public static async Task<string> GenerateArchive(
        IHttpClientFactory httpFactory,
        IOptions<PixaultOptions> pixault,
        AgentScope scope,
        [Description("Comma-separated image IDs to include in the archive")] string imageIds,
        [Description("Relative .zip path under the working directory (default: ./pixault-archive.zip)")] string? outputPath = null,
        [Description("Project identifier (uses the default project if not specified)")] string? project = null)
    {
        // Bulk-exports original bytes to disk — gate behind write permission.
        if (scope.CheckWrite() is { } denied) return denied;

        var ids = imageIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (ids.Count == 0) return "No image IDs provided.";

        // Validate the project is a plain slug before interpolating it into the request URL.
        var proj = project ?? pixault.Value.DefaultProject;
        if (string.IsNullOrEmpty(proj)) return "No project specified and no default project is configured.";
        if (!IsSafeSlug(proj)) return $"Invalid project identifier '{proj}' (allowed: letters, digits, '-', '_').";

        // Confine the output to the working directory: block absolute paths, traversal, and non-.zip targets.
        var requested = string.IsNullOrWhiteSpace(outputPath) ? "pixault-archive.zip" : outputPath!;
        var cwd = Path.GetFullPath(Directory.GetCurrentDirectory());
        var fullPath = Path.GetFullPath(Path.Combine(cwd, requested));
        if (!fullPath.StartsWith(cwd + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            return "Refusing to write outside the working directory; provide a relative path under it.";
        if (!fullPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return "Output path must end in '.zip'.";

        var http = httpFactory.CreateClient("PixaultApi");
        using var resp = await http.PostAsJsonAsync($"api/{Uri.EscapeDataString(proj)}/archive",
            new { imageIds = ids, fileName = Path.GetFileName(fullPath) });
        if (!resp.IsSuccessStatusCode)
            return $"Archive failed ({(int)resp.StatusCode} {resp.ReasonPhrase}): {await resp.Content.ReadAsStringAsync()}";

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = File.Create(fullPath))
            await resp.Content.CopyToAsync(fs);

        var size = new FileInfo(fullPath).Length;
        return $"Archived {ids.Count} image(s) to '{fullPath}' ({size:N0} bytes).";
    }

    private static bool IsSafeSlug(string s) =>
        s.Length is > 0 and <= 128 && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    [McpServerTool, Description(
        "Upload an image or video to Pixault. Accepts file path and uploads it to the specified project. " +
        "Returns the new image ID and CDN URL. Supported formats: JPEG, PNG, WebP, GIF, AVIF, SVG, MP4, WebM, MOV.")]
    public static async Task<string> UploadImage(
        PixaultUploadClient client,
        AgentScope scope,
        [Description("Project identifier (e.g. 'barber', 'tattoo')")] string project,
        [Description("Absolute path to the file to upload")] string filePath)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        if (!File.Exists(filePath))
            return $"Error: File not found at '{filePath}'";

        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        await using var stream = File.OpenRead(filePath);
        var response = await client.UploadAsync(project, fileName, stream, contentType);

        return $"Uploaded successfully.\n- Image ID: {response.ImageId}\n- URL: {response.Url}";
    }

    [McpServerTool, Description(
        "List and search images for a project. Supports filtering by text search, category, keyword, author, folder, and media type.")]
    public static async Task<string> ListImages(
        PixaultAdminClient client,
        [Description("Maximum number of images to return (default 20, max 50)")] int limit = 20,
        [Description("Pagination cursor from a previous response")] string? cursor = null,
        [Description("Free-text search across image name, filename, and ID")] string? search = null,
        [Description("Filter by category (exact match, case-insensitive)")] string? category = null,
        [Description("Filter by keyword tag (case-insensitive)")] string? keyword = null,
        [Description("Filter by author/creator name (case-insensitive)")] string? author = null,
        [Description("Filter to images in a specific folder (e.g. 'products/hero')")] string? folder = null,
        [Description("Filter to videos only (true) or images only (false)")] bool? isVideo = null,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var response = await client.ListImagesAsync(
            Math.Min(limit, 50), cursor, project: project,
            search: search, category: category, keyword: keyword, author: author, isVideo: isVideo, folder: folder);

        if (response.Images.Count == 0)
            return "No images found.";

        var lines = new List<string> { $"Found {response.TotalCount} images (showing {response.Images.Count}):", "" };

        foreach (var img in response.Images)
        {
            var badge = img.IsVideo ? " [VIDEO]" : img.IsSvg ? " [SVG]" : img.IsEps ? " [EPS]" : "";
            var loc = img.Folder is not null ? $" @{img.Folder}" : "";
            lines.Add($"- {img.ImageId}{badge}: {img.OriginalFileName} ({img.Width}x{img.Height}, {img.FormattedSize}){loc}");
        }

        if (response.NextCursor is not null)
            lines.Add($"\nNext cursor: {response.NextCursor}");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description("Delete an image and all its cached variants from Pixault.")]
    public static async Task<string> DeleteImage(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The image ID to delete (e.g. 'img_01JKABC')")] string imageId)
    {
        if (scope.CheckDelete() is { } denied) return denied;

        await client.DeleteImageAsync(imageId);
        return $"Image '{imageId}' deleted successfully.";
    }

    [McpServerTool, Description(
        "Bulk-delete images in a project. Select targets by explicit image IDs, by original filenames, or by a " +
        "filter (folder / category / text search / media type). SAFETY: defaults to a DRY RUN that only reports " +
        "what would be deleted — pass confirm=true to actually delete. Deletion is permanent. Requires delete permission.")]
    public static async Task<string> DeleteImages(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("Set true to actually delete; false (default) previews what would be deleted. Always preview first.")] bool confirm = false,
        [Description("Explicit image IDs to delete, comma-separated")] string? imageIds = null,
        [Description("Original filenames to delete, comma-separated (resolved to images in the project)")] string? fileNames = null,
        [Description("Delete every image in this folder")] string? folder = null,
        [Description("Delete images matching this free-text search (name/filename/id)")] string? search = null,
        [Description("Delete images in this category (exact, case-insensitive)")] string? category = null,
        [Description("Limit to videos (true) or images (false)")] bool? isVideo = null,
        [Description("Safety cap: refuse if more than this many images match (default 500)")] int max = 500,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckDelete() is { } denied) return denied;

        var targets = new Dictionary<string, string>(StringComparer.Ordinal); // imageId -> filename

        if (!string.IsNullOrWhiteSpace(imageIds))
            foreach (var id in imageIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                targets[id] = "";

        var wantFiles = string.IsNullOrWhiteSpace(fileNames)
            ? null
            : fileNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var useScan = wantFiles is not null || !string.IsNullOrWhiteSpace(folder)
                   || !string.IsNullOrWhiteSpace(search) || !string.IsNullOrWhiteSpace(category) || isVideo.HasValue;
        var truncated = false;
        if (useScan)
        {
            string? cursor = null;
            var pages = 0;
            do
            {
                var page = await client.ListImagesAsync(50, cursor, project: project,
                    search: search, category: category, folder: folder, isVideo: isVideo);
                foreach (var i in page.Images)
                {
                    if (wantFiles is not null && !wantFiles.Contains(i.OriginalFileName)) continue;
                    targets[i.ImageId] = i.OriginalFileName;
                }
                cursor = page.NextCursor;
            } while (cursor is not null && ++pages < 60);
            truncated = cursor is not null; // hit the ~3000-image scan cap with more remaining
        }

        if (targets.Count == 0)
            return "No matching images to delete.";
        if (targets.Count > max)
            return $"Refusing to delete {targets.Count} images — that exceeds the safety cap of {max}. " +
                   $"Narrow the selection, or raise 'max' if this is intended.";

        var proj = project ?? "default";
        if (!confirm)
        {
            var preview = targets.Take(50).Select(t => $"- {t.Key}{(t.Value.Length > 0 ? $" ({t.Value})" : "")}");
            return $"DRY RUN — would delete {targets.Count} image(s) from project '{proj}':\n" +
                   string.Join("\n", preview) +
                   (targets.Count > 50 ? $"\n… and {targets.Count - 50} more" : "") +
                   (truncated ? "\n(scan hit ~3000 images; more may match)" : "") +
                   "\n\nRe-run with confirm=true to permanently delete these.";
        }

        var ok = 0;
        var errors = new List<string>();
        foreach (var (id, _) in targets)
        {
            try { await client.DeleteImageAsync(id, project); ok++; }
            catch (Exception ex) { errors.Add($"{id}: {ex.Message}"); }
        }

        var result = $"Deleted {ok}/{targets.Count} image(s) from project '{proj}'.";
        if (errors.Count > 0)
            result += $"\nErrors ({errors.Count}):\n" + string.Join("\n", errors.Take(10));
        return result;
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
        [Description("Named transform preset to apply")] string? transform = null,
        [Description("Watermark ID to overlay (must already exist for the project)")] string? watermarkId = null,
        [Description("Watermark position: tl, tr, bl, br, c (center), tile (default br)")] string? watermarkPosition = null,
        [Description("Watermark opacity 0-100 (default 30)")] int? watermarkOpacity = null)
    {
        var builder = imageService.For(project, imageId);

        if (transform is not null) builder.Transform(transform);
        if (width.HasValue) builder.Width(width.Value);
        if (height.HasValue) builder.Height(height.Value);
        if (fit is not null) builder.Fit(ParseFitMode(fit));
        if (quality.HasValue) builder.Quality(quality.Value);
        if (blur.HasValue) builder.Blur(blur.Value);
        if (watermarkId is not null)
            builder.Watermark(watermarkId, ParseWmPosition(watermarkPosition), watermarkOpacity ?? 30);
        if (format is not null) builder.Format(format);

        return builder.Build();
    }

    [McpServerTool, Description(
        "Generate a ready-to-paste responsive HTML embed for an image. Returns either an <img> tag with a " +
        "srcset (auto format negotiation) or a <picture> element with AVIF/WebP sources and a JPEG fallback. " +
        "Ideal for dropping into web pages with correct SEO, lazy-loading, and responsive sizing.")]
    public static string BuildImageEmbed(
        PixaultImageService imageService,
        [Description("Project identifier")] string project,
        [Description("Image ID")] string imageId,
        [Description("Alt text for the image (required for accessibility and SEO)")] string alt,
        [Description("Embed style: 'img' (single tag, auto format) or 'picture' (AVIF/WebP/JPEG sources). Default 'picture'.")] string style = "picture",
        [Description("Comma-separated candidate widths in pixels (default '400,800,1200')")] string? widths = null,
        [Description("CSS 'sizes' attribute describing layout width (default '100vw')")] string sizes = "100vw",
        [Description("Loading strategy: 'lazy' or 'eager' (default 'lazy')")] string loading = "lazy",
        [Description("Optional CSS class to apply to the tag")] string? cssClass = null,
        [Description("Named transform preset to apply before sizing")] string? transform = null)
    {
        var builder = imageService.For(project, imageId);
        if (transform is not null) builder.Transform(transform);

        var widthArray = widths?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(w => int.TryParse(w, out var v) ? v : 0)
            .Where(v => v > 0)
            .ToArray();
        if (widthArray is { Length: 0 }) widthArray = null;

        return style.Equals("img", StringComparison.OrdinalIgnoreCase)
            ? builder.ToImgTag(alt, widthArray, sizes, loading, cssClass)
            : builder.ToPictureTag(alt, widthArray, sizes, loading, cssClass);
    }

    [McpServerTool, Description(
        "Find an image by its EXACT original filename within a project and return its ID plus ready-to-use CDN URLs. " +
        "Use this to turn a known filename (e.g. a per-shop QR code) into a link. For fuzzy/partial matching, use " +
        "ListImages with 'search' instead.")]
    public static async Task<string> FindImageByFilename(
        PixaultAdminClient client,
        IOptions<PixaultOptions> options,
        [Description("The exact original filename to find, including extension (e.g. 'final-cut-boca-llc-1426.svg')")] string fileName,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var proj = project ?? options.Value.DefaultProject ?? "default";
        var baseUrl = (options.Value.CdnUrl ?? options.Value.BaseUrl).TrimEnd('/');

        // 'search' narrows server-side (filename/name/id substrings); then keep the exact filename matches.
        var response = await client.ListImagesAsync(50, null, project: project, search: fileName);
        var matches = response.Images
            .Where(i => string.Equals(i.OriginalFileName, fileName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.UploadedAt)
            .ToList();

        if (matches.Count == 0)
            return $"No image with the exact filename '{fileName}' in project '{proj}'. " +
                   $"Try ListImages with search=\"{fileName}\" for partial matches.";

        var img = matches[0];
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var lines = new List<string>
        {
            $"Found '{fileName}' in project '{proj}':",
            $"- Image ID:      {img.ImageId}",
            $"- Dimensions:    {img.Width}x{img.Height} ({img.FormattedSize})",
            $"- Filename URL:  {baseUrl}/{proj}/{fileName}",
            $"- Canonical URL: {baseUrl}/{proj}/{img.ImageId}/original.{ext}",
        };
        if (matches.Count > 1)
            lines.Add($"\nNote: {matches.Count} images share this filename; returned the most recent. " +
                      $"Others: {string.Join(", ", matches.Skip(1).Select(m => m.ImageId))}");
        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Resolve many original filenames to their Pixault CDN URLs in one call. Returns each filename → its clean " +
        "filename URL and flags any that don't exist in the project. Ideal for turning a batch of known filenames " +
        "(e.g. per-shop QR codes) into links.")]
    public static async Task<string> ResolveImageUrls(
        PixaultAdminClient client,
        IOptions<PixaultOptions> options,
        [Description("Filenames to resolve, comma- or newline-separated, each with its extension")] string fileNames,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var proj = project ?? options.Value.DefaultProject ?? "default";
        var baseUrl = (options.Value.CdnUrl ?? options.Value.BaseUrl).TrimEnd('/');

        var requested = fileNames
            .Split([',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requested.Count == 0)
            return "No filenames provided.";

        // Page the project once and build a filename set — cheaper than one search per filename for large batches.
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = await client.ListImagesAsync(50, cursor, project: project);
            foreach (var i in page.Images) existing.Add(i.OriginalFileName);
            cursor = page.NextCursor;
        } while (cursor is not null && ++pages < 40);   // cap ~2000 images scanned

        var found = new List<string>();
        var missing = new List<string>();
        foreach (var fn in requested)
        {
            if (existing.Contains(fn)) found.Add($"- {fn} → {baseUrl}/{proj}/{fn}");
            else missing.Add(fn);
        }

        var lines = new List<string> { $"Resolved {found.Count}/{requested.Count} filename(s) in project '{proj}':", "" };
        lines.AddRange(found);
        if (missing.Count > 0)
            lines.Add($"\nNot found ({missing.Count}): {string.Join(", ", missing)}");
        if (cursor is not null)
            lines.Add("\nNote: scanned the first ~2000 images; a larger project may have unchecked filenames.");
        return string.Join("\n", lines);
    }

    private static FitMode ParseFitMode(string fit) => fit.ToLowerInvariant() switch
    {
        "cover" => FitMode.Cover,
        "contain" => FitMode.Contain,
        "fill" => FitMode.Fill,
        "pad" => FitMode.Pad,
        _ => FitMode.Cover
    };

    private static WmPosition ParseWmPosition(string? pos) => pos?.ToLowerInvariant() switch
    {
        "tl" => WmPosition.TopLeft,
        "tr" => WmPosition.TopRight,
        "bl" => WmPosition.BottomLeft,
        "br" => WmPosition.BottomRight,
        "c" or "center" => WmPosition.Center,
        "tile" => WmPosition.Tile,
        _ => WmPosition.BottomRight
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
