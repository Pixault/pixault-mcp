# Architecture Overview

This page describes how Pixault processes and delivers images at scale. Understanding the architecture helps you optimize your integration for performance and cost.

## Request Flow

```
Client Request
    │
    ▼
┌─────────────────────────────┐
│  CDN (Edge)                 │
│  • 30-day immutable cache   │
│  • Auto WebP/AVIF           │
│  • DDoS protection          │
│                             │
│  Cache HIT → Serve (0ms)    │
│  Cache MISS ──┐             │
└───────────────┼─────────────┘
                │
                ▼
┌─────────────────────────────┐
│  Pixault API (.NET 10)      │
│                             │
│  1. Rate limiting           │
│  2. URL parsing             │
│  3. Signature validation*   │
│  4. Cache check             │
│  5. Transform / rasterize   │
│  6. Store variant            │
│  7. Respond + cache headers │
└─────────────────────────────┘
                │
                ▼
┌─────────────────────────────┐
│  Object Storage             │
│  • Originals                │
│  • Cached variants          │
│  • Metadata                 │
│  • Watermarks               │
│  • Transform definitions    │
└─────────────────────────────┘
```

\* Signature validation applies only to signed `original.{format}` downloads (see below); it is not run on every request.

## CDN Layer

Every image URL maps to a unique CDN cache key. The first request triggers a transformation; subsequent requests for the same URL are served directly from the CDN edge with zero origin traffic.

**Cache headers:**

```
Cache-Control: public, max-age=2592000, immutable
```

This means:
- Browsers and CDN cache for 30 days
- `immutable` tells browsers not to revalidate (the content at this URL never changes)
- Different transform parameters = different URL = different cache entry

## Image Processing

Pixault performs all image transformations on-demand:

| Operation | Implementation |
|-----------|---------------|
| Resize | `bitmap.Resize()` with configurable sampling |
| Format conversion | JPEG, PNG, WebP, AVIF encoding |
| Quality | Configurable per-request (1–100) |
| Blur | Gaussian blur via `SKImageFilter` |
| Watermark | Composite overlay with position and opacity |
| SVG | Sanitization + optional rasterization |
| EPS / PostScript | Vector assets are rasterized on delivery; multi-design EPS can be split and embedded SVG extracted |

Processing happens on-demand and results are cached. The cache key is a SHA256 hash of the transformation parameters, ensuring deterministic variant identification.

### Delivery Paths

Beyond on-demand transformed images, Pixault serves two other first-class asset types:

- **EPS / PostScript** — Vector uploads are rasterized for delivery. A multi-design EPS can be split into its constituent designs, and embedded SVG can be extracted as a derived asset.
- **Video** — Video files are streamed over HTTP range requests from `/{project}/{videoId}/video.{ext}` (e.g. `mp4`, `webm`, `mov`), enabling seeking and partial-content playback.

## Multi-Project Isolation

Each project gets isolated:

- **Storage** — Separate object prefixes per project
- **Metadata** — Independent image metadata per project
- **Named Transforms** — Project-specific presets
- **Usage Tracking** — Per-project bandwidth and storage metering

Projects share the same API infrastructure but have no visibility into each other's data.

## Storage Architecture

Pixault keeps each asset class in object storage, namespaced per project:

| Class | Purpose | Retention |
|-------|---------|-----------|
| Originals | Uploaded files | Until deleted |
| Cache | Transformed variants | Auto-expires, regenerated on demand |
| Metadata | Image metadata | Mirrors originals lifecycle |
| Watermarks | Watermark overlay images | Until deleted |
| Transforms | Named transform definitions | Until deleted |

Within each class, objects are keyed by project and image ID (and, for cached variants, by a hash of the transform parameters).

## Billing Engine

The billing system tracks usage per subscription:

- **Bandwidth** — Bytes served on each image response
- **Storage** — Total bytes stored (originals)
- **Projects** — Count of active project identifiers

Usage snapshots are recorded per project for historical analytics and invoice generation. Overages are calculated at the end of each billing period.

## API Authentication

Pixault uses two distinct auth paths: browser sessions for the dashboard, and a single API key for all programmatic access.

### Dashboard (Browser)

- Microsoft Entra External ID (OpenID Connect)
- Cookie-based sessions
- Automatic account provisioning on first login

### API (Machine-to-Machine)

- A single `X-Api-Key` header authenticates every API request — there is no client-id/client-secret pair
- Hashed key storage (secrets never stored in plaintext)
- Per-key and per-project rate limiting

For signed `original.{format}` downloads, requests additionally carry `sig` and `exp` query parameters when an HMAC secret is configured; this signature check is the only request-time validation beyond the API key.

## Observability

| Signal | Technology | Details |
|--------|-----------|---------|
| Tracing | OpenTelemetry | Distributed traces across HTTP, processing, and storage |
| Metrics | OpenTelemetry | Images served, bytes, transform latency, cache rates |
| Logging | Serilog | Structured logs enriched with trace/span IDs |
| Export | OTLP | Compatible with Jaeger, Grafana, Datadog, etc. |

## Custom Domains

Paid plans support custom domains:

```
images.yourdomain.com → CNAME → img.pixault.io
```

SSL is handled automatically. Images are served from your own domain with full CDN caching.
