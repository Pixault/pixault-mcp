# Vue Integration

Use the Pixault JavaScript SDK with Vue 3 Composition API for image management, responsive rendering, and uploads.

## Setup

```bash
npm install @pixault/sdk
```

```javascript
// composables/usePixault.js
import { PixaultUrl } from '@pixault/sdk';

const pxUrl = new PixaultUrl({
  baseUrl: 'https://img.pixault.io',
  project: 'myapp',
});

export function usePixault() {
  function buildUrl(imageId, { width, height, fit, quality, format } = {}) {
    let builder = pxUrl.image(imageId);
    if (width) builder = builder.width(width);
    if (height) builder = builder.height(height);
    if (fit) builder = builder.fit(fit);
    if (quality) builder = builder.quality(quality);
    return builder.format(format || 'webp').build();
  }

  function buildSrcSet(imageId, widths = [400, 800, 1200]) {
    return widths
      .map(w => `${buildUrl(imageId, { width: w })} ${w}w`)
      .join(', ');
  }

  function buildPlaceholder(imageId) {
    return buildUrl(imageId, { width: 40, quality: 20 });
  }

  return { buildUrl, buildSrcSet, buildPlaceholder };
}
```

## Responsive Image Component

```vue
<!-- components/PixaultImage.vue -->
<script setup>
import { computed } from 'vue';
import { usePixault } from '@/composables/usePixault';

const props = defineProps({
  imageId: { type: String, required: true },
  alt: { type: String, default: '' },
  widths: { type: Array, default: () => [400, 800, 1200] },
  sizes: { type: String, default: '(max-width: 600px) 400px, (max-width: 1024px) 800px, 1200px' },
});

const { buildUrl, buildSrcSet, buildPlaceholder } = usePixault();

const src = computed(() => buildUrl(props.imageId, { width: 800 }));
const srcset = computed(() => buildSrcSet(props.imageId, props.widths));
const placeholder = computed(() => buildPlaceholder(props.imageId));
</script>

<template>
  <img
    :src="src"
    :srcset="srcset"
    :sizes="sizes"
    :alt="alt"
    loading="lazy"
    :style="{ backgroundImage: `url(${placeholder})`, backgroundSize: 'cover' }"
  />
</template>
```

### Usage

```vue
<PixaultImage image-id="img_01JKXYZ" alt="Product photo" />
```

## Image Gallery

```vue
<!-- components/PixaultGallery.vue -->
<script setup>
import { ref, onMounted } from 'vue';
import PixaultImage from './PixaultImage.vue';

const props = defineProps({
  project: { type: String, required: true },
});

const emit = defineEmits(['select']);

const images = ref([]);
const loading = ref(true);

onMounted(async () => {
  const res = await fetch(`/api/images?project=${props.project}`);
  const data = await res.json();
  images.value = data.images;
  loading.value = false;
});
</script>

<template>
  <div v-if="loading" class="gallery-loading">Loading...</div>
  <div v-else class="gallery-grid">
    <div
      v-for="img in images"
      :key="img.id"
      class="gallery-item"
      @click="emit('select', img)"
    >
      <PixaultImage :image-id="img.id" :alt="img.alt" :widths="[200, 400]" sizes="200px" />
      <p class="gallery-caption">{{ img.alt }}</p>
    </div>
  </div>
</template>

<style scoped>
.gallery-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
}
.gallery-item { cursor: pointer; }
.gallery-caption { font-size: 0.875rem; margin-top: 0.5rem; color: #64748b; }
</style>
```

## Upload Component

```vue
<!-- components/PixaultUploader.vue -->
<script setup>
import { ref } from 'vue';

const emit = defineEmits(['uploaded']);

const uploading = ref(false);
const dragOver = ref(false);

async function handleFiles(files) {
  if (!files.length) return;
  uploading.value = true;

  for (const file of files) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('alt', file.name.replace(/\.[^.]+$/, ''));

    const res = await fetch('/api/upload', { method: 'POST', body: formData });
    const image = await res.json();
    emit('uploaded', image);
  }

  uploading.value = false;
}

function onDrop(e) {
  dragOver.value = false;
  handleFiles(e.dataTransfer.files);
}

function onFileSelect(e) {
  handleFiles(e.target.files);
}
</script>

<template>
  <div
    class="upload-zone"
    :class="{ 'drag-over': dragOver }"
    @dragover.prevent="dragOver = true"
    @dragleave="dragOver = false"
    @drop.prevent="onDrop"
  >
    <p v-if="uploading">Uploading...</p>
    <template v-else>
      <p>Drag images here or</p>
      <label class="upload-btn">
        Browse files
        <input type="file" accept="image/*" multiple hidden @change="onFileSelect" />
      </label>
    </template>
  </div>
</template>

<style scoped>
.upload-zone {
  border: 2px dashed #e2e8f0;
  border-radius: 12px;
  padding: 2rem;
  text-align: center;
  transition: all 0.2s;
}
.upload-zone.drag-over { border-color: #6366f1; background: #eef2ff; }
.upload-btn {
  display: inline-block;
  background: #6366f1;
  color: white;
  padding: 0.5rem 1rem;
  border-radius: 8px;
  cursor: pointer;
  margin-top: 0.5rem;
}
</style>
```

## Nuxt.js Server Route

```javascript
// server/api/upload.post.js
import { Pixault } from '@pixault/sdk';

const px = new Pixault({
  clientId: process.env.PIXAULT_CLIENT_ID,
  clientSecret: process.env.PIXAULT_CLIENT_SECRET,
  project: 'myapp',
});

export default defineEventHandler(async (event) => {
  const formData = await readMultipartFormData(event);
  const file = formData.find(f => f.name === 'file');

  const image = await px.upload(file.data, {
    filename: file.filename,
    alt: formData.find(f => f.name === 'alt')?.data?.toString() || '',
  });

  return image;
});
```
