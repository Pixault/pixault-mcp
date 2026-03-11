# Pixault MCP Server

[Model Context Protocol](https://modelcontextprotocol.io) server for the [Pixault](https://pixault.io) image processing CDN. Gives AI assistants the ability to manage images, apply transforms, browse documentation, and more.

## Features

- **Image management** — list, search, upload, and delete images
- **Folder operations** — create, list, and navigate folders
- **Transformations** — apply and manage named transforms
- **Plugin marketplace** — browse and install plugins
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

## Dependencies

- [Pixault.Client](https://github.com/pixault/pixault-dotnet) — .NET SDK
- [ModelContextProtocol](https://github.com/modelcontextprotocol/csharp-sdk) — C# MCP SDK

## License

[MIT](LICENSE)
