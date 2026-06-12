using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class EpsTools
{
    [McpServerTool, Description(
        "List the derived assets generated from a source image — for example the individual designs split out " +
        "of a multi-design EPS file, or an SVG extracted from vector source. Returns each derivative's ID, type, " +
        "dimensions, and size.")]
    public static async Task<string> GetDerivedAssets(
        PixaultAdminClient client,
        [Description("The source image ID to list derivatives for")] string imageId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var assets = await client.GetDerivedAssetsAsync(imageId, project);
        if (assets.Count == 0)
            return $"No derived assets for '{imageId}'.";

        var lines = new List<string> { $"Found {assets.Count} derived assets for '{imageId}':", "" };
        foreach (var a in assets)
        {
            var type = a.DerivationType is not null ? $" [{a.DerivationType}]" : "";
            lines.Add($"- {a.ImageId}{type}: {a.Width}x{a.Height}, {a.ContentType} ({FormatSize(a.SizeBytes)})");
        }
        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Get the processing status of an EPS split or vector-extraction job for an image, including how many " +
        "assets have been processed, succeeded, and failed.")]
    public static async Task<string> GetProcessingStatus(
        PixaultAdminClient client,
        [Description("The image ID whose processing job to inspect")] string imageId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var status = await client.GetEpsProcessingStatusAsync(imageId, project);
        if (status is null)
            return $"No processing job found for '{imageId}'.";

        var lines = new List<string>
        {
            $"Job: {status.Id}",
            $"Source: {status.Source}",
            $"Status: {status.Status}",
            $"Progress: {status.ProcessedAssets}/{status.TotalAssets} processed ({status.SucceededAssets} succeeded, {status.FailedAssets} failed)",
            $"Created: {status.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC"
        };
        if (status.StartedAt is { } started) lines.Add($"Started: {started:yyyy-MM-dd HH:mm:ss} UTC");
        if (status.CompletedAt is { } completed) lines.Add($"Completed: {completed:yyyy-MM-dd HH:mm:ss} UTC");

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Split a multi-design EPS file into individual design assets. This starts an asynchronous job; poll " +
        "GetProcessingStatus and then GetDerivedAssets to retrieve the resulting designs.")]
    public static async Task<string> SplitEpsDesigns(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The EPS image ID to split into separate designs")] string imageId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        await client.SplitEpsDesignsAsync(imageId, project);
        return $"EPS split started for '{imageId}'. Use GetProcessingStatus to track progress.";
    }

    [McpServerTool, Description(
        "Extract a scalable SVG from an EPS/vector source image. This starts an asynchronous job; poll " +
        "GetProcessingStatus and then GetDerivedAssets to retrieve the SVG.")]
    public static async Task<string> ExtractEpsSvg(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("The EPS image ID to extract an SVG from")] string imageId,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        await client.ExtractEpsSvgAsync(imageId, project);
        return $"SVG extraction started for '{imageId}'. Use GetProcessingStatus to track progress.";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
