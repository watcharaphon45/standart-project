# ADR-079 — Production Readiness Baseline

Status: Accepted for Phase 4.3 engineering deliverables; hosting/domain values, manual screen-reader sign-off, load targets, and audit retention periods remain organization-specific configuration.

## Context

Core functionality (`Auth`, `DBRT01`–`DBRT07`, `SURT01`–`SURT09`, Application Shell, Responsive Data Tables) is complete and verified. Phase 4.3 requires production readiness capabilities—readiness probing, structured logging with trace correlation, response security headers, configuration guardrails, migration dry-run SQL generation, and backup/restore verification—while strictly complying with `docs/cost-policy.md` (zero paid licenses, zero unnecessary external SaaS/telemetry fees, lockfile-pinned packages).

## Decisions

1. **Built-in ASP.NET Core Capabilities Only:**
   Implement health checks, JSON console logging, forwarded headers, security headers middleware, and file-system Data Protection key persistence using built-in `Microsoft.AspNetCore.*` and already-referenced `Npgsql` packages. No new NuGet or npm packages are introduced (`RestorePackagesWithLockFile=true`).
2. **Sanitized Readiness Contract (`/health` and `/health/ready`):**
   Keep `GET /health` as a process liveness probe (`{"status":"ok"}`) and add `GET /health/ready` executing a bounded (5-second timeout) PostgreSQL `SELECT 1` query. Failed readiness returns HTTP `503` with `{"status":"unavailable","checks":{"database":"unavailable"}}` and strictly suppresses connection strings, hostnames, usernames, SQL states, and exception messages. Both health endpoints bypass the `must_change_password` gate so orchestrators can probe while admins complete password changes.
3. **Structured Observability & Correlation Without PII/Secrets:**
   Add `RequestObservabilityMiddleware` emitting `X-Trace-Id` headers on every response (exposed via CORS `X-Trace-Id` header) and unified across `CurrentActor`, `ValidateCsrfFilter`, `GlobalExceptionHandler`, and `Program` ProblemDetails. Request logs record Method, Path (never QueryString, bodies, cookies, or headers), StatusCode, ElapsedMs, numeric `UserId`, and `TraceId`. Production `appsettings.json` configures UTC JSON console logging.
4. **Defensive API Security Headers & Strict Configuration Validation:**
   `SecurityHeadersMiddleware` sets `Cache-Control: no-store`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` on API responses. Startup validates `Cors:AllowedOrigins` (rejecting wildcards, paths/queries, and non-`https://` in non-Development), parses `Cookies:SameSite` (`None`/`Lax`/`Strict`), and supports optional `ReverseProxy` (`KnownProxies`/`KnownIPNetworks`) and `DataProtection:KeyDirectory` settings.
5. **Local Operational Tooling (`scripts/` & `docs/operations/deployment.md`):**
   Provide `Export-Migrations.ps1`, `Backup-Database.ps1`, and `Restore-Database.ps1 -Drill` using PostgreSQL native tools (`pg_dump`/`pg_restore`/`psql`) or free Podman containers (with explicit notice if falling back to an existing legacy Docker container per `docs/cost-policy.md`), avoiding PowerShell pipeline binary corruption and never echoing passwords.

## Consequences

- Zero licensing/cost impact and clean offline/reproducible builds.
- Multi-instance production deployments must mount shared storage for `DataProtection:KeyDirectory` and configure explicit `ReverseProxy:KnownProxies`/`KnownNetworks` behind TLS-terminating proxies.
- Audit archival/purging automation remains a documented manual/scheduled DBA procedure until the organization finalizes statutory retention periods.
