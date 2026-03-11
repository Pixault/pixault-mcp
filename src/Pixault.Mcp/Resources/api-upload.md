# Upload API

Upload images to Pixault via multipart form POST. All upload endpoints require API key authentication.

## Authentication

Include your credentials in every request:

```
X-Client-Id: px_cl_your_client_id
X-Client-Secret: pk_your_secret_key
```

Legacy single-key authentication is also supported:

```
X-Api-Key: pk_your_api_key
```

## Upload an Image

**`POST /api/{project}/images`**

Upload a single image with optional metadata.

### Request

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | Yes | Image file (JPEG, PNG, WebP, AVIF, GIF, SVG) |
| `alt` | string | No | Alt text description |
| `tags` | string[] | No | Searchable tags |
| `title` | string | No | Image title |
| `description` | string | No | Image description |
| `customId` | string | No | Your own identifier for the image |

### Example

```bash
curl -X POST https://img.pixault.io/api/myapp/images \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456" \
  -F "file=@photo.jpg" \
  -F "alt=Team photo from company retreat" \
  -F "tags=team,retreat,2025"
```

### Response `201 Created`

```json
{
  "id": "img_01JKXYZ123",
  "project": "myapp",
  "filename": "photo.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 2456789,
  "width": 4000,
  "height": 3000,
  "alt": "Team photo from company retreat",
  "tags": ["team", "retreat", "2025"],
  "createdAt": "2025-03-15T10:30:00Z",
  "url": "https://img.pixault.io/myapp/img_01JKXYZ123/original.jpg"
}
```

## List Images

**`GET /api/{project}/images`**

Retrieve a paginated list of images.

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `limit` | int | 20 | Results per page (1–100) |
| `cursor` | string | — | Pagination cursor from previous response |
| `tag` | string | — | Filter by tag |
| `search` | string | — | Search in alt text and tags |

### Example

```bash
curl https://img.pixault.io/api/myapp/images?limit=10&tag=nature \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456"
```

### Response `200 OK`

```json
{
  "images": [
    {
      "id": "img_01JKXYZ123",
      "filename": "sunset.jpg",
      "contentType": "image/jpeg",
      "sizeBytes": 1234567,
      "width": 3000,
      "height": 2000,
      "alt": "Golden sunset over the ocean",
      "tags": ["nature", "sunset"],
      "createdAt": "2025-03-15T10:30:00Z"
    }
  ],
  "cursor": "eyJpZCI6Imlt...",
  "hasMore": true
}
```

## Get Image Metadata

**`GET /api/{project}/images/{imageId}`**

Returns the full metadata for a single image.

### Response `200 OK`

```json
{
  "id": "img_01JKXYZ123",
  "project": "myapp",
  "filename": "photo.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 2456789,
  "width": 4000,
  "height": 3000,
  "alt": "Team photo from company retreat",
  "tags": ["team", "retreat"],
  "title": "Company Retreat 2025",
  "description": "Annual team photo at mountain lodge",
  "createdAt": "2025-03-15T10:30:00Z",
  "updatedAt": "2025-03-15T11:00:00Z"
}
```

## Update Image Metadata

**`PATCH /api/{project}/images/{imageId}`**

Update metadata without re-uploading the image.

### Request Body (JSON)

```json
{
  "alt": "Updated alt text",
  "tags": ["updated", "tags"],
  "title": "New Title"
}
```

### Response `200 OK`

Returns the updated image metadata.

## Delete an Image

**`DELETE /api/{project}/images/{imageId}`**

Permanently delete an image and all its cached variants.

### Response `204 No Content`

No body returned.

## Supported Formats

| Format | MIME Type | Upload | Delivery |
|--------|-----------|--------|----------|
| JPEG | `image/jpeg` | Yes | Yes |
| PNG | `image/png` | Yes | Yes |
| WebP | `image/webp` | Yes | Yes |
| AVIF | `image/avif` | Yes | Yes |
| GIF | `image/gif` | Yes | Yes |
| SVG | `image/svg+xml` | Yes | Yes (sanitized) |

### SVG Handling

SVGs are sanitized on upload to remove potentially dangerous elements (scripts, external references). SVGs can be served as-is or rasterized to bitmap format via transform parameters.

## Video Upload

**`POST /api/{project}/videos`**

Upload video files. Pixault auto-generates thumbnail frames.

| Format | Supported |
|--------|-----------|
| MP4 | Yes |
| WebM | Yes |
| MOV | Yes |

Videos support range-request streaming for playback.

## Rate Limits

Upload endpoints use per-API-key token bucket rate limiting:

| Limit | Value |
|-------|-------|
| Requests per minute | 100 per API key |
| Queue depth | 10 |

## Storage Quotas

Uploads are subject to your plan's storage limits. If you exceed your storage quota:

- **Paid plans**: Overages accrue at your plan's overage rate
- **Trial/Free**: Uploads are rejected with `413 Payload Too Large`

Check your current usage in the <a href="billing-and-plans.md">billing dashboard</a>.
