# React Integration

Use the Pixault JavaScript SDK to build image galleries, uploaders, and responsive image components in React.

## Setup

```bash
npm install @pixault/sdk
```

```javascript
// lib/pixault.js
import { Pixault, PixaultUrl } from '@pixault/sdk';

// Server-side only (API calls)
export const px = new Pixault({
  clientId: process.env.PIXAULT_CLIENT_ID,
  clientSecret: process.env.PIXAULT_CLIENT_SECRET,
  project: 'myapp',
});

// Client-safe (URL building only — no secrets)
export const pxUrl = new PixaultUrl({
  baseUrl: 'https://img.pixault.io',
  project: 'myapp',
});
```

## Responsive Image Component

```jsx
function PixaultImage({ imageId, alt, sizes, widths = [400, 800, 1200], className }) {
  const srcSet = widths
    .map(w => `${pxUrl.image(imageId).width(w).format('webp').build()} ${w}w`)
    .join(', ');

  const src = pxUrl.image(imageId).width(800).format('webp').build();

  const placeholder = pxUrl.image(imageId)
    .width(40).quality(20).blur(10).format('webp').build();

  return (
    <img
      src={src}
      srcSet={srcSet}
      sizes={sizes || '(max-width: 600px) 400px, (max-width: 1024px) 800px, 1200px'}
      alt={alt}
      loading="lazy"
      className={className}
      style={{ backgroundImage: `url(${placeholder})`, backgroundSize: 'cover' }}
    />
  );
}
```

### Usage

```jsx
<PixaultImage
  imageId="img_01JKXYZ"
  alt="Team photo"
  sizes="(max-width: 768px) 100vw, 50vw"
/>
```

## Image Gallery

```jsx
import { useState, useEffect } from 'react';

function ImageGallery({ project }) {
  const [images, setImages] = useState([]);
  const [selected, setSelected] = useState(null);

  useEffect(() => {
    fetch(`/api/images?project=${project}`)
      .then(res => res.json())
      .then(data => setImages(data.images));
  }, [project]);

  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: '1rem' }}>
      {images.map(img => (
        <div key={img.id} onClick={() => setSelected(img)} style={{ cursor: 'pointer' }}>
          <PixaultImage
            imageId={img.id}
            alt={img.alt}
            widths={[200, 400]}
            sizes="200px"
          />
          <p style={{ fontSize: '0.875rem', marginTop: '0.5rem' }}>{img.alt}</p>
        </div>
      ))}
    </div>
  );
}
```

## Upload Component

```jsx
import { useState, useCallback } from 'react';

function ImageUploader({ onUpload }) {
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);

  const handleUpload = useCallback(async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    setUploading(true);
    const formData = new FormData();
    formData.append('file', file);
    formData.append('alt', file.name.replace(/\.[^.]+$/, ''));

    try {
      const res = await fetch('/api/upload', {
        method: 'POST',
        body: formData,
      });
      const image = await res.json();
      onUpload?.(image);
    } finally {
      setUploading(false);
      setProgress(0);
    }
  }, [onUpload]);

  return (
    <div>
      <input type="file" accept="image/*" onChange={handleUpload} disabled={uploading} />
      {uploading && <progress value={progress} max={100} />}
    </div>
  );
}
```

## Next.js Image Optimization

Use Pixault as a custom loader for `next/image`:

```jsx
// next.config.js
module.exports = {
  images: {
    loader: 'custom',
    loaderFile: './lib/pixault-loader.js',
  },
};

// lib/pixault-loader.js
export default function pixaultLoader({ src, width, quality }) {
  const params = [`w_${width}`];
  if (quality) params.push(`q_${quality}`);
  return `https://img.pixault.io/myapp/${src}/${params.join(',')}.webp`;
}
```

```jsx
import Image from 'next/image';

<Image
  src="img_01JKXYZ"
  alt="Optimized image"
  width={800}
  height={600}
  quality={85}
/>
```

## Server-Side API Route (Next.js)

```javascript
// app/api/upload/route.js
import { px } from '@/lib/pixault';

export async function POST(request) {
  const formData = await request.formData();
  const file = formData.get('file');

  const image = await px.upload(file, {
    alt: formData.get('alt') || '',
    tags: formData.get('tags')?.split(',') || [],
  });

  return Response.json(image, { status: 201 });
}
```
