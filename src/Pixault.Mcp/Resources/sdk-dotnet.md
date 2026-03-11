# .NET SDK

The Pixault .NET SDK provides a fluent API for image URL construction, uploads, and management. Available as `Pixault.Client` on NuGet.

## Installation

```bash
dotnet add package Pixault.Client
```

## Configuration

### ASP.NET Core (Dependency Injection)

```csharp
builder.Services.AddPixault(options =>
{
    options.BaseUrl = "https://img.pixault.io";
    options.DefaultProject = "myapp";
    options.ClientId = "px_cl_your_client_id";
    options.ClientSecret = "pk_your_secret_key";
});
```

### Manual Instantiation

```csharp
var options = new PixaultOptions
{
    BaseUrl = "https://img.pixault.io",
    DefaultProject = "myapp",
    ClientId = "px_cl_your_client_id",
    ClientSecret = "pk_your_secret_key",
};

var imageService = new PixaultImageService(options);
var uploadClient = new PixaultUploadClient(httpClient, options);
var adminClient = new PixaultAdminClient(httpClient, options);
```

## URL Builder

The fluent URL builder generates transform URLs without making HTTP requests:

```csharp
// Inject PixaultImageService
var url = imageService.Url("img_01JKXYZ")
    .Width(800)
    .Height(600)
    .Fit(FitMode.Cover)
    .Quality(85)
    .Format(OutputFormat.WebP)
    .Build();
// → "https://img.pixault.io/myapp/img_01JKXYZ/w_800,h_600,fit_cover,q_85.webp"
```

### Available Methods

| Method | Description |
|--------|-------------|
| `.Width(int)` | Set width (1–4096) |
| `.Height(int)` | Set height (1–4096) |
| `.Fit(FitMode)` | Resize mode: `Cover`, `Contain`, `Fill`, `Pad` |
| `.Quality(int)` | Output quality (1–100) |
| `.Blur(int)` | Gaussian blur radius (1–100) |
| `.Watermark(string)` | Watermark overlay ID |
| `.WatermarkPosition(WatermarkPosition)` | Position: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, `Center`, `Tile` |
| `.WatermarkOpacity(int)` | Opacity (1–100) |
| `.Transform(string)` | Named transform preset |
| `.Format(OutputFormat)` | Output format: `Jpeg`, `Png`, `WebP`, `Avif` |
| `.Project(string)` | Override default project |
| `.Build()` | Generate the URL string |

### Named Transforms

```csharp
var url = imageService.Url("img_01JKXYZ")
    .Transform("thumbnail")
    .Format(OutputFormat.WebP)
    .Build();
```

### LQIP Placeholder

```csharp
var placeholder = imageService.Url("img_01JKXYZ")
    .Width(40).Quality(20).Blur(10)
    .Format(OutputFormat.WebP)
    .Build();
```

## Upload Client

```csharp
// From file stream
await using var stream = File.OpenRead("photo.jpg");
var result = await uploadClient.UploadAsync(
    stream,
    "photo.jpg",
    alt: "Team photo",
    tags: ["team", "2025"]);

Console.WriteLine($"Uploaded: {result.Id}");
Console.WriteLine($"URL: {result.Url}");
```

## Admin Client

```csharp
// List images
var response = await adminClient.ListImagesAsync(limit: 20, tag: "nature");
foreach (var image in response.Images)
{
    Console.WriteLine($"{image.Id}: {image.Alt}");
}

// Get metadata
var metadata = await adminClient.GetImageAsync("img_01JKXYZ");

// Update metadata
await adminClient.UpdateImageAsync("img_01JKXYZ", new
{
    alt = "Updated description",
    tags = new[] { "updated", "tags" }
});

// Delete
await adminClient.DeleteImageAsync("img_01JKXYZ");

// Named transforms
var transforms = await adminClient.ListTransformsAsync();
await adminClient.CreateTransformAsync("thumb", new
{
    parameters = new { w = 200, h = 200, fit = "cover" },
    locked = new[] { "w", "h" }
});
```

## List & Search Images

```csharp
// List all images
var result = await admin.ListImagesAsync(project: "my-project");

// Search by text
var matches = await admin.ListImagesAsync(project: "my-project", search: "hero");

// Filter by category
var tattoos = await admin.ListImagesAsync(project: "my-project", category: "tattoo-flash");

// Paginate
var page2 = await admin.ListImagesAsync(cursor: result.NextCursor, project: "my-project");
```

## Blazor Integration

The Pixault.Blazor component library provides ready-to-use Blazor components. See the <a href="integration-blazor.md">Blazor Integration Guide</a> for details.

## Error Handling

The SDK throws `PixaultApiException` for HTTP errors:

```csharp
try
{
    await uploadClient.UploadAsync(stream, "photo.jpg");
}
catch (PixaultApiException ex) when (ex.StatusCode == 413)
{
    Console.WriteLine("Storage quota exceeded");
}
catch (PixaultApiException ex) when (ex.StatusCode == 429)
{
    Console.WriteLine($"Rate limited. Retry after {ex.RetryAfter} seconds");
}
```
