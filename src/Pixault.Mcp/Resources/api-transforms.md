# Named Transforms

Named transforms are reusable transformation presets that you define once and reference by name in image URLs. They enforce consistent image dimensions and quality across your application.

## Why Named Transforms?

- **Consistency** — Ensure all thumbnails, gallery images, and hero images use identical parameters
- **Security** — Lock parameters so clients can't request arbitrary sizes
- **Simplicity** — Use `t_thumbnail` instead of `w_200,h_200,fit_cover,q_80`
- **Maintainability** — Change dimensions in one place, all URLs update automatically

## Usage in URLs

```
# Apply a named transform
https://img.pixault.io/myapp/img_01JK/t_thumbnail.webp

# Named transform with overrides (if allowed)
https://img.pixault.io/myapp/img_01JK/t_gallery,w_400.webp
```

When a parameter is **locked** in the transform definition, URL overrides for that parameter are ignored.

## API Reference

All endpoints require API key authentication.

### Create a Named Transform

**`POST /api/{project}/transforms`**

```bash
curl -X POST https://img.pixault.io/api/myapp/transforms \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "thumbnail",
    "parameters": {
      "w": 200,
      "h": 200,
      "fit": "cover",
      "q": 80
    },
    "locked": ["w", "h", "fit"],
    "description": "Standard thumbnail for grid views"
  }'
```

#### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Transform name (alphanumeric, hyphens, underscores) |
| `parameters` | object | Yes | Default transform parameters |
| `locked` | string[] | No | Parameters that cannot be overridden via URL |
| `description` | string | No | Human-readable description |

#### Response `201 Created`

```json
{
  "name": "thumbnail",
  "parameters": { "w": 200, "h": 200, "fit": "cover", "q": 80 },
  "locked": ["w", "h", "fit"],
  "description": "Standard thumbnail for grid views",
  "createdAt": "2025-03-15T10:00:00Z"
}
```

### List Named Transforms

**`GET /api/{project}/transforms`**

```bash
curl https://img.pixault.io/api/myapp/transforms \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456"
```

#### Response `200 OK`

```json
{
  "transforms": [
    {
      "name": "thumbnail",
      "parameters": { "w": 200, "h": 200, "fit": "cover", "q": 80 },
      "locked": ["w", "h", "fit"],
      "description": "Standard thumbnail for grid views"
    },
    {
      "name": "gallery",
      "parameters": { "w": 800, "q": 85 },
      "locked": [],
      "description": "Gallery view — width locked, quality adjustable"
    }
  ]
}
```

### Get a Named Transform

**`GET /api/{project}/transforms/{name}`**

### Update a Named Transform

**`PUT /api/{project}/transforms/{name}`**

Send the full transform definition. All fields are replaced.

### Delete a Named Transform

**`DELETE /api/{project}/transforms/{name}`**

Returns `204 No Content`.

## Common Transform Presets

Here are recommended presets for typical use cases:

### Thumbnails

```json
{
  "name": "thumb",
  "parameters": { "w": 200, "h": 200, "fit": "cover", "q": 80 },
  "locked": ["w", "h", "fit"]
}
```

### Gallery

```json
{
  "name": "gallery",
  "parameters": { "w": 800, "h": 600, "fit": "cover", "q": 85 },
  "locked": ["w", "h"]
}
```

### Hero / Banner

```json
{
  "name": "hero",
  "parameters": { "w": 1920, "h": 600, "fit": "cover", "q": 90 },
  "locked": ["w", "h", "fit"]
}
```

### LQIP Placeholder

```json
{
  "name": "lqip",
  "parameters": { "w": 40, "q": 20, "blur": 10 },
  "locked": ["w", "q", "blur"]
}
```

### Watermarked Download

```json
{
  "name": "download_wm",
  "parameters": { "w": 1200, "q": 90, "wm": "company_logo", "wm_pos": "br", "wm_opacity": 30 },
  "locked": ["wm", "wm_pos", "wm_opacity"]
}
```

## Parameter Resolution

When a named transform is used alongside explicit URL parameters, Pixault resolves them in this order:

1. Start with the named transform's default parameters
2. Apply URL parameters as overrides
3. Skip overrides for locked parameters

```
# Transform "gallery" = { w: 800, h: 600, fit: "cover", q: 85 } with locked: ["w", "h"]

# URL: /myapp/img_01JK/t_gallery,q_90.webp
# Result: w=800, h=600, fit=cover, q=90  (q was overridden, w and h were locked)
```
