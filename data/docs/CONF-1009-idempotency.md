---
id: CONF-1009
title: Idempotency Keys
---

Pass `Idempotency-Key` on mutating requests. Keys must be unique per tenant and **expire after 24 hours**. Replays with the same key within the window return the original response without duplicating side effects.
