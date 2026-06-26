---
id: CONF-1001
title: API Authentication
---

CapStream API requests require a Bearer token in the `Authorization` header. Tokens are issued from the CapStream Developer Portal under **Settings → API Keys**. Read-only integrations need the `leases:read` scope; write operations require `leases:write`.
