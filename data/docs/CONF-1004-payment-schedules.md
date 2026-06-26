---
id: CONF-1004
title: Payment Schedule Calculation
---

Use `POST /v1/schedules/calculate` with `principal_amount`, `annual_rate`, and `term_months`. Standard leases use actuarial amortization. **Fleet leases** (asset_type `fleet`) may apply a different day-count convention — see partner-specific notes before go-live.
