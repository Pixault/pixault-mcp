# Pixault MCP Server

[Model Context Protocol](https://modelcontextprotocol.io) server for the [Pixault](https://pixault.io) image processing CDN. Gives AI assistants the ability to manage images, apply transforms, browse documentation, and more.

## Features

- **Image management** — list, search, upload, and delete images
- **Metadata** — read and edit Schema.org fields, geo-location, folders, custom tags; strip EXIF
- **Folder operations** — create, list, and delete folders; filter images by folder
- **Transformations** — apply and manage named transforms; build CDN and responsive embed URLs
- **Watermarks** — upload, list, and delete watermark images
- **EPS / vector** — split multi-design EPS files, extract SVG, inspect derived assets
- **Plugin marketplace** — browse, activate, and deactivate plugins
- **Documentation** — embedded API docs available as resources

## Installation

### Claude Desktop / Claude Code

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "pixault": {
      "command": "dotnet",
      "args": ["run", "--project", "src/Pixault.Mcp"],
      "env": {
        "PIXAULT_BASE_URL": "https://img.pixault.io",
        "PIXAULT_PROJECT": "my-project",
        "PIXAULT_API_KEY": "pk_your_api_key"
      }
    }
  }
}
```

### From source

```bash
git clone https://github.com/pixault/pixault-mcp.git
cd pixault-mcp
dotnet run --project src/Pixault.Mcp
```

## Configuration

| Environment Variable | Required | Description |
|---------------------|----------|-------------|
| `PIXAULT_BASE_URL` | Yes | Pixault CDN base URL |
| `PIXAULT_PROJECT` | Yes | Default project identifier |
| `PIXAULT_API_KEY` | Yes | API key for authentication |
| `PIXAULT_HMAC_SECRET` | No | HMAC secret for signed URL generation |

> The `Pixault__BaseUrl` / `Pixault__Project` (double-underscore) form is also accepted for every setting.

### Agent scoping

Restrict what the AI assistant can do through this server. Read operations and URL/embed
generation are always allowed; the flags below gate mutating tools.

| Environment Variable | Default | Gates |
|---------------------|---------|-------|
| `PIXAULT_ALLOW_WRITE` | `true` | Uploads, metadata edits, transform/watermark/folder creation, EXIF strip, EPS jobs |
| `PIXAULT_ALLOW_DELETE` | `false` | Deleting images, transforms, watermarks, folders |
| `PIXAULT_ALLOW_PLUGINS` | `false` | Activating / deactivating marketplace plugins |

When a category is disabled, the corresponding tools return a clear "permission denied"
message instead of performing the operation.

## Dependencies

- [Pixault.Client](https://github.com/pixault/pixault-dotnet) — .NET SDK
- [ModelContextProtocol](https://github.com/modelcontextprotocol/csharp-sdk) — C# MCP SDK

## License

[MIT](LICENSE)
