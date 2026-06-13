# Named Transforms

Named transforms are reusable, server-side transformation presets. You define a set of transform parameters once under a short name, then reference that name in delivery URLs with `t_{name}` instead of repeating raw parameters.

## Why named transforms?

- **Reuse** — define `thumbnail` once, use `t_thumbnail` everywhere instead of `w_200,h_200,fit_cover,q_80`.
- **Consistency** — every caller gets identical dimensions, quality, and watermarking.
- **Enforcement** — lock specific parameters so client URL params can't override them. This is how you guarantee a watermark or cap the output size regardless of what the client requests.
- **Maintainability** — change the preset in one place; every URL that references it updates.

## Authentication

All transform-management endpoints require a single header:

```
X-Api-Key: <your-api-key>
```

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/{project}/transforms` | List all named transforms |
| `GET` | `/api/{project}/transforms/{name}` | Get one named transform |
| `PUT` | `/api/{project}/transforms/{name}` | Create or update (upsert) |
| `DELETE` | `/api/{project}/transforms/{name}` | Delete |

There is no `POST` — creates and updates both go through `PUT /api/{project}/transforms/{name}` (upsert).

### Name rules

The transform `{name}` must match `^[a-z0-9][a-z0-9\-]{0,31}$`:

- lowercase letters and digits only, plus hyphens
- must start with a letter or digit
- max 32 characters
- no underscores, no uppercase

## Request body (PUT)

The body is a flat JSON object. Every field is optional — send only what the preset needs.

| Field | Type | Notes |
|-------|------|-------|
| `width` | int | Target width in pixels |
| `height` | int | Target height in pixels |
| `fitMode` | string | `cover`, `contain`, `fill`, or `pad` |
| `quality` | int | 1–100 |
| `blur` | int | Blur radius |
| `watermarkId` | string | ID of a watermark (see Watermarks) |
| `watermarkPosition` | string | `tl`, `tr`, `bl`, `br`, `c`, or `tile` |
| `watermarkOpacity` | int | 0–100 |
| `plugins` | object | Per-plugin settings applied by this transform |
| `lockedPlugins` | string[] | Plugin names the client cannot toggle off |
| `lockedParameters` | string[] | Parameters the client cannot override via URL |

Valid `lockedParameters` values: `width`, `height`, `fitMode`, `quality`, `blur`, `watermarkId`, `watermarkPosition`, `watermarkOpacity`.

### Example: create or update a `thumbnail` transform

```bash
curl -X PUT https://img.pixault.io/api/myapp/transforms/thumbnail \
  -H "X-Api-Key: <your-api-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "width": 200,
    "height": 200,
    "fitMode": "cover",
    "quality": 80,
    "lockedParameters": ["width", "height", "fitMode"]
  }'
```

### Response

The response is a flat `NamedTransformDto`:

```json
{
  "name": "thumbnail",
  "projectId": "myapp",
  "width": 200,
  "height": 200,
  "fitMode": "cover",
  "quality": 80,
  "blur": null,
  "watermarkId": null,
  "watermarkPosition": null,
  "watermarkOpacity": null,
  "plugins": null,
  "lockedPlugins": [],
  "lockedParameters": ["width", "height", "fitMode"]
}
```

`GET /api/{project}/transforms` returns an array of these objects.

## Applying a transform in a delivery URL

Reference a transform with the `t_{name}` token in a public delivery URL (delivery URLs are unauthenticated and served from the CDN — no `/api` prefix):

```
https://img.pixault.io/myapp/img_01JK/t_thumbnail.webp
```

You can combine `t_{name}` with explicit transform params in the same comma-separated segment:

```
https://img.pixault.io/myapp/img_01JK/t_gallery,w_400.webp
```

### How overrides and locked params interact

1. Start from the transform's preset values.
2. Apply any explicit URL params (`w_`, `h_`, `fit_`, `q_`, `blur_`, `wm_`, `wm_pos_`, `wm_opacity_`) as overrides.
3. Drop any override that targets a parameter listed in `lockedParameters` — the preset value wins.

```
# transform "gallery" = { width: 800, height: 600, fitMode: cover, quality: 85 }
#                        lockedParameters: ["width", "height"]

# URL: /myapp/img_01JK/t_gallery,q_90,w_400.webp
# Result: width=800, height=600, fitMode=cover, quality=90
#         (q_90 applied; w_400 dropped because width is locked)
```

## Enforcing a watermark

Lock the watermark parameters so no client URL can strip or move the watermark:

```bash
curl -X PUT https://img.pixault.io/api/myapp/transforms/download \
  -H "X-Api-Key: <your-api-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "width": 1200,
    "quality": 90,
    "watermarkId": "company-logo",
    "watermarkPosition": "br",
    "watermarkOpacity": 30,
    "lockedParameters": ["watermarkId", "watermarkPosition", "watermarkOpacity"]
  }'
```

Any request to `…/t_download.webp` now carries the watermark, and `…/t_download,wm_opacity_0.webp` still renders at opacity 30 because `watermarkOpacity` is locked.

## Watermarks

`watermarkId` values refer to watermarks managed through the watermark API:

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/api/{project}/watermarks` | List watermarks |
| `PUT` | `/api/{project}/watermarks/{id}` | Upload/replace a watermark (raw image body) |
| `DELETE` | `/api/{project}/watermarks/{id}` | Delete a watermark |

Upload a watermark image, then reference its `{id}` as `watermarkId` in a transform (or as `wm_{id}` in a delivery URL).
