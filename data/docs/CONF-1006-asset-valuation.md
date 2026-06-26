---
id: CONF-1006
title: Asset Valuation
---

The valuation service estimates fair market value via `POST /v1/valuations`. Provide `asset_category`, `model_year`, and `mileage_or_hours`. Results include `fmv_amount` and a confidence band. Valuations expire after **72 hours**.
