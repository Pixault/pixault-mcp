# WordPress Integration

Pixault integrates with WordPress through a plugin that can operate in two modes: CDN mode (non-destructive URL rewriting) or Full mode (Pixault as primary storage).

## CDN Mode

CDN mode rewrites WordPress media URLs to serve images through Pixault transformations. Existing media stays in WordPress — Pixault acts as a processing and delivery layer.

### How It Works

```
Original: https://yoursite.com/wp-content/uploads/2025/photo.jpg
Rewritten: https://img.pixault.io/yoursite/img_abc/w_800,q_85.webp
```

WordPress's `wp_get_attachment_url` filter rewrites URLs at render time. Originals remain in the WordPress media library.

### Setup

1. Install the Pixault plugin from the WordPress plugin directory
2. Go to **Settings** → **Pixault**
3. Enter your **Client ID** and **Client Secret**
4. Set your **Project** name
5. Select **CDN Mode**
6. Save

### Configuration

```
Pixault Client ID: px_cl_your_client_id
Pixault Client Secret: pk_your_secret_key
Project: yoursite
Default Quality: 85
Default Format: webp
Enable Lazy Loading: Yes
Enable LQIP Placeholders: Yes
```

### Responsive Images

The plugin automatically generates `srcset` attributes using Pixault transforms:

```html
<!-- Output -->
<img src="https://img.pixault.io/yoursite/img_abc/w_800,q_85.webp"
     srcset="https://img.pixault.io/yoursite/img_abc/w_400,q_85.webp 400w,
             https://img.pixault.io/yoursite/img_abc/w_800,q_85.webp 800w,
             https://img.pixault.io/yoursite/img_abc/w_1200,q_85.webp 1200w"
     sizes="(max-width: 600px) 100vw, (max-width: 1024px) 50vw, 800px"
     loading="lazy"
     alt="Photo description" />
```

### LQIP Blur-Up

When enabled, the plugin embeds a tiny blurred placeholder inline:

```html
<img src="https://img.pixault.io/yoursite/img_abc/w_800,q_85.webp"
     style="background-image: url(data:image/webp;base64,...); background-size: cover;"
     loading="lazy" />
```

## Full Mode

Full mode replaces the WordPress media upload flow. Images are uploaded directly to Pixault and served from the CDN. WordPress stores only a reference.

### Advantages Over CDN Mode

- **Reduced server storage** — Originals stored in Pixault (GCS), not WordPress
- **Consistent metadata** — Alt text, tags, and titles synced with Pixault
- **Advanced features** — Watermarking, named transforms, signed URLs

### Setup

1. Switch to **Full Mode** in Settings → Pixault
2. New uploads go to Pixault automatically
3. Existing media can be migrated with the bulk migration tool

### Bulk Migration

**Settings** → **Pixault** → **Migrate Existing Media**

The migration tool:
1. Scans your WordPress media library
2. Uploads each file to Pixault
3. Updates database references to point to Pixault URLs
4. Optionally removes local copies after verification

## Gutenberg Block

The plugin adds a **Pixault Image** Gutenberg block:

- Image picker with Pixault gallery browser
- Transform selector (choose from named transforms)
- Alt text, caption, and link fields
- Responsive breakpoint configuration
- Live preview with selected transforms

## Named Transform Support

Map WordPress image sizes to Pixault named transforms:

```
thumbnail → t_thumb (200x200, cover crop)
medium    → t_medium (600px wide)
large     → t_large (1024px wide)
full      → original (no transformation)
```

Configure in **Settings** → **Pixault** → **Image Size Mapping**.

## WooCommerce Compatibility

The plugin integrates with WooCommerce product images:

- Product gallery images served through Pixault
- Zoom and lightbox support maintained
- Variation images with automatic format optimization

## Requirements

- WordPress 6.0+
- PHP 8.1+
- A Pixault account with an active subscription
- The Pixault PHP SDK (bundled with the plugin)

## Oqtane Module

For .NET CMS integration, Pixault provides a native **Oqtane module** that reuses the `Pixault.Blazor` component library directly:

- File manager integration for uploads
- Theme-level image optimization
- Module settings: API key, project, default transforms
- Native Blazor components (no JavaScript bridge needed)

Since Oqtane is built on Blazor, see the <a href="integration-blazor.md">Blazor Integration Guide</a> for component details.
