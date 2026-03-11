# Image Delivery API

Pixault serves transformed images through a URL-based API. Every transformation is encoded directly in the URL path, making images fully cacheable at the CDN edge.

## URL Pattern

```
https://img.pixault.io/{project}/{imageId}/{transformations}.{format}
```

| Segment | Description |
|---------|-------------|
| `project` | Your project identifier (e.g., `myapp`) |
| `imageId` | The image ID returned from upload (e.g., `img_01JKXYZ`) |
| `transformations` | Comma-separated key-value pairs (e.g., `w_800,h_600,fit_cover`) |
| `format` | Output format as file extension: `jpg`, `png`, `webp`, `avif` |

## Transformation Parameters

| Parameter | Values | Default | Description |
|-----------|--------|---------|-------------|
| `w` | 1–4096 | Original | Width in pixels |
| `h` | 1–4096 | Original | Height in pixels |
| `fit` | `cover`, `contain`, `fill`, `pad` | `cover` | Resize mode |
| `q` | 1–100 | 80 | Output quality |
| `blur` | 1–100 | — | Gaussian blur radius |
| `wm` | watermark ID | — | Watermark overlay |
| `wm_pos` | `tl`, `tr`, `bl`, `br`, `c`, `tile` | `br` | Watermark position |
| `wm_opacity` | 1–100 | 30 | Watermark opacity percentage |
| `t` | transform name | — | Apply a named transform preset |

### Fit Modes

| Mode | Behavior |
|------|----------|
| `cover` | Resize to fill the target dimensions, cropping excess. No whitespace. |
| `contain` | Resize to fit within the target dimensions. May have letterboxing. |
| `fill` | Stretch to exactly fill the target dimensions. May distort. |
| `pad` | Fit within the target dimensions and pad remaining area with background. |

## Examples

### Basic Resize

```
# 800px wide, auto height, WebP
https://img.pixault.io/myapp/img_01JK/w_800.webp

# 200x200 thumbnail, cover crop
https://img.pixault.io/myapp/img_01JK/w_200,h_200,fit_cover.webp
```

### Quality Control

```
# High quality for print
https://img.pixault.io/myapp/img_01JK/w_1200,q_95.jpg

# Compressed for mobile
https://img.pixault.io/myapp/img_01JK/w_400,q_60.webp
```

### Low-Quality Image Placeholder (LQIP)

Generate tiny blurred placeholders for progressive image loading:

```
https://img.pixault.io/myapp/img_01JK/w_40,q_20,blur_10.webp
```

Use this as a `background-image` while the full image loads.

### Watermarking

```
# Bottom-right watermark at 40% opacity
https://img.pixault.io/myapp/img_01JK/w_1200,wm_logo,wm_pos_br,wm_opacity_40.jpg

# Tiled watermark across entire image
https://img.pixault.io/myapp/img_01JK/w_1200,wm_copyright,wm_pos_tile,wm_opacity_20.jpg
```

### Named Transforms

Apply preconfigured transform presets:

```
# Apply the "gallery" transform
https://img.pixault.io/myapp/img_01JK/t_gallery.webp

# Named transform with overrides
https://img.pixault.io/myapp/img_01JK/t_gallery,w_400.webp
```

See <a href="api-transforms.md">Named Transforms</a> for creating presets.

## Signed URLs

For original file downloads, Pixault supports HMAC-SHA256 signed URLs:

```
https://img.pixault.io/myapp/img_01JK/original.jpg?sig=abc123def&exp=1709312400
```

| Parameter | Description |
|-----------|-------------|
| `sig` | HMAC-SHA256 signature |
| `exp` | Unix timestamp expiration |

The signature is computed over `{project}/{imageId}/original.{format}?exp={timestamp}` using your account's HMAC secret.

## Response Headers

| Header | Value | Description |
|--------|-------|-------------|
| `Cache-Control` | `public, max-age=2592000, immutable` | 30-day CDN caching |
| `Content-Type` | `image/webp`, `image/jpeg`, etc. | Output format MIME type |
| `X-Pixault-Cache` | `HIT` or `MISS` | Whether the variant was cached |
| `ETag` | `"sha256hash"` | Content hash for conditional requests |

## Format Negotiation

If the `Accept` header includes `image/webp` or `image/avif`, Pixault will automatically select the most efficient format when the URL extension is omitted. Explicit format extensions always take precedence.

## Rate Limits

Delivery endpoints use per-project sliding window rate limiting:

| Limit | Value |
|-------|-------|
| Requests per minute | 1,000 per project |
| Queue depth | 50 |

Exceeding the limit returns `429 Too Many Requests` with a `Retry-After` header.

## Error Responses

All errors follow RFC 7807 ProblemDetails format:

```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Not Found",
  "detail": "Image 'img_01JKXYZ' not found in project 'myapp'.",
  "status": 404
}
```
