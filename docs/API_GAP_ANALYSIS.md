# API Gap Analysis: Reporting & Editing Coverage

Date: 2026-05-05

## Scope reviewed
- `src/VoiceAgent.Api/Controllers/*.cs`
- Domain entities for reporting/editing-adjacent concerns:
  - Calls/recordings/cost logs/outbound (`src/VoiceAgent.Domain/Calls`, `src/VoiceAgent.Domain/Outbound`)
  - Restaurant deal composition (`src/VoiceAgent.Domain/Restaurants`)
  - Billing/audit (`src/VoiceAgent.Domain/Billing`)

## Quick status by requested area

| Area requested | Current status | Notes |
|---|---|---|
| PUT/PATCH update endpoints | **Partial** | Only `ContactUsController` has `PATCH` for status. Most resource controllers are create/list only. |
| DELETE / soft-delete endpoints | **Missing** | No `HttpDelete` routes found in API controllers. |
| Pagination/filtering endpoints | **Mostly missing** | Shared pagination DTOs exist, but controllers mostly expose simple list/by-id routes without standard pageable query contracts. |
| Deal item/addon/choice-group endpoints | **Missing (dedicated endpoints)** | `DealsController` supports create + list by client, but no explicit CRUD endpoints for `RestaurantDealItem`, `RestaurantDealAddon`, `RestaurantDealChoiceGroup`. |
| Recording upload/read URL endpoints | **Missing (public API)** | Recording domain/infrastructure exists, but no explicit upload URL or signed read URL endpoints in controllers. |
| Billing/cost report endpoints | **Missing (public API)** | Cost-related domain/services/worker exist, but no dedicated reporting controller endpoints for summarized billing/cost analytics. |
| Outbound campaign run/lead endpoints | **Missing (public API)** | Outbound domain entities and worker/orchestrator exist, but no public CRUD/reporting endpoints for run/lead lifecycle. |

## Existing endpoint inventory (high-level)

- Create/list pattern present for core config resources:
  - tenants, clients, branches, campaigns, campaign-configurations, external-api-configurations, menus, deals, courier-pricing, knowledge-base.
- Call query endpoints present:
  - list calls, call by id, turns/events/tool-logs.
- ContactUs has richer management:
  - list with query model, get by id, patch status, status-summary.

## What seems missing in **editing**

1. **No consistent update contract per aggregate**
   - Most controllers do not expose `PUT` or `PATCH` for resource updates.
   - Missing optimistic concurrency/version update strategy in API contracts.

2. **No delete semantics (hard/soft)**
   - No controller-level delete routes.
   - Soft-delete policy fields exist at base entity level, but not exposed via API behavior standards.

3. **No nested resource editing for restaurant deal composition**
   - No endpoints for adding/removing/updating deal items, addons, and choice groups independently.

4. **No edition workflow conventions**
   - No explicit draft/publish lifecycle endpoints for mutable config objects (menus/deals/campaign configs), if intended.

## What seems missing in **reporting**

1. **Billing/cost reporting APIs**
   - No endpoints for cost over time, by tenant/client/campaign, by provider, or per call-session rollups.

2. **Outbound performance reporting APIs**
   - No endpoints for campaign run summaries, lead-level outcomes, retry metrics, disposition funnels.

3. **Recording access/reporting APIs**
   - No explicit API to request upload URLs, finalize recordings, or fetch secure playback/download URLs.

4. **Standardized pagination/filter/sort for heavy lists**
   - Reusable pagination models exist in `VoiceAgent.Common`, but not consistently used by reporting-style list endpoints.

## Priority recommendations (planning level)

1. Establish a **CRUD baseline matrix** per aggregate (Create, Read list/by-id, Update, Delete/Soft-delete).
2. Add a reusable **query contract convention** for list/report endpoints (pagination + filters + sorts).
3. Define explicit **reporting bounded contexts**:
   - Billing reports
   - Outbound campaign analytics
   - Call/recording operational reporting
4. Add dedicated **deal composition sub-resource APIs** for item/addon/choice-group operations.
5. Add **recording storage APIs** for upload/read URL workflows with tenant-scoped authorization.

## Suggested missing endpoint groups to plan next

- Editing
  - `PUT/PATCH /api/{resource}/{id}` for tenants/clients/branches/campaigns/menus/deals/configurations.
  - `DELETE /api/{resource}/{id}` (hard delete or soft-delete toggle policy).

- Restaurant deals
  - `/api/deals/{dealId}/items`
  - `/api/deals/{dealId}/addons`
  - `/api/deals/{dealId}/choice-groups`

- Recording
  - `/api/recordings/upload-url`
  - `/api/recordings/{recordingId}/read-url`

- Billing/reporting
  - `/api/reports/billing/costs`
  - `/api/reports/calls/usage`

- Outbound
  - `/api/outbound/runs`
  - `/api/outbound/runs/{runId}/leads`
  - `/api/reports/outbound/performance`
