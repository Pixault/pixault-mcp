# Plugin Marketplace

Plugins extend Pixault's image processing with capabilities beyond the built-in transforms — background removal, smart cropping, image filters, watermark templates, and more. You browse the marketplace, activate the plugins you want for a project, then invoke them directly in delivery URLs.

## Authentication

The plugin management endpoints require a single header:

```
X-Api-Key: <your-api-key>
```

(Delivery URLs that invoke an activated plugin are public, like any other delivery URL.)

## Marketplace

```
GET /api/plugins
GET /api/plugins/{name}
```

`GET /api/plugins` lists every available marketplace plugin. Each entry includes:

| Field | Description |
|-------|-------------|
| `name` | Plugin identifier |
| `displayName` | Human-readable name |
| `description` | What the plugin does |
| `vendor` | Plugin author |
| `category` | Grouping (e.g. `enhancement`, `cropping`) |
| `stage` | Release stage (e.g. `stable`, `beta`) |
| `priceCentsPerInvocation` | Per-invocation price in cents (`0` = free) |
| `urlPrefix` | The token used to invoke the plugin in a delivery URL |

## Per-project activation

```
GET   /api/{project}/plugins                       # list with activation status
POST  /api/{project}/plugins/{name}/activate
POST  /api/{project}/plugins/{name}/deactivate
```

`GET /api/{project}/plugins` returns the marketplace list annotated with an `isActivated` flag for the project. A plugin must be activated before it can be invoked in that project's URLs.

```bash
# Activate background removal for "myapp"
curl -X POST https://img.pixault.io/api/myapp/plugins/background-removal/activate \
  -H "X-Api-Key: <your-api-key>"
```

## Invoking a plugin in a delivery URL

Once activated, invoke a plugin by adding its `urlPrefix` as a token in the comma-separated transform segment, in the form `{prefix}_{value}`. Plugin tokens combine freely with the built-in transform parameters.

```
# Background removal, then resize
https://img.pixault.io/myapp/img_01JK/bg_remove,w_800.webp

# Smart crop to a square
https://img.pixault.io/myapp/img_01JK/smart_crop,w_600,h_600.webp

# Sepia filter
https://img.pixault.io/myapp/img_01JK/filter_sepia,w_800.webp
```

Built-in marketplace plugins and their URL prefixes include:

| Plugin | Prefix | Example |
|--------|--------|---------|
| Background removal | `bg` | `bg_remove` |
| Smart crop | `smart_crop` | `smart_crop` |
| Image filter | `filter` | `filter_sepia` |
| Watermark template | `wt` | `wt_{template}` |
| Draw | `draw` | `draw_{spec}` |

Anything in the transform segment that is not a known built-in parameter (`w_`, `h_`, `fit_`, `q_`, `blur_`, `wm_`, `t_`, …) is routed to the plugin system by its prefix.

## Enforcing plugins with named transforms

Named transforms can carry default plugin invocations and lock them so client URLs can't disable them. Set `plugins` (a map of plugin name → parameters) and `lockedPlugins` on a transform — see [Named Transforms](api-transforms.md).

## Billing

Plugins with a non-zero `priceCentsPerInvocation` are billed per invocation (each transformed render that runs the plugin). Free plugins (`priceCentsPerInvocation: 0`) carry no per-use charge. See [Billing & Plans](billing-and-plans.md).
