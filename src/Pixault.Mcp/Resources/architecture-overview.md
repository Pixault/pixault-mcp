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
│  3. Signature validation    │
│  4. Cache check             │
│  5. Transform               │
│  6. Store variant            │
│  7. Respond + cache headers │
└─────────────────────────────┘
                │
                ▼
┌─────────────────────────────┐
│  Cloud Storage              │
│  • Originals bucket         │
│  • Cache bucket (variants)  │
│  • Metadata bucket          │
│  • Watermarks bucket        │
│  • Transforms bucket        │
└─────────────────────────────┘
```

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

Processing happens on-demand and results are cached. The cache key is a SHA256 hash of the transformation parameters, ensuring deterministic variant identification.

## Multi-Project Isolation

Each project gets isolated:

- **Storage** — Separate object prefixes per project
- **Metadata** — Independent image metadata per project
- **Named Transforms** — Project-specific presets
- **Usage Tracking** — Per-project bandwidth and storage metering

Projects share the same API infrastructure but have no visibility into each other's data.

## Storage Architecture

```
pixault-originals/{project}/{imageId}
pixault-cache/{project}/{imageId}/{variant-hash}
pixault-metadata/{project}/{imageId}.json
pixault-watermarks/{project}/{watermarkId}
pixault-transforms/{project}/{transformName}.json
```

| Bucket | Purpose | Retention |
|--------|---------|-----------|
| Originals | Uploaded files | Until deleted |
| Cache | Transformed variants | Auto-expires, regenerated on demand |
| Metadata | Image metadata JSON | Mirrors originals lifecycle |
| Watermarks | Watermark overlay images | Until deleted |
| Transforms | Named transform definitions | Until deleted |

## Billing Engine

The billing system tracks usage per subscription:

- **Bandwidth** — Bytes served on each image response
- **Storage** — Total bytes stored (originals only)
- **Projects** — Count of active project identifiers

Usage snapshots are recorded daily per project for historical analytics and invoice generation. Overages are calculated at the end of each billing period.

## API Authentication

Two authentication models:

### Dashboard (Browser)

- Microsoft Entra External ID (OpenID Connect)
- Cookie-based sessions
- Automatic account provisioning on first login

### API (Machine-to-Machine)

- Client ID + Client Secret headers
- SHA256-hashed key storage (secrets never stored in plaintext)
- Per-key rate limiting (100 req/min)
- Optional project scoping per key

## Observability

| Signal | Technology | Details |
|--------|-----------|---------|
| Tracing | OpenTelemetry | Distributed traces across HTTP, processing, and storage |
| Metrics | OpenTelemetry | Images served, bytes, transform latency, cache rates |
| Logging | Serilog | Structured logs enriched with trace/span IDs |
| Export | OTLP | Compatible with Jaeger, Grafana, Datadog, etc. |

## Custom Domains

Growth, Pro, and Business plans support custom domains:

```
images.yourdomain.com → CNAME → img.pixault.io
```

SSL is handled automatically. Images are served from your own domain with full CDN caching.

## Performance Characteristics

| Metric | Typical Value |
|--------|--------------|
| CDN cache hit latency | < 50ms (edge) |
| Transform (resize + encode) | 50–200ms |
| Original fetch | 10–30ms |
| Cached variant fetch | 10–30ms |
| Cold start | < 2s |
