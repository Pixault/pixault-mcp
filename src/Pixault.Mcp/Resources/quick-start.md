# Quick Start

Get your first image uploaded and served through Pixault in under 5 minutes.

## Prerequisites

- A Pixault account at [pixault.io](https://pixault.io)
- An API key (client ID + client secret) from the dashboard

## Step 1: Get Your API Credentials

1. Sign in at [pixault.io](https://pixault.io)
2. Go to **Billing** → **API Keys**
3. Click **Create API Key**
4. Save the **Client ID** (`px_cl_...`) and **Client Secret** (`pk_...`) — the secret is only shown once

## Step 2: Upload an Image

```bash
curl -X POST https://img.pixault.io/api/myapp/images \
  -H "X-Client-Id: px_cl_your_client_id" \
  -H "X-Client-Secret: pk_your_secret_key" \
  -F "file=@photo.jpg" \
  -F "alt=A beautiful sunset" \
  -F "tags=nature,sunset"
```

Response:

```json
{
  "id": "img_01JKXYZ123",
  "project": "myapp",
  "filename": "photo.jpg",
  "contentType": "image/jpeg",
  "sizeBytes": 2456789,
  "width": 4000,
  "height": 3000,
  "url": "https://img.pixault.io/myapp/img_01JKXYZ123/original.jpg"
}
```

## Step 3: Serve a Transformed Image

Use URL parameters to transform on-the-fly:

```
# Thumbnail (200x200, cover crop, WebP)
https://img.pixault.io/myapp/img_01JKXYZ123/w_200,h_200,fit_cover.webp

# Gallery size with quality
https://img.pixault.io/myapp/img_01JKXYZ123/w_800,q_85.webp

# Blurred placeholder (LQIP)
https://img.pixault.io/myapp/img_01JKXYZ123/w_40,q_20,blur_10.webp
```

## Step 4: Use an SDK (Optional)

### JavaScript

```javascript
import { Pixault } from '@pixault/sdk';

const px = new Pixault({
  clientId: 'px_cl_your_client_id',
  clientSecret: 'pk_your_secret_key',
  project: 'myapp',
});

// Upload
const image = await px.upload(file, {
  alt: 'A beautiful sunset',
  tags: ['nature', 'sunset'],
});

// Build transform URL
const url = px.url(image.id)
  .width(800)
  .height(600)
  .fit('cover')
  .format('webp')
  .build();
```

### .NET

```csharp
// Register in DI
builder.Services.AddPixault(options =>
{
    options.BaseUrl = "https://img.pixault.io";
    options.DefaultProject = "myapp";
});

// Upload
var result = await uploadClient.UploadAsync(stream, "photo.jpg",
    alt: "A beautiful sunset", tags: ["nature", "sunset"]);

// Build transform URL
var url = imageService.Url(result.Id)
    .Width(800).Height(600)
    .Fit(FitMode.Cover)
    .Format(OutputFormat.WebP)
    .Build();
```

### PHP

```php
$pixault = new Pixault([
    'client_id' => 'px_cl_your_client_id',
    'client_secret' => 'pk_your_secret_key',
    'base_url' => 'https://img.pixault.io',
    'project' => 'myapp',
]);

// Upload
$image = $pixault->upload('/path/to/photo.jpg', [
    'alt' => 'A beautiful sunset',
    'tags' => ['nature', 'sunset'],
]);

// Transform URL
$url = $pixault->url($image['id'])
    ->width(800)->height(600)
    ->fit('cover')->format('webp')
    ->build();
```

### Python

```python
from pixault import Pixault

px = Pixault(
    client_id="px_cl_your_client_id",
    client_secret="pk_your_secret_key",
    project="myapp",
)

# Upload
image = px.upload("photo.jpg", alt="A beautiful sunset", tags=["nature", "sunset"])

# Transform URL
url = (px.url(image["id"])
    .width(800).height(600)
    .fit("cover").format("webp")
    .build())
```

## What's Next?

- <a href="api-image-delivery.md">Image Delivery API</a> — Full URL scheme and transform parameter reference
- <a href="api-upload.md">Upload API</a> — Multi-file upload, metadata, tagging
- <a href="api-transforms.md">Named Transforms</a> — Create reusable transform presets
- <a href="billing-and-plans.md">Billing & Plans</a> — Plan features and usage limits
