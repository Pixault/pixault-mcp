# Upload API

Upload images, video, and EPS/PostScript assets to Pixault via a single multipart form POST. All API requests require API key authentication.

## Authentication

Every request to the API authenticates with a single header:

```
X-Api-Key: your-api-key
```

This is the only authentication scheme. There is no client-id/client-secret pair.

## Upload an Asset

**`POST /api/{project}/upload`**

Send `multipart/form-data`. The only required part is `file`. All other fields are optional form fields. The same endpoint handles images, video, and EPS — the asset type is detected from the file's content type.

### Form fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | Yes | The asset to upload |
| `name` | string | No | Display name |
| `description` | string | No | Description |
| `caption` | string | No | Caption |
| `category` | string | No | Category |
| `keywords` | string | No | Comma-separated keywords |
| `author` | string | No | Author |
| `folder` | string | No | Folder path to file the asset under |
| `stripExif` | bool | No | Strip EXIF metadata from the stored original |

### Example

```bash
curl -X POST https://img.pixault.io/api/myapp/upload \
  -H "X-Api-Key: your-api-key" \
  -F "file=@photo.jpg" \
  -F "name=Team photo from company retreat" \
  -F "keywords=team,retreat,2025" \
  -F "folder=events/2025" \
  -F "stripExif=true"
```

### Response `200 OK`

```json
{
  "imageId": "img_01JKXYZ123",
  "url": "/myapp/img_01JKXYZ123",
  "width": 4000,
  "height": 3000,
  "size": 2456789,
  "isVideo": false,
  "isEps": false,
  "duration": null,
  "thumbnailId": null,
  "processingJobId": null
}
```

`imageId` is prefixed by asset type: `img_` for images, `vid_` for video, `eps_` for EPS. For video, `isVideo` is `true`, `duration` is populated, and `thumbnailId` references the auto-generated poster frame. For EPS, `isEps` is `true` and `processingJobId` is set while the asset is processed asynchronously (see below).

## Supported Formats & Size Limits

| Type | Formats | MIME types | Max size |
|------|---------|------------|----------|
| Image | JPEG, PNG, WebP, GIF, AVIF, SVG | `image/jpeg`, `image/png`, `image/webp`, `image/gif`, `image/avif`, `image/svg+xml` | 20 MB |
| Video | MP4, WebM, MOV | `video/mp4`, `video/webm`, `video/quicktime` | 100 MB |
| EPS/PostScript | EPS, PostScript | `application/postscript`, `application/eps`, `image/x-eps`, `image/eps` | 50 MB |

Video uploads use the same `POST /api/{project}/upload` endpoint — there is no separate video route. Pixault auto-generates a thumbnail and serves video with HTTP range streaming.

### EPS / PostScript (asynchronous)

EPS uploads return immediately with a `processingJobId`. The asset is rasterized and analyzed in the background. Once processing completes you can:

- `POST /api/{project}/{imageId}/split` — split a multi-design EPS into separate assets
- `POST /api/{project}/{imageId}/extract-svg` — extract vector SVG output
- `GET /api/{project}/{imageId}/derived` — list derived assets
- `GET /api/{project}/{imageId}/processing-status` — poll processing state

## Other Endpoints

### List images

**`GET /api/{project}/images`**

Paginated list and search.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `limit` | int | 50 | Results per page |
| `cursor` | string | — | Pagination cursor from previous response |
| `search` | string | — | Full-text search |
| `category` | string | — | Filter by category |
| `keyword` | string | — | Filter by keyword |
| `author` | string | — | Filter by author |
| `isVideo` | bool | — | Filter to (or away from) video assets |
| `folder` | string | — | Filter by folder |
| `includeDerived` | bool | — | Include derived assets (e.g. EPS outputs) |

Returns `{ "images": [...], "nextCursor": ..., "totalCount": ... }`.

### Get metadata

**`GET /api/{project}/{imageId}/metadata`** — returns the full metadata document for an asset.

### Update metadata

**`PATCH /api/{project}/{imageId}/metadata`** — update metadata in place; send any subset of the editable fields (`name`, `description`, `caption`, `category`, `folder`, `keywords`, `author`, `copyrightHolder`, `copyrightYear`, `license`, `dateCreated`, `datePublished`, location fields, `tags`, …). Returns the updated metadata.

### Delete an asset

**`DELETE /api/{project}/{imageId}`** — permanently deletes the asset and all its cached variants.
