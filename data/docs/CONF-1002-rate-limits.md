---
id: CONF-1002
title: Rate Limits
---

Production tenants are limited to **600 requests per minute** per API key. Sandbox keys are capped at **60 requests per minute**. Exceeding the limit returns HTTP 429 with a `Retry-After` header measured in seconds.
