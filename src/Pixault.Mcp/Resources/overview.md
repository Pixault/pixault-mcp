# Pixault Documentation

Pixault is an image processing CDN and API that handles upload, storage, transformation, watermarking, and global delivery of images. Build responsive, optimized image pipelines without managing infrastructure.

## What Pixault Does

- **Upload** images via REST API or SDK
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

## Key Features

| Feature | Description |
|---------|-------------|
| On-the-fly transforms | Resize, crop, blur, quality, format conversion via URL params |
| Named transforms | Reusable presets with locked parameters for consistency |
| Watermarking | Positioned or tiled watermarks with opacity control |
| Format negotiation | Automatic WebP/AVIF based on `Accept` header |
| Multi-project | Isolated storage and billing per project |
| Usage metering | Bandwidth, storage, and request tracking per subscription |
| Signed URLs | HMAC-SHA256 signed URLs for original file access |
| SDK support | .NET, JavaScript, PHP, Python SDKs |

## Getting Started

<a href="quick-start.md">Quick Start Guide</a> — Get your first image uploaded and served in under 5 minutes.

## SDKs

| Language | Package |
|----------|---------|
| .NET | `Pixault.Client` (NuGet) |
| JavaScript | `@pixault/sdk` (npm) |
| PHP | `pixault/pixault-php` (Composer) |
| Python | `pixault` (PyPI) |

## API Base URLs

| Endpoint | URL |
|----------|-----|
| Image CDN | `https://img.pixault.io` |
| Dashboard | `https://pixault.io` |
| API | `https://img.pixault.io/api` |
| Docs | `https://pixault.dev` |
