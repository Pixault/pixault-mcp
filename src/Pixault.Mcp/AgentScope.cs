using Pixault.Client;

namespace Pixault.Mcp;

/// <summary>
/// Enforces <see cref="AgentPermissions"/> across the MCP tools so operators can
/// restrict what an AI assistant is allowed to do through this server.
///
/// Read operations and URL/embed generation are always permitted. Mutating tools
/// call the matching <c>Check*</c> method first; when a category is disabled the tool
/// returns the denial message instead of performing the operation.
///
/// Configure via (either style binds):
///   <c>PIXAULT_ALLOW_WRITE</c>   / <c>Pixault__AllowWrite</c>   — create/update (default: true)
///   <c>PIXAULT_ALLOW_DELETE</c>  / <c>Pixault__AllowDelete</c>  — delete (default: false)
///   <c>PIXAULT_ALLOW_PLUGINS</c> / <c>Pixault__AllowPlugins</c> — activate/deactivate plugins (default: false)
/// </summary>
public sealed class AgentScope
{
    private readonly AgentPermissions _permissions;

    public AgentScope(AgentPermissions permissions) => _permissions = permissions;

    /// <summary>Returns a denial message if write (create/update) is disabled, otherwise null.</summary>
    public string? CheckWrite() =>
        _permissions.AllowWrite ? null : Denied("write (create/update)", "PIXAULT_ALLOW_WRITE");

    /// <summary>Returns a denial message if delete is disabled, otherwise null.</summary>
    public string? CheckDelete() =>
        _permissions.AllowDelete ? null : Denied("delete", "PIXAULT_ALLOW_DELETE");

    /// <summary>Returns a denial message if plugin management is disabled, otherwise null.</summary>
    public string? CheckPluginManagement() =>
        _permissions.AllowPluginManagement ? null : Denied("plugin management", "PIXAULT_ALLOW_PLUGINS");

    private static string Denied(string category, string setting) =>
        $"Permission denied: this Pixault MCP server is configured without {category} access. " +
        $"An operator can enable it by setting {setting}=true.";
}
