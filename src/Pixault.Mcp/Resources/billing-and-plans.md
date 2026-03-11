# Billing & Plans

Pixault offers four subscription tiers designed for teams of every size. All plans include a 14-day free trial.

## Plans

| | **Starter** | **Growth** | **Pro** | **Business** |
|---|---|---|---|---|
| **Monthly** | $9/mo | $29/mo | $69/mo | $99/mo |
| **Annual** | $90/yr (save 17%) | $290/yr (save 17%) | $690/yr (save 17%) | $990/yr (save 17%) |
| **Bandwidth** | 15 GB/mo | 50 GB/mo | 200 GB/mo | 1 TB/mo |
| **Storage** | 5 GB | 25 GB | 100 GB | 500 GB |
| **Projects** | 1 | 3 | 15 | Unlimited |
| **API Keys** | 1 | 3 | 10 | Unlimited |
| **Custom Domain** | -- | Yes | Yes | Yes |
| **Bring Your Own Storage** | -- | -- | Yes | Yes |
| **Bandwidth Overage** | $0.12/GB | $0.10/GB | $0.08/GB | $0.06/GB |
| **Storage Overage** | $0.06/GB | $0.05/GB | $0.04/GB | $0.03/GB |

## Choosing a Plan

- **Starter** — For personal projects and small sites with a single project
- **Growth** — For agencies or apps with multiple brands/projects
- **Pro** — For production SaaS with heavy image traffic
- **Business** — For enterprise with unlimited projects, keys, and BYOS

## Free Trial

All plans come with a 14-day free trial. During the trial:

- Full access to all plan features
- No charges until the trial ends
- Credit card required at signup
- Cancel anytime before trial ends to avoid charges

## Usage Metering

Pixault tracks three usage dimensions:

### Bandwidth

Measured as bytes transferred when serving images. Includes:
- Transformed images served to end users
- Original file downloads
- API responses

Does **not** include:
- CDN cache hits (these don't reach the origin)
- Upload traffic

### Storage

Measured as total bytes stored (originals only). Cached variants don't count against storage limits.

### Projects

The number of unique project identifiers you use. Each project has isolated storage and can have independent named transforms.

## Overages

Paid plans allow overages — you won't be cut off when you exceed included limits:

- Bandwidth overages are billed per GB at the plan's overage rate
- Storage overages are billed per GB at the plan's overage rate
- Overages appear on the next invoice as line items

## Managing Your Subscription

### Dashboard

Visit [pixault.io](https://pixault.io) → **Billing** to:

- View your current plan and usage
- See bandwidth and storage meters
- Manage API keys
- View invoice history
- Change or cancel your plan

### API Keys

Each API key consists of:

- **Client ID** (`px_cl_...`) — Public identifier, safe to store in configs
- **Client Secret** (`pk_...`) — Secret key, shown only once at creation time

Use both in API requests:

```
X-Client-Id: px_cl_your_client_id
X-Client-Secret: pk_your_secret_key
```

Keys can be scoped to specific projects for security.

### Cancellation

Cancel anytime from the billing dashboard. When you cancel:

- Access continues until the end of your billing period
- No further charges after the period ends
- Data is retained for 30 days after cancellation
- Re-subscribe within 30 days to retain all data

## Payment

Pixault accepts credit and debit cards processed through CardPointe (PCI Level 1 compliant). Card details are tokenized — Pixault never stores raw card numbers.

## Invoices

Invoices are generated at the start of each billing period and include:

- Base plan charge
- Bandwidth overage charges (if applicable)
- Storage overage charges (if applicable)
- Applicable taxes

View and download invoices from the billing dashboard.

## Need Help?

Contact support at **support@pixault.dev** for:

- Enterprise pricing or custom plans
- Volume discounts
- Technical questions about usage metering
- Billing disputes or refund requests
