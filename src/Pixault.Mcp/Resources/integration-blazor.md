# Blazor Integration

Pixault provides a native Blazor component library (`Pixault.Blazor`) with interactive gallery, uploader, image detail, and transform manager components. These work with both Blazor Server and Blazor WebAssembly.

## Installation

```bash
dotnet add package Pixault.Blazor
dotnet add package Pixault.Client
```

## Setup

Register Pixault services in `Program.cs`:

```csharp
builder.Services.AddPixault(options =>
{
    options.BaseUrl = "https://img.pixault.io";
    options.DefaultProject = "myapp";
    options.ClientId = "px_cl_your_client_id";
    options.ClientSecret = "pk_your_secret_key";
});
```

## Components

### PixaultGallery

A searchable, filterable image gallery with pagination and selection:

```razor
<PixaultGallery Project="myapp"
                OnImageSelected="HandleImageSelected" />

@code {
    private void HandleImageSelected(ImageMetadataDto image)
    {
        // Handle selection — show detail view, etc.
    }
}
```

**Features:**
- Grid layout with responsive columns
- Search by alt text and tags
- Tag filtering
- Cursor-based pagination
- Click to select

### PixaultUploader

Drag-and-drop multi-file uploader with progress tracking:

```razor
<PixaultUploader Project="myapp"
                 OnUploadComplete="HandleUploadComplete" />

@code {
    private void HandleUploadComplete(UploadCompleteEventArgs args)
    {
        // args.ImageId — the uploaded image ID
        // args.Url — the original image URL
    }
}
```

**Features:**
- Drag-and-drop zone
- Multi-file support
- Real-time progress bars
- Alt text and tag input per file
- Automatic format detection

### PixaultImageDetail

Display and edit image metadata:

```razor
<PixaultImageDetail Project="myapp"
                    Image="@selectedImage"
                    OnDeleted="HandleDeleted"
                    OnUpdated="HandleUpdated" />

@code {
    private ImageMetadataDto? selectedImage;

    private void HandleDeleted()
    {
        selectedImage = null;
    }

    private void HandleUpdated(ImageMetadataDto updated)
    {
        selectedImage = updated;
    }
}
```

**Features:**
- Image preview with responsive sizing
- Metadata fields (alt, title, tags) with inline editing
- Copy URL buttons for common transforms
- Delete with confirmation
- Video playback support

### PixaultTransformManager

CRUD interface for named transforms:

```razor
<PixaultTransformManager Project="myapp" />
```

**Features:**
- List all named transforms
- Create new transforms with parameter builder
- Visual preview of transform output
- Lock/unlock individual parameters
- Delete transforms

## Full Dashboard Example

Combine components into a complete image management dashboard:

```razor
@page "/media"

<div style="display: flex; gap: 1.5rem;">
    <div style="flex: 1;">
        <PixaultGallery Project="myapp" OnImageSelected="OnSelect" />
    </div>

    @if (_selected is not null)
    {
        <div style="width: 380px;">
            <PixaultImageDetail Project="myapp"
                                Image="_selected"
                                OnDeleted="OnDelete"
                                OnUpdated="OnUpdate" />
        </div>
    }
</div>

@code {
    private ImageMetadataDto? _selected;

    private void OnSelect(ImageMetadataDto image) => _selected = image;
    private void OnDelete() => _selected = null;
    private void OnUpdate(ImageMetadataDto updated) => _selected = updated;
}
```

## Responsive Image Tag Helper

Build responsive `<img>` tags with srcset:

```csharp
@inject PixaultImageService ImageService

@{
    var srcset = string.Join(", ",
        new[] { 400, 800, 1200 }.Select(w =>
            $"{ImageService.Url(imageId).Width(w).Format(OutputFormat.WebP).Build()} {w}w"));
}

<img src="@ImageService.Url(imageId).Width(800).Format(OutputFormat.WebP).Build()"
     srcset="@srcset"
     sizes="(max-width: 600px) 400px, (max-width: 1024px) 800px, 1200px"
     alt="@image.Alt"
     loading="lazy" />
```

## Oqtane Module

Since Oqtane is built on Blazor, Pixault components integrate natively as an Oqtane module. See the <a href="integration-wordpress.md">CMS Integration Guide</a> for module configuration details.
