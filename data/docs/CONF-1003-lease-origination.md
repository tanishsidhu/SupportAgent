---
id: CONF-1003
title: Lease Origination
---

Submit a new lease application with `POST /v1/leases`. Required fields are `asset_type`, `term_months`, `principal_amount`, and `customer_ref`. The API returns a `lease_id` prefixed with `LE-` and status `pending_approval`.
