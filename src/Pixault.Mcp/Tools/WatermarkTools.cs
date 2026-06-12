using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class WatermarkTools
{
    [McpServerTool, Description(
        "List the watermark images stored for a project. Watermarks are referenced by ID in named " +
        "transforms (see SaveTransform) and in BuildImageUrl to overlay a logo or mark on delivered images.")]
    public static async Task<string> ListWatermarks(
        PixaultAdminClient client,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var watermarks = await client.ListWatermarksAsync(project);
        if (watermarks.Count == 0)
            return "No watermarks configured for this project.";

        var lines = new List<string> { $"Found {watermarks.Count} watermarks:", "" };
        foreach (var w in watermarks)
        {
            var updated = w.UpdatedAt is { } u ? u.ToString("yyyy-MM-dd") : "unknown";
            lines.Add($"- {w.Id} ({w.ContentType}, {FormatSize(w.SizeBytes)}, updated {updated})");
        }

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Upload (or replace) a watermark image from a local file path. The watermark is stored under the " +
        "given ID and can then be applied to images via named transforms or BuildImageUrl. PNG with " +
        "transparency is recommended. Supported formats: PNG, WebP, SVG.")]
    public static async Task<string> UploadWatermark(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("Watermark ID to create or overwrite (e.g. 'logo', 'brand-mark')")] string watermarkId,
        [Description("Absolute path to the watermark image file")] string filePath,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        if (!File.Exists(filePath))
            return $"Error: File not found at '{filePath}'";

        var contentType = GetContentType(filePath);
        await using var stream = File.OpenRead(filePath);
        var result = await client.UploadWatermarkAsync(watermarkId, stream, contentType, project);

        return result is not null
            ? $"Watermark '{result.Id}' uploaded ({result.ContentType}, {FormatSize(result.SizeBytes)})."
            : $"Watermark '{watermarkId}' uploaded.";
    }

    [McpServerTool, Description(
        "Delete a watermark image. Named transforms that reference this watermark will no longer apply it.")]
    public static async Task<string> DeleteWatermark(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("Watermark ID to delete")] string watermarkId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckDelete() is { } denied) return denied;

        await client.DeleteWatermarkAsync(watermarkId, project);
        return $"Watermark '{watermarkId}' deleted.";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/png"
        };
    }
}
