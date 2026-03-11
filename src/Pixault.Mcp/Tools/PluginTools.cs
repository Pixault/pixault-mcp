using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class PluginTools
{
    [McpServerTool, Description(
        "List all available Pixault marketplace plugins. Plugins extend image processing with capabilities " +
        "like background removal, smart cropping, image filters, and watermark templates.")]
    public static async Task<string> ListPlugins(PixaultAdminClient client)
    {
        var plugins = await client.GetAllPluginsAsync();
        if (plugins.Count == 0)
            return "No plugins available.";

        var lines = new List<string> { $"Available plugins ({plugins.Count}):", "" };

        foreach (var p in plugins)
        {
            var price = p.PriceCentsPerInvocation > 0
                ? $"${p.PriceCentsPerInvocation / 100.0:F2}/use"
                : "free";
            lines.Add($"- {p.DisplayName} ({p.Name})");
            lines.Add($"  {p.Description}");
            lines.Add($"  Category: {p.Category} | Stage: {p.Stage} | Price: {price} | URL prefix: {p.UrlPrefix}");
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "List plugins with their activation status for a specific project. Shows which plugins are currently enabled.")]
    public static async Task<string> ListProjectPlugins(
        PixaultAdminClient client,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var plugins = await client.GetProjectPluginsAsync(project);
        if (plugins.Count == 0)
            return "No plugins available for this project.";

        var lines = new List<string> { $"Plugins for project ({plugins.Count}):", "" };

        foreach (var p in plugins)
        {
            var status = p.IsActivated ? "ACTIVE" : "inactive";
            lines.Add($"- [{status}] {p.DisplayName} ({p.Name}): {p.Description}");
        }

        return string.Join("\n", lines);
    }

    [McpServerTool, Description("Activate a marketplace plugin for a project. Once activated, the plugin can be invoked via URL parameters.")]
    public static async Task<string> ActivatePlugin(
        PixaultAdminClient client,
        [Description("Project identifier")] string project,
        [Description("Plugin name to activate (e.g. 'background-removal', 'smart-crop', 'image-filter')")] string pluginName)
    {
        await client.ActivatePluginAsync(project, pluginName);
        return $"Plugin '{pluginName}' activated for project '{project}'.";
    }

    [McpServerTool, Description("Deactivate a marketplace plugin for a project.")]
    public static async Task<string> DeactivatePlugin(
        PixaultAdminClient client,
        [Description("Project identifier")] string project,
        [Description("Plugin name to deactivate")] string pluginName)
    {
        await client.DeactivatePluginAsync(project, pluginName);
        return $"Plugin '{pluginName}' deactivated for project '{project}'.";
    }
}
