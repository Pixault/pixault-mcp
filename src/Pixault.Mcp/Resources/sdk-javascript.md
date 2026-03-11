# JavaScript SDK

The Pixault JavaScript SDK provides image URL construction, uploads, and management for Node.js and browser environments. Available as `@pixault/sdk` on npm.

## Installation

```bash
npm install @pixault/sdk
# or
yarn add @pixault/sdk
```

## Configuration

```javascript
import { Pixault } from '@pixault/sdk';

const px = new Pixault({
  clientId: 'px_cl_your_client_id',
  clientSecret: 'pk_your_secret_key',
  baseUrl: 'https://img.pixault.io',  // optional, this is the default
  project: 'myapp',
});
```

> **Browser usage:** Never expose your client secret in client-side code. Use the URL builder (which doesn't require secrets) in the browser, and make API calls from your server.

## URL Builder

Generate transform URLs without making network requests:

```javascript
const url = px.url('img_01JKXYZ')
  .width(800)
  .height(600)
  .fit('cover')
  .quality(85)
  .format('webp')
  .build();
// → "https://img.pixault.io/myapp/img_01JKXYZ/w_800,h_600,fit_cover,q_85.webp"
```

### Methods

| Method | Description |
|--------|-------------|
| `.width(n)` | Width in pixels (1–4096) |
| `.height(n)` | Height in pixels (1–4096) |
| `.fit(mode)` | `'cover'`, `'contain'`, `'fill'`, `'pad'` |
| `.quality(n)` | Output quality (1–100) |
| `.blur(n)` | Gaussian blur radius (1–100) |
| `.watermark(id)` | Watermark overlay ID |
| `.watermarkPosition(pos)` | `'tl'`, `'tr'`, `'bl'`, `'br'`, `'c'`, `'tile'` |
| `.watermarkOpacity(n)` | Opacity (1–100) |
| `.transform(name)` | Named transform preset |
| `.format(fmt)` | `'jpg'`, `'png'`, `'webp'`, `'avif'` |
| `.build()` | Returns the URL string |

### Browser-Only URL Builder

For client-side usage without credentials:

```javascript
import { PixaultUrl } from '@pixault/sdk';

const builder = new PixaultUrl({
  baseUrl: 'https://img.pixault.io',
  project: 'myapp',
});

const url = builder.image('img_01JKXYZ')
  .width(400).format('webp').build();
```

## Upload

```javascript
// Node.js (with fs)
import fs from 'fs';

const image = await px.upload(fs.createReadStream('photo.jpg'), {
  filename: 'photo.jpg',
  alt: 'Team photo from retreat',
  tags: ['team', 'retreat'],
});

console.log(image.id);  // "img_01JKXYZ123"
console.log(image.url);  // "https://img.pixault.io/myapp/img_01JKXYZ123/original.jpg"

// Browser (with File object)
const fileInput = document.querySelector('input[type="file"]');
const file = fileInput.files[0];

const image = await px.upload(file, {
  alt: 'User avatar',
  tags: ['avatar'],
});
```

## Image Management

```javascript
// List images
const { images, cursor, hasMore } = await px.listImages({
  limit: 20,
  tag: 'nature',
});

// Get metadata
const metadata = await px.getImage('img_01JKXYZ');

// Update metadata
await px.updateImage('img_01JKXYZ', {
  alt: 'Updated description',
  tags: ['updated', 'tags'],
});

// Delete
await px.deleteImage('img_01JKXYZ');
```

## List & Search Images

```javascript
// List all images
const result = await pixault.listImages('my-project');

// Search by text
const matches = await pixault.listImages('my-project', { search: 'hero' });

// Filter by category
const tattoos = await pixault.listImages('my-project', { category: 'tattoo-flash' });

// Filter by keyword
const portraits = await pixault.listImages('my-project', { keyword: 'portrait' });

// Paginate
const page2 = await pixault.listImages('my-project', {}, 50, result.nextCursor);
```

## Named Transforms

```javascript
// Create
await px.createTransform('thumbnail', {
  parameters: { w: 200, h: 200, fit: 'cover', q: 80 },
  locked: ['w', 'h', 'fit'],
});

// List
const transforms = await px.listTransforms();

// Use in URL builder
const url = px.url('img_01JKXYZ').transform('thumbnail').format('webp').build();
```

## TypeScript Support

The SDK is written in TypeScript and exports full type definitions:

```typescript
import type { PixaultConfig, ImageMetadata, UploadOptions } from '@pixault/sdk';

const config: PixaultConfig = {
  clientId: 'px_cl_abc123',
  clientSecret: 'pk_secret456',
  project: 'myapp',
};
```

## Error Handling

```javascript
try {
  await px.upload(file, { alt: 'Photo' });
} catch (err) {
  if (err.status === 413) {
    console.error('Storage quota exceeded');
  } else if (err.status === 429) {
    console.error(`Rate limited. Retry after ${err.retryAfter}s`);
  }
}
```

## React Integration

See the <a href="integration-react.md">React Integration Guide</a> for component examples and hooks.
