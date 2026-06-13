# Quick Start

Get your first image uploaded and served through Pixault in under 5 minutes.

## Prerequisites

- A Pixault account at [pixault.io](https://pixault.io)
- An API key from the dashboard

## Step 1: Get Your API Key

1. Sign in at [pixault.io](https://pixault.io)
2. Go to **Billing** → **API Keys**
3. Click **Create API Key**
4. Copy the key and store it securely — it is only shown once

Every API request authenticates with a single header:

```
X-Api-Key: <your-api-key>
```

There is no client ID / client secret pair — just the one key.

## Step 2: Upload an Image

Upload is a `multipart/form-data` POST to `/api/{project}/upload`. The only required part is `file`. Optional form fields include `name`, `description`, `caption`, `category`, `keywords` (comma-separated), `author`, `folder`, and `stripExif`.

```bash
curl -X POST https://img.pixault.io/api/myapp/upload \
  -H "X-Api-Key: your-api-key" \
  -F "file=@photo.jpg" \
  -F "name=A beautiful sunset" \
  -F "keywords=nature,sunset"
```

A successful upload returns **200 OK**:

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

The `imageId` prefix tells you the asset type: `img_` for images, `vid_` for video, `eps_` for EPS/PostScript. Video uploads use this same endpoint — the content type is detected automatically.

Limits: images up to 20 MB, video up to 100 MB, EPS up to 50 MB.

## Step 3: Serve a Transformed Image

Delivery URLs are public (no `/api` prefix) and served through the CDN. The shape is `/{project}/{imageId}/{params}.{format}`, where `params` is a comma-separated list (`w_{n}`, `h_{n}`, `fit_{cover|contain|fill|pad}`, `q_{n}`, `blur_{n}`, and more).

```
# Thumbnail (400x400, cover crop, WebP)
https://img.pixault.io/myapp/img_01JKXYZ123/w_400,h_400,fit_cover.webp

# Gallery size with quality
https://img.pixault.io/myapp/img_01JKXYZ123/w_800,q_85.webp

# Blurred placeholder (LQIP)
https://img.pixault.io/myapp/img_01JKXYZ123/w_40,q_20,blur_10.webp
```

Use the `.auto` extension to let the server pick the best format (AVIF, WebP, etc.) from the request's `Accept` header:

```
https://img.pixault.io/myapp/img_01JKXYZ123/w_800.auto
```

The untransformed original lives at `/{project}/{imageId}/original.{format}`. Transformed assets are returned with `Cache-Control: public, max-age=2592000, immutable` (30 days) plus an ETag.

## Step 4: Use the .NET SDK (Optional)

Install the published package:

```bash
dotnet add package Pixault.Client
```

Register it in DI, supplying your API key:

```csharp
builder.Services.AddPixault(options =>
{
    options.BaseUrl = "https://img.pixault.io";
    options.CdnUrl = "https://img.pixault.io";
    options.DefaultProject = "myapp";
    options.ApiKey = "your-api-key";
});
```

Upload a file with `PixaultUploadClient`:

```csharp
await using var stream = File.OpenRead("photo.jpg");
var result = await uploadClient.UploadAsync(
    "myapp", "photo.jpg", stream, "image/jpeg");

string imageId = result.ImageId;
```

Build a transformed delivery URL with `PixaultImageService`:

```csharp
var url = images.For("myapp", imageId)
    .Width(400)
    .Height(400)
    .Fit(FitMode.Cover)
    .Format("webp")
    .Build();
```

`Format` takes a string (for example `"webp"`). Use `.For(...)` to start a URL — there is no `.Url()` method.

SDKs for JavaScript, Python, and PHP are also available — see their dedicated pages.

## What's Next?

- [Image Delivery API](api-image-delivery.md) — Full URL scheme and transform parameter reference
- [Upload API](api-upload.md) — Upload fields, metadata, and folders
- [Named Transforms](api-transforms.md) — Create reusable transform presets
- [.NET SDK](sdk-dotnet.md) — Full SDK reference
