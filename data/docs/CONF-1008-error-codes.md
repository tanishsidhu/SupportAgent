---
id: CONF-1008
title: Error Codes
---

Common errors: `CS-4001` invalid payload, `CS-4010` expired token, `CS-4290` rate limited, `CS-5000` internal error. Retry idempotent GETs on 5xx; do not blindly retry POST without an idempotency key.
