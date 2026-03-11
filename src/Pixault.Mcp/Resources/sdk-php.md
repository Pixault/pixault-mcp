# PHP SDK

The Pixault PHP SDK provides image URL construction, uploads, and management. Available as `pixault/pixault-php` on Packagist.

## Installation

```bash
composer require pixault/pixault-php
```

Requires PHP 8.1+ and the `curl` extension.

## Configuration

```php
use Pixault\Pixault;

$pixault = new Pixault([
    'client_id'     => 'px_cl_your_client_id',
    'client_secret' => 'pk_your_secret_key',
    'base_url'      => 'https://img.pixault.io',  // optional default
    'project'       => 'myapp',
]);
```

## URL Builder

Generate transform URLs without making HTTP requests:

```php
$url = $pixault->url('img_01JKXYZ')
    ->width(800)
    ->height(600)
    ->fit('cover')
    ->quality(85)
    ->format('webp')
    ->build();
// → "https://img.pixault.io/myapp/img_01JKXYZ/w_800,h_600,fit_cover,q_85.webp"
```

### Methods

| Method | Description |
|--------|-------------|
| `->width(int)` | Width (1–4096) |
| `->height(int)` | Height (1–4096) |
| `->fit(string)` | `'cover'`, `'contain'`, `'fill'`, `'pad'` |
| `->quality(int)` | Output quality (1–100) |
| `->blur(int)` | Gaussian blur (1–100) |
| `->watermark(string)` | Watermark overlay ID |
| `->watermarkPosition(string)` | `'tl'`, `'tr'`, `'bl'`, `'br'`, `'c'`, `'tile'` |
| `->watermarkOpacity(int)` | Opacity (1–100) |
| `->transform(string)` | Named transform preset |
| `->format(string)` | `'jpg'`, `'png'`, `'webp'`, `'avif'` |
| `->build()` | Returns URL string |

### Named Transforms

```php
$url = $pixault->url('img_01JKXYZ')
    ->transform('thumbnail')
    ->format('webp')
    ->build();
```

## Upload

```php
// Upload from file path
$image = $pixault->upload('/path/to/photo.jpg', [
    'alt'  => 'Team photo',
    'tags' => ['team', 'retreat'],
]);

echo $image['id'];   // "img_01JKXYZ123"
echo $image['url'];  // "https://img.pixault.io/myapp/img_01JKXYZ123/original.jpg"

// Upload from stream
$stream = fopen('/path/to/photo.jpg', 'r');
$image = $pixault->uploadStream($stream, 'photo.jpg', [
    'alt' => 'Uploaded via stream',
]);
```

## Image Management

```php
// List images
$result = $pixault->listImages(['limit' => 20, 'tag' => 'nature']);
foreach ($result['images'] as $image) {
    echo $image['id'] . ': ' . $image['alt'] . "\n";
}

// Get metadata
$metadata = $pixault->getImage('img_01JKXYZ');

// Update metadata
$pixault->updateImage('img_01JKXYZ', [
    'alt'  => 'Updated description',
    'tags' => ['updated', 'tags'],
]);

// Delete
$pixault->deleteImage('img_01JKXYZ');
```

## List & Search Images

```php
// List all images
$result = $pixault->listImages('my-project');

// Search by text
$matches = $pixault->listImages('my-project', ['search' => 'hero']);

// Filter by category
$tattoos = $pixault->listImages('my-project', ['category' => 'tattoo-flash']);

// Paginate
$page2 = $pixault->listImages('my-project', [], 50, $result['nextCursor']);
```

## Named Transforms

```php
// Create
$pixault->createTransform('thumbnail', [
    'parameters' => ['w' => 200, 'h' => 200, 'fit' => 'cover'],
    'locked'     => ['w', 'h', 'fit'],
]);

// List
$transforms = $pixault->listTransforms();
```

## Laravel Integration

```php
// config/services.php
'pixault' => [
    'client_id'     => env('PIXAULT_CLIENT_ID'),
    'client_secret' => env('PIXAULT_CLIENT_SECRET'),
    'project'       => env('PIXAULT_PROJECT', 'default'),
],

// AppServiceProvider
$this->app->singleton(Pixault::class, function () {
    return new Pixault(config('services.pixault'));
});

// In a controller
public function upload(Request $request, Pixault $pixault)
{
    $file = $request->file('image');
    $image = $pixault->upload($file->getRealPath(), [
        'alt' => $request->input('alt'),
    ]);

    return response()->json($image, 201);
}
```

## Error Handling

```php
use Pixault\Exceptions\PixaultApiException;
use Pixault\Exceptions\QuotaExceededException;
use Pixault\Exceptions\RateLimitException;

try {
    $pixault->upload('/path/to/photo.jpg');
} catch (QuotaExceededException $e) {
    echo "Storage quota exceeded: " . $e->getMessage();
} catch (RateLimitException $e) {
    echo "Rate limited. Retry after {$e->retryAfter} seconds";
} catch (PixaultApiException $e) {
    echo "API error ({$e->getCode()}): {$e->getMessage()}";
}
```

## WordPress Integration

See the <a href="integration-wordpress.md">WordPress Integration Guide</a> for using Pixault as a media library replacement.
