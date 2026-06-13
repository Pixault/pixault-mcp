# .NET SDK

`Pixault.Client` (v1.3.0) is the official .NET SDK for Pixault. It provides a fluent URL builder for image and video delivery, responsive `<img>`/`<picture>` embeds, uploads, and a full admin client for metadata, transforms, watermarks, folders, and EXIF management.

All API requests authenticate with a single `X-Api-Key` header — there is no client-id/secret pair. The SDK wires this for you from `PixaultOptions.ApiKey`.

## Installation

```bash
dotnet add package Pixault.Client
```

## Registration (Dependency Injection)

Call `AddPixault` in your service configuration. It registers `PixaultImageService` (singleton) plus typed `HttpClient`s for `PixaultUploadClient` and `PixaultAdminClient`, attaching the `X-Api-Key` header automatically.

```csharp
builder.Services.AddPixault(options =>
{
    options.BaseUrl       = "https://img.pixault.io"; // API host (upload/admin HTTP calls)
    options.CdnUrl        = "https://img.pixault.io"; // public delivery host for <img src>; falls back to BaseUrl
    options.DefaultProject = "myapp";                 // used by single-arg For(imageId) and admin calls
    options.ApiKey        = builder.Configuration["Pixault:ApiKey"];
    options.HmacSecret    = builder.Configuration["Pixault:HmacSecret"]; // optional, for signed originals
});
```

`BaseUrl` is used by the upload/admin HTTP clients. `CdnUrl` is used by `PixaultImageService` to build browser-facing delivery URLs; if unset it falls back to `BaseUrl`. Delivery URLs are public and have **no** `/api` prefix.

Then inject the services:

```csharp
public sealed class GalleryService(
    PixaultImageService images,
    PixaultUploadClient uploads,
    PixaultAdminClient admin)
{
    // ...
}
```

## Building Image URLs

`PixaultImageService` builds delivery URLs with no HTTP call. Start a builder with `For(project, imageId)` (explicit project) or `For(imageId)` (uses `DefaultProject`), then chain fluent methods and call `Build()`.

```csharp
var url = images.For("myapp", "img_01JKXYZ")
    .Width(800)
    .Height(600)
    .Fit(FitMode.Cover)
    .Quality(85)
    .Format("webp")
    .Build();
// → https://img.pixault.io/myapp/img_01JKXYZ/w_800,h_600,fit_cover,q_85.webp
```

### Builder methods

| Method | Effect |
|--------|--------|
| `.Width(int)` | Adds `w_{n}` |
| `.Height(int)` | Adds `h_{n}` |
| `.Fit(FitMode)` | Adds `fit_{cover\|contain\|fill\|pad}` |
| `.Quality(int)` | Adds `q_{n}` (1–100) |
| `.Blur(int)` | Adds `blur_{n}` |
| `.Watermark(id, WmPosition pos = BottomRight, int opacity = 30)` | Adds `wm_{id},wm_pos_{...},wm_opacity_{n}` |
| `.Format(string)` | Sets the extension, e.g. `"webp"`, `"jpeg"`, `"png"`, `"avif"`, `"auto"`. Default is `webp` |
| `.Transform(string)` | Applies a named transform preset (`t_{name}`) |
| `.Build()` | Returns the URL string (also via `ToString()`) |

`Format` takes a **string**, not an enum. With no parameters, `Build()` emits `/{project}/{imageId}/original.{format}`.

Enums:

- `FitMode { Cover, Contain, Fill, Pad }`
- `WmPosition { TopLeft, TopRight, BottomLeft, BottomRight, Center, Tile }` → `tl, tr, bl, br, c, tile`

### Watermark + Fit + named transform

```csharp
// Apply an explicit watermark with position and opacity:
var watermarked = images.For("myapp", "img_01JKXYZ")
    .Width(1200)
    .Fit(FitMode.Contain)
    .Watermark("logo", WmPosition.BottomRight, opacity: 40)
    .Format("webp")
    .Build();
// → https://img.pixault.io/myapp/img_01JKXYZ/w_1200,fit_contain,wm_logo,wm_pos_br,wm_opacity_40.webp

// Or use a named transform — locked params (e.g. a mandatory watermark)
// are enforced server-side and cannot be stripped from the URL:
var gallery = images.For("myapp", "img_01JKXYZ")
    .Transform("gallery")
    .Width(800)            // overrides unlocked preset values
    .Build();
// → https://img.pixault.io/myapp/img_01JKXYZ/t_gallery,w_800.webp
```

### LQIP placeholder

```csharp
var placeholder = images.For("myapp", "img_01JKXYZ")
    .Width(40).Quality(20).Blur(10)
    .Format("webp")
    .Build();
```

## Responsive Embeds

`ToImgTag` and `ToPictureTag` generate complete responsive HTML from the same builder. Both apply any transform params you've chained, then append per-width sizing.

`ToImgTag` emits a single `<img>` with a `srcset` of `.auto` URLs — the browser picks the width, and the CDN negotiates the best format (AVIF/WebP/…) from the `Accept` header (`Vary: Accept`).

```csharp
var html = images.For("myapp", "img_01JKXYZ")
    .ToImgTag(
        alt: "Team photo",
        widths: [400, 800, 1200],
        sizes: "(max-width: 768px) 100vw, 800px",
        loading: "lazy",
        cssClass: "hero");
// <img src=".../w_1200.auto" srcset=".../w_400.auto 400w, .../w_800.auto 800w, .../w_1200.auto 1200w"
//      sizes="..." alt="Team photo" width="1200" loading="lazy" decoding="async" class="hero">
```

`ToPictureTag` emits a `<picture>` with explicit AVIF and WebP `<source>` srcsets and a JPEG `<img>` fallback:

```csharp
var picture = images.For("myapp", "img_01JKXYZ")
    .Fit(FitMode.Cover)
    .ToPictureTag(alt: "Team photo", widths: [400, 800, 1200], sizes: "100vw");
```

Both default to `widths: [400, 800, 1200]`, `sizes: "100vw"`, and `loading: "lazy"` when omitted.

## Uploading

`UploadAsync(project, fileName, stream, contentType, folder?, ct)` posts multipart form data and returns an `UploadResponse`. The same endpoint accepts images, video (content-type sniffed — there is no separate video route), and EPS/PostScript.

```csharp
await using var stream = File.OpenRead("photo.jpg");
UploadResponse result = await uploads.UploadAsync(
    project: "myapp",
    fileName: "photo.jpg",
    data: stream,
    contentType: "image/jpeg",
    folder: "events/2026"); // optional

Console.WriteLine(result.ImageId);          // "img_…" (or "vid_…" / "eps_…")
Console.WriteLine(result.Url);              // "/myapp/img_…"
Console.WriteLine(result.IsEps);
Console.WriteLine(result.ProcessingJobId);  // Guid? — set for async EPS/video processing
```

`PixaultUploadClient` also exposes `DeleteAsync(project, imageId, ct)`.

## Admin Operations

`PixaultAdminClient` covers metadata, listing/search, transforms, watermarks, folders, EXIF, EPS, and plugins. Every method takes an optional `project` argument; when omitted it uses `DefaultProject`.

### Metadata: get & update

```csharp
ImageMetadataDto? meta = await admin.GetMetadataAsync("img_01JKXYZ");

var updated = await admin.UpdateMetadataAsync("img_01JKXYZ", new MetadataUpdate
{
    Name        = "Summer launch hero",
    Description = "Crowd at the 2026 launch event",
    Category    = "events",
    Author      = "Jane Doe",
    Keywords    = ["launch", "2026", "crowd"],
    Tags        = new() { ["campaign"] = "summer", ["approved"] = "true" }
});
```

`MetadataUpdate` accepts any subset of: `Name, Description, Caption, Category, Folder, Keywords` (string list), `Author, CopyrightHolder, CopyrightYear` (int), `License, DateCreated, DatePublished`, `RepresentativeOfPage` (bool), `LocationLatitude/Longitude` (double), `LocationName, ExifData`, and `Tags` (key→value strings). Both methods return the full `ImageMetadataDto`.

### List & search

```csharp
ImageListResponse page = await admin.ListImagesAsync(
    limit: 20,
    search: "hero",
    category: "events",
    keyword: "launch",
    author: "Jane Doe",
    isVideo: false,
    folder: "events/2026");

foreach (var img in page.Images)
    Console.WriteLine($"{img.ImageId}: {img.Name}");

// Paginate with the returned cursor:
var next = await admin.ListImagesAsync(limit: 20, cursor: page.NextCursor);
```

`ImageListResponse` exposes `Images`, `NextCursor`, and `TotalCount`. Filters are `search`, `category`, `keyword`, `author`, `isVideo`, and `folder` — there is no `tag` filter.

### Delete & EXIF strip

```csharp
await admin.DeleteImageAsync("img_01JKXYZ");

// Strip EXIF (including GPS) from the stored original:
ImageMetadataDto? stripped = await admin.StripExifAsync("img_01JKXYZ");
```

### Named transforms

Transforms are an upsert via `SaveTransformAsync` (the API uses `PUT`). Names must match `^[a-z0-9][a-z0-9\-]{0,31}$` (lowercase alphanumerics and hyphens, ≤32 chars, no underscores).

```csharp
List<NamedTransformDto> transforms = await admin.ListTransformsAsync();
NamedTransformDto? thumb = await admin.GetTransformAsync("thumb");

// Create or update — lock width/height/watermark so clients can't override them:
await admin.SaveTransformAsync("gallery", new NamedTransformSave
{
    Width             = 800,
    Height            = 800,
    FitMode           = "cover",
    Quality           = 82,
    WatermarkId       = "logo",
    WatermarkPosition = "br",
    WatermarkOpacity  = 30,
    LockedParameters  = ["width", "height", "watermarkId", "watermarkPosition", "watermarkOpacity"]
});

await admin.DeleteTransformAsync("gallery");
```

`FitMode`/`WatermarkPosition` on the DTO are strings (`cover|contain|fill|pad`, `tl|tr|bl|br|c|tile`). Lockable parameter names: `width, height, fitMode, quality, blur, watermarkId, watermarkPosition, watermarkOpacity`.

### Watermarks

```csharp
List<WatermarkDto> marks = await admin.ListWatermarksAsync();

await using var logo = File.OpenRead("logo.png");
WatermarkDto? saved = await admin.UploadWatermarkAsync("logo", logo, contentType: "image/png");

await admin.DeleteWatermarkAsync("logo");
```

### Folders

```csharp
List<string> folders = await admin.ListFoldersAsync();
await admin.CreateFolderAsync("events/2026");
await admin.DeleteFolderAsync("events/2026");
```

### EPS / PostScript

```csharp
await admin.SplitEpsDesignsAsync("eps_01JKXYZ");      // split multi-design EPS
await admin.ExtractEpsSvgAsync("eps_01JKXYZ");        // extract vector SVG
var derived = await admin.GetDerivedAssetsAsync("eps_01JKXYZ");
var status  = await admin.GetEpsProcessingStatusAsync("eps_01JKXYZ");
```

### Plugins

```csharp
var all      = await admin.GetAllPluginsAsync();                 // marketplace
var enabled  = await admin.GetProjectPluginsAsync();             // for DefaultProject
await admin.ActivatePluginAsync("myapp", "background-removal");
await admin.DeactivatePluginAsync("myapp", "background-removal");
```

## Video URLs

`VideoUrl(project, videoId, contentType)` returns a range-streamable delivery URL with the extension derived from the content type (`video/mp4` → `mp4`, `video/webm` → `webm`, `video/quicktime` → `mov`):

```csharp
var src = images.VideoUrl("myapp", "vid_01JKXYZ", "video/mp4");
// → https://img.pixault.io/myapp/vid_01JKXYZ/video.mp4
```

## Signed (Protected) Originals

When a project has an HMAC secret configured, requests for `original.{format}` require `sig` and `exp` query parameters; the signature is computed over the delivery path `/{project}/{imageId}/original.{format}`. Set `options.HmacSecret` so the SDK can sign these original-download URLs. Transformed delivery URLs are public and CDN-cached (`Cache-Control: public, max-age=2592000, immutable`, 30 days).

## Error Handling

The client does not define a custom exception type. Upload and admin methods call `EnsureSuccessStatusCode()`, so failed requests throw `HttpRequestException`. Inspect `StatusCode` to branch:

```csharp
try
{
    await uploads.UploadAsync("myapp", "photo.jpg", stream, "image/jpeg");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
{
    Console.WriteLine("File exceeds the size limit (images 20MB, video 100MB, EPS 50MB).");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    Console.WriteLine("Rate limited — retry later.");
}
```

`GetMetadataAsync` and `GetTransformAsync` are the exceptions: they swallow `404` and return `null`.

## Blazor Integration

The Pixault.Blazor component library provides ready-to-use components. See the <a href="integration-blazor.md">Blazor Integration Guide</a> for details.
