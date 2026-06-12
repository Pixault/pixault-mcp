using System.ComponentModel;
using ModelContextProtocol.Server;
using Pixault.Client;

namespace Pixault.Mcp.Tools;

[McpServerToolType]
public sealed class FolderTools
{
    [McpServerTool, Description(
        "List the folders defined for a project. Folders organize images into a virtual hierarchy; " +
        "use the 'folder' filter on ListImages to browse a folder's contents.")]
    public static async Task<string> ListFolders(
        PixaultAdminClient client,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        var folders = await client.ListFoldersAsync(project);
        if (folders.Count == 0)
            return "No folders defined for this project.";

        var lines = new List<string> { $"Found {folders.Count} folders:", "" };
        lines.AddRange(folders.OrderBy(f => f).Select(f => $"- {f}"));
        return string.Join("\n", lines);
    }

    [McpServerTool, Description(
        "Create a folder in a project. Folder paths use forward slashes for nesting (e.g. 'products/hero'). " +
        "Images are assigned to folders via the 'folder' field on UpdateImageMetadata.")]
    public static async Task<string> CreateFolder(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("Folder path to create (e.g. 'products', 'products/hero')")] string folderPath,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckWrite() is { } denied) return denied;

        await client.CreateFolderAsync(folderPath, project);
        return $"Folder '{folderPath}' created.";
    }

    [McpServerTool, Description(
        "Delete a folder from a project. Images previously assigned to the folder are not deleted; they " +
        "become unfiled.")]
    public static async Task<string> DeleteFolder(
        PixaultAdminClient client,
        AgentScope scope,
        [Description("Folder path to delete")] string folderPath,
        [Description("Project identifier (uses default project if not specified)")] string? project = null)
    {
        if (scope.CheckDelete() is { } denied) return denied;

        await client.DeleteFolderAsync(folderPath, project);
        return $"Folder '{folderPath}' deleted.";
    }
}
