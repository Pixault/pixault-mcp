# Image Metadata

Every image, video, and EPS asset in Pixault carries structured metadata aligned with [Schema.org ImageObject](https://schema.org/ImageObject), so your assets are search-engine indexable, geo-taggable, and programmatically manageable. This page covers the HTTP metadata API.

## Authentication

All API requests authenticate with a single header:

```
X-Api-Key: <your-api-key>
```

There is no client-id/client-secret pair.

## Get Metadata

```
GET /api/{project}/{imageId}/metadata
```

Returns the full `ImageMetadataDto` for an asset.

```bash
curl https://img.pixault.io/api/myapp/img_01JKXYZ/metadata \
  -H "X-Api-Key: <your-api-key>"
```

```json
{
  "imageId": "img_01JKXYZ",
  "projectId": "myapp",
  "originalFileName": "team-photo.jpg",
  "uploadedAt": "2026-03-15T10:30:00Z",
  "contentType": "image/jpeg",
  "sizeBytes": 2411724,
  "width": 4000,
  "height": 3000,
  "name": "Team Photo 2026",
  "description": "Annual team photo at the mountain lodge",
  "caption": "The whole crew at the spring retreat",
  "category": "events",
  "folder": "retreats/2026",
  "keywords": ["team", "retreat", "mountain"],
  "author": "Jane Doe",
  "copyrightHolder": "Acme Inc.",
  "copyrightYear": 2026,
  "license": "https://creativecommons.org/licenses/by/4.0/",
  "dateCreated": "2026-03-15T09:00:00Z",
  "datePublished": "2026-03-20T00:00:00Z",
  "dateModified": "2026-03-21T14:02:00Z",
  "representativeOfPage": true,
  "exifData": {
    "Make": "Canon",
    "Model": "EOS R5",
    "FNumber": "2.8"
  },
  "locationLatitude": 39.6403,
  "locationLongitude": -106.3742,
  "locationName": "Vail, Colorado",
  "tags": {
    "campaign": "spring-2026",
    "internalRef": "HR-4417"
  },
  "isVideo": false,
  "duration": null,
  "hasAudio": false,
  "thumbnailId": null,
  "isEps": false,
  "sourceAssetId": null,
  "derivationType": null
}
```

### Field notes

- **Schema.org / SEO fields** — `name`, `description`, `caption`, `category`, `keywords` (string array), `author`, `copyrightHolder`, `copyrightYear`, `license`, `dateCreated`, `datePublished`, `dateModified`, `representativeOfPage`. These map directly to Schema.org `ImageObject` properties and are surfaced in the JSON-LD output (see below) for rich search-engine indexing.
- **Geo-location** — `locationLatitude`, `locationLongitude` (doubles), `locationName`.
- **`folder`** — the asset's folder path (see the Folders API).
- **`tags`** — an **object** of custom key→value strings (e.g. your own metadata), not a list of labels. To free-text search labels, use the `keywords` field instead.
- **`exifData`** — extracted EXIF as a key→value object.
- **Video / EPS fields** — `isVideo`, `duration`, `hasAudio`, `thumbnailId`, `isEps`, `sourceAssetId`, `derivationType`.

## Update Metadata

```
PATCH /api/{project}/{imageId}/metadata
```

Send any subset of the writable fields. Only provided fields are updated; omitted fields are left unchanged. The updated metadata is returned.

Writable fields: `name`, `description`, `caption`, `category`, `folder`, `keywords` (string array), `author`, `copyrightHolder`, `copyrightYear` (int), `license`, `dateCreated`, `datePublished` (ISO 8601), `representativeOfPage` (bool), `locationLatitude`, `locationLongitude` (double), `locationName`, `exifData` (object), `tags` (object of key→value strings).

```bash
curl -X PATCH https://img.pixault.io/api/myapp/img_01JKXYZ/metadata \
  -H "X-Api-Key: <your-api-key>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Team Photo 2026",
    "caption": "The whole crew at the spring retreat",
    "category": "events",
    "keywords": ["team", "retreat", "mountain"],
    "author": "Jane Doe",
    "copyrightHolder": "Acme Inc.",
    "copyrightYear": 2026,
    "license": "https://creativecommons.org/licenses/by/4.0/",
    "representativeOfPage": true,
    "locationLatitude": 39.6403,
    "locationLongitude": -106.3742,
    "locationName": "Vail, Colorado",
    "tags": { "campaign": "spring-2026", "internalRef": "HR-4417" }
  }'
```

## JSON-LD

```
GET /api/{project}/{imageId}/metadata/jsonld
```

Returns a Schema.org `ImageObject` as a JSON-LD document, built from the asset's metadata. This is a dedicated endpoint — there is no `?format=jsonld` query parameter.

```bash
curl https://img.pixault.io/api/myapp/img_01JKXYZ/metadata/jsonld \
  -H "X-Api-Key: <your-api-key>"
```

```json
{
  "@context": "https://schema.org",
  "@type": "ImageObject",
  "contentUrl": "https://img.pixault.io/myapp/img_01JKXYZ/original.jpg",
  "thumbnailUrl": "https://img.pixault.io/myapp/img_01JKXYZ/w_200,h_200,fit_cover.webp",
  "name": "Team Photo 2026",
  "description": "Annual team photo at the mountain lodge",
  "width": { "@type": "QuantitativeValue", "value": 4000, "unitCode": "E37" },
  "height": { "@type": "QuantitativeValue", "value": 3000, "unitCode": "E37" },
  "encodingFormat": "image/jpeg",
  "uploadDate": "2026-03-15T10:30:00Z"
}
```

Paste the document into a `<script type="application/ld+json">` block in your page `<head>` so search engines index the image with its metadata.

## Strip EXIF

```
POST /api/{project}/{imageId}/strip-exif
```

Removes EXIF metadata (camera info, embedded GPS, timestamps) from the stored original. Use this before publishing user-supplied images to avoid leaking location or device data.

```bash
curl -X POST https://img.pixault.io/api/myapp/img_01JKXYZ/strip-exif \
  -H "X-Api-Key: <your-api-key>"
```

EXIF can also be stripped at upload time by sending `stripExif=true` on the upload request.

## Searching by Metadata

Metadata is searchable through the list endpoint, `GET /api/{project}/images`. There is no `tag` query parameter; use these filters instead:

```bash
# Free-text search across name, description, keywords, etc.
curl "https://img.pixault.io/api/myapp/images?search=sunset" \
  -H "X-Api-Key: <your-api-key>"

# Filter by category, keyword, author, folder, or media type
curl "https://img.pixault.io/api/myapp/images?category=events&author=Jane%20Doe&isVideo=false" \
  -H "X-Api-Key: <your-api-key>"
```

Supported filters: `search`, `category`, `keyword`, `author`, `isVideo`, `folder` (plus `limit`, `cursor`, `includeDerived` for paging).
