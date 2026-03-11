# Image Metadata

Every image in Pixault carries structured metadata aligned with [Schema.org ImageObject](https://schema.org/ImageObject). This enables rich search engine indexing, accessibility, and programmatic image management.

## Metadata Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique image identifier (ULID format, e.g., `img_01JKXYZ`) |
| `project` | string | Project this image belongs to |
| `filename` | string | Original filename |
| `contentType` | string | MIME type (e.g., `image/jpeg`) |
| `sizeBytes` | long | File size in bytes |
| `width` | int | Image width in pixels |
| `height` | int | Image height in pixels |
| `alt` | string | Alt text for accessibility |
| `title` | string | Image title |
| `description` | string | Longer description |
| `tags` | string[] | Searchable tags |
| `customId` | string | Your own external identifier |
| `createdAt` | datetime | Upload timestamp (UTC) |
| `updatedAt` | datetime | Last modification timestamp (UTC) |

## JSON-LD Output

Pixault can return metadata as JSON-LD for embedding in HTML pages:

```bash
curl https://img.pixault.io/api/myapp/images/img_01JKXYZ?format=jsonld \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456"
```

```json
{
  "@context": "https://schema.org",
  "@type": "ImageObject",
  "contentUrl": "https://img.pixault.io/myapp/img_01JKXYZ/original.jpg",
  "thumbnailUrl": "https://img.pixault.io/myapp/img_01JKXYZ/w_200,h_200,fit_cover.webp",
  "name": "Team Photo 2025",
  "description": "Annual team photo at the mountain lodge",
  "width": { "@type": "QuantitativeValue", "value": 4000, "unitCode": "E37" },
  "height": { "@type": "QuantitativeValue", "value": 3000, "unitCode": "E37" },
  "encodingFormat": "image/jpeg",
  "contentSize": "2.3 MB",
  "uploadDate": "2025-03-15T10:30:00Z"
}
```

Embed this in your HTML `<head>`:

```html
<script type="application/ld+json">
  <!-- paste JSON-LD here -->
</script>
```

## Updating Metadata

Use `PATCH` to update metadata fields without re-uploading:

```bash
curl -X PATCH https://img.pixault.io/api/myapp/images/img_01JKXYZ \
  -H "X-Client-Id: px_cl_abc123" \
  -H "X-Client-Secret: pk_secret456" \
  -H "Content-Type: application/json" \
  -d '{
    "alt": "Updated alt text for better accessibility",
    "tags": ["team", "retreat", "mountain"]
  }'
```

Only provided fields are updated; omitted fields remain unchanged.

## Searching by Metadata

Use query parameters on the list endpoint to search:

```bash
# Search by tag
curl "https://img.pixault.io/api/myapp/images?tag=nature"

# Search in alt text and tags
curl "https://img.pixault.io/api/myapp/images?search=sunset"
```

## Video Metadata

Video files include additional metadata:

| Field | Type | Description |
|-------|------|-------------|
| `duration` | float | Duration in seconds |
| `thumbnailUrl` | string | Auto-generated thumbnail URL |
| `codec` | string | Video codec (e.g., `h264`) |

## Best Practices

1. **Always set alt text** — Improves accessibility and SEO
2. **Use consistent tags** — Establish a tagging taxonomy for your application
3. **Leverage customId** — Map Pixault images to your own database records
4. **Embed JSON-LD** — Search engines index your images with rich metadata
5. **Keep titles concise** — Titles appear in image search results and previews
