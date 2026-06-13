# Pixault Documentation

Pixault is an image processing CDN and API that handles upload, storage, transformation, watermarking, and global delivery of images, video, and EPS/PostScript vector artwork. Build responsive, optimized image pipelines without managing infrastructure.

## What Pixault Does

- **Upload** images, video, and EPS/PostScript files via REST API or SDK
- **Transform** on-the-fly: resize, crop, blur, watermark, format conversion
- **Deliver** globally through edge CDN with immutable caching
- **Track** usage with built-in bandwidth, storage, and request metering

## How It Works

```
Your App → Upload API → Cloud Storage
                           ↓
User Request → CDN Edge → Pixault API → Transform → Cache → Respond
                  ↓
              Cache Hit → Serve instantly (zero origin traffic)
```

Every image is addressable by a URL that encodes the desired transformations:

```
https://img.pixault.io/{project}/{imageId}/{transforms}.{format}
```

For example:

```
https://img.pixault.io/myapp/img_01JK/w_800,h_600,fit_cover,q_85.webp
```

Asset IDs are prefixed by type: `img_` for images, `vid_` for video, and `eps_` for EPS/PostScript files.

## Authentication

All API requests authenticate with a single header:

```
X-Api-Key: <your-api-key>
```

There is no client-id/client-secret pair — just the one key.

## Key Features

| Feature | Description |
|---------|-------------|
| On-the-fly transforms | Resize, crop, blur, quality, format conversion via URL params |
| Named transforms | Reusable presets with lockable parameters for consistency |
| Watermarking | Positioned or tiled watermark management with opacity control |
| Video | MP4/WebM/MOV with auto-generated thumbnails and HTTP range streaming |
| EPS/PostScript vector | Split multi-design artboards, extract SVG, generate derived assets |
| AVIF output | Modern AVIF encoding alongside WebP, JPEG, and PNG |
| `.auto` format negotiation | The `.auto` extension picks AVIF/WebP/etc. from the `Accept` header (`Vary: Accept`) |
| Responsive embeds | Ready-to-paste `<img srcset>` / `<picture>` HTML embeds |
| Schema.org metadata | Structured metadata + JSON-LD, geo-location, folders, and custom key=value tags |
| Multi-project | Isolated storage and billing per project |
| Usage metering | Bandwidth, storage, and request tracking per subscription |
| Signed URLs | HMAC-SHA256 signed URLs for original file access |
| Plugin marketplace | Activate marketplace plugins per project to extend processing |
| SDK support | .NET, JavaScript, Python, and PHP SDKs plus a Blazor component library |

## Getting Started

[Quick Start Guide](quick-start.md) — Get your first image uploaded and served in under 5 minutes.

## SDKs

| Language | Package | Status |
|----------|---------|--------|
| .NET | `Pixault.Client` | Published on NuGet |
| JavaScript | `@pixault/sdk` | Available (install from source — not yet on npm) |
| Python | `pixault` | Available (install from source — not yet on PyPI) |
| PHP | `pixault/pixault-php` | Available (install from source — not yet on Packagist) |

A Blazor component library is also available for Blazor apps, and the **Pixault MCP server** (`Pixault.Mcp`) exposes Pixault to AI assistants.

The .NET SDK is published and installs directly from NuGet:

```
dotnet add package Pixault.Client
```

The JavaScript, Python, and PHP packages are not yet published to npm/PyPI/Packagist — install them from source for now; registry releases are coming.

## API Base URLs

| Endpoint | URL |
|----------|-----|
| Image CDN & API | `https://img.pixault.io` |
| Dashboard | `https://pixault.io` |
