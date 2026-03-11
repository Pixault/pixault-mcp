using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class TransformTools
{
    [McpServerTool, Description(
        "List all named transform presets for the project. Named transforms are server-side presets " +
        "that define reusable transformation parameters (width, height, quality, watermark, etc.). " +
        "Parameters can be locked to prevent client-side override.")]
    public static async Task<string> ListTransforms(PixaultAdminClient client)
    {
        var transforms = await client.ListTransformsAsync();
        if (transforms.Count == 0)
            return "No named transforms configured.";

        var lines = new List<string> { $"Found {transforms.Count} named transforms:", "" };

        foreach (var t in transforms)
        {
            var parts = new List<string>();
            if (t.Width.HasValue) parts.Add($"w:{t.Width}");
            if (t.Height.HasValue) parts.Add($"h:{t.Height}");
            if (t.FitMode is not null) parts.Add($"fit:{t.FitMode}");
            if (t.Quality.HasValue) parts.Add($"q:{t.Quality}");
            if (t.Blur.HasValue) parts.Add($"blur:{t.Blur}");
            if (t.WatermarkId is not null) parts.Add($"wm:{t.WatermarkId}");
            if (t.LockedParameters.Count > 0) parts.Add($"locked:[{string.Join(",", t.LockedParameters)}]");

            lines.Add($"- {t.Name}: {string.Join(", ", parts)}");
        }

        return string.Join("\n", lines);
    }

    [McpServerTool, Description("Get details of a specific named transform preset.")]
    public static async Task<string> GetTransform(
        PixaultAdminClient client,
        [Description("Transform preset name")] string name)
    {
        var t = await client.GetTransformAsync(name);
        if (t is null)
            return $"Transform '{name}' not found.";

        var lines = new List<string>
        {
            $"Transform: {t.Name}",
            $"Project: {t.ProjectId}"
        };

        if (t.Width.HasValue) lines.Add($"Width: {t.Width}");
        if (t.Height.HasValue) lines.Add($"Height: {t.Height}");
        if (t.FitMode is not null) lines.Add($"Fit: {t.FitMode}");
        if (t.Quality.HasValue) lines.Add($"Quality: {t.Quality}");
        if (t.Blur.HasValue) lines.Add($"Blur: {t.Blur}");
        if (t.WatermarkId is not null)
        {
            lines.Add($"Watermark: {t.WatermarkId}");
            if (t.WatermarkPosition is not null) lines.Add($"Watermark Position: {t.WatermarkPosition}");
            if (t.WatermarkOpacity.HasValue) lines.Add($"Watermark Opacity: {t.WatermarkOpacity}%");
        }
        if (t.LockedParameters.Count > 0)
            lines.Add($"Locked Parameters: {string.Join(", ", t.LockedParameters)}");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Create or update a named transform preset. Presets allow reusable server-side transformation " +
        "configurations. Parameters can be locked to prevent client override (useful for enforcing watermarks).")]
    public static async Task<string> SaveTransform(
        PixaultAdminClient client,
        [Description("Preset name (lowercase alphanumeric + hyphens, max 32 chars)")] string name,
        [Description("Target width in pixels")] int? width = null,
        [Description("Target height in pixels")] int? height = null,
        [Description("Fit mode: cover, contain, fill, pad")] string? fitMode = null,
        [Description("Quality 1-100")] int? quality = null,
        [Description("Blur radius 1-100")] int? blur = null,
        [Description("Watermark image ID")] string? watermarkId = null,
        [Description("Watermark position: tl, tc, tr, cl, cc, cr, bl, bc, br")] string? watermarkPosition = null,
        [Description("Watermark opacity 0-100")] int? watermarkOpacity = null,
        [Description("Comma-separated parameter names to lock (e.g. 'watermark,quality')")] string? lockedParameters = null)
    {
        var save = new NamedTransformSave
        {
            Width = width,
            Height = height,
            FitMode = fitMode,
            Quality = quality,
            Blur = blur,
            WatermarkId = watermarkId,
            WatermarkPosition = watermarkPosition,
            WatermarkOpacity = watermarkOpacity,
            LockedParameters = lockedParameters?
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet()
        };

        var result = await client.SaveTransformAsync(name, save);
        return result is not null
            ? $"Transform '{name}' saved successfully."
            : $"Failed to save transform '{name}'.";
    }

    [McpServerTool, Description("Delete a named transform preset.")]
    public static async Task<string> DeleteTransform(
        PixaultAdminClient client,
        [Description("Transform preset name to delete")] string name)
    {
        await client.DeleteTransformAsync(name);
        return $"Transform '{name}' deleted.";
    }
}
