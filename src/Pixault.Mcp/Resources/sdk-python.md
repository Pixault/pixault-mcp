# Python SDK

The Pixault Python SDK provides image URL construction, uploads, and management. Available as `pixault` on PyPI.

## Installation

```bash
pip install pixault
```

Requires Python 3.9+.

## Configuration

```python
from pixault import Pixault

px = Pixault(
    client_id="px_cl_your_client_id",
    client_secret="pk_your_secret_key",
    base_url="https://img.pixault.io",  # optional default
    project="myapp",
)
```

## URL Builder

Generate transform URLs without making network requests:

```python
url = (px.url("img_01JKXYZ")
    .width(800)
    .height(600)
    .fit("cover")
    .quality(85)
    .format("webp")
    .build())
# → "https://img.pixault.io/myapp/img_01JKXYZ/w_800,h_600,fit_cover,q_85.webp"
```

### Methods

| Method | Description |
|--------|-------------|
| `.width(n)` | Width (1–4096) |
| `.height(n)` | Height (1–4096) |
| `.fit(mode)` | `"cover"`, `"contain"`, `"fill"`, `"pad"` |
| `.quality(n)` | Output quality (1–100) |
| `.blur(n)` | Gaussian blur (1–100) |
| `.watermark(id)` | Watermark overlay ID |
| `.watermark_position(pos)` | `"tl"`, `"tr"`, `"bl"`, `"br"`, `"c"`, `"tile"` |
| `.watermark_opacity(n)` | Opacity (1–100) |
| `.transform(name)` | Named transform preset |
| `.format(fmt)` | `"jpg"`, `"png"`, `"webp"`, `"avif"` |
| `.build()` | Returns URL string |

### Named Transforms

```python
url = (px.url("img_01JKXYZ")
    .transform("thumbnail")
    .format("webp")
    .build())
```

### LQIP Placeholder

```python
placeholder = (px.url("img_01JKXYZ")
    .width(40).quality(20).blur(10)
    .format("webp")
    .build())
```

## Upload

```python
# Upload from file path
image = px.upload("photo.jpg", alt="Team photo", tags=["team", "retreat"])

print(image["id"])   # "img_01JKXYZ123"
print(image["url"])  # "https://img.pixault.io/myapp/img_01JKXYZ123/original.jpg"

# Upload from file-like object
with open("photo.jpg", "rb") as f:
    image = px.upload_stream(f, filename="photo.jpg", alt="From stream")

# Upload with all metadata
image = px.upload(
    "photo.jpg",
    alt="Sunset over the ocean",
    title="Ocean Sunset",
    description="Captured at Malibu Beach during golden hour",
    tags=["nature", "sunset", "ocean"],
    custom_id="my-app-image-42",
)
```

## Image Management

```python
# List images
result = px.list_images(limit=20, tag="nature")
for img in result["images"]:
    print(f"{img['id']}: {img['alt']}")

# Paginate
while result["hasMore"]:
    result = px.list_images(cursor=result["cursor"])
    for img in result["images"]:
        print(img["id"])

# Get metadata
metadata = px.get_image("img_01JKXYZ")

# Update metadata
px.update_image("img_01JKXYZ", alt="Updated text", tags=["new", "tags"])

# Delete
px.delete_image("img_01JKXYZ")
```

## List & Search Images

```python
# List all images
result = px.list_images("my-project")

# Search by text
matches = px.list_images("my-project", search="hero")

# Filter by category
tattoos = px.list_images("my-project", category="tattoo-flash")

# Paginate
page2 = px.list_images("my-project", cursor=result["nextCursor"])
```

## Named Transforms

```python
# Create
px.create_transform("thumbnail", {
    "parameters": {"w": 200, "h": 200, "fit": "cover", "q": 80},
    "locked": ["w", "h", "fit"],
})

# List
transforms = px.list_transforms()
for t in transforms["transforms"]:
    print(f"{t['name']}: {t['parameters']}")

# Delete
px.delete_transform("thumbnail")
```

## Django Integration

```python
# settings.py
PIXAULT_CLIENT_ID = os.environ.get("PIXAULT_CLIENT_ID")
PIXAULT_CLIENT_SECRET = os.environ.get("PIXAULT_CLIENT_SECRET")
PIXAULT_PROJECT = os.environ.get("PIXAULT_PROJECT", "default")

# views.py
from pixault import Pixault
from django.conf import settings

def get_pixault():
    return Pixault(
        client_id=settings.PIXAULT_CLIENT_ID,
        client_secret=settings.PIXAULT_CLIENT_SECRET,
        project=settings.PIXAULT_PROJECT,
    )

def upload_image(request):
    px = get_pixault()
    file = request.FILES["image"]
    image = px.upload_stream(
        file, filename=file.name,
        alt=request.POST.get("alt", ""),
    )
    return JsonResponse(image, status=201)
```

## FastAPI Integration

```python
from fastapi import FastAPI, UploadFile
from pixault import Pixault

app = FastAPI()
px = Pixault(
    client_id="px_cl_abc123",
    client_secret="pk_secret456",
    project="myapp",
)

@app.post("/upload")
async def upload(file: UploadFile):
    content = await file.read()
    image = px.upload_bytes(content, filename=file.filename)
    return image
```

## Async Support

```python
from pixault import AsyncPixault

px = AsyncPixault(
    client_id="px_cl_abc123",
    client_secret="pk_secret456",
    project="myapp",
)

image = await px.upload("photo.jpg", alt="Async upload")
images = await px.list_images(limit=10)
```

## Error Handling

```python
from pixault.exceptions import (
    PixaultApiError,
    QuotaExceededError,
    RateLimitError,
)

try:
    px.upload("large_file.jpg")
except QuotaExceededError:
    print("Storage quota exceeded")
except RateLimitError as e:
    print(f"Rate limited. Retry after {e.retry_after}s")
except PixaultApiError as e:
    print(f"API error ({e.status_code}): {e.message}")
```
