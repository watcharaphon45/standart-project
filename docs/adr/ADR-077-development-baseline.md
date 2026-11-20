# ADR-077 — Development baseline
Status: accepted for the initial development milestone.

The user selected MediatR 12.5.0 (Apache-2.0) before installation. Do not
upgrade to a differently licensed major. The latest user requirement permits
only software/services usable without license, subscription or usage fees for
this project, including organizational commercial use. If an upgrade does not
meet that requirement, select a free replacement. See ../cost-policy.md.
Runtime target is net10.0. The installed stable SDK 11.0.100 builds that
target and is pinned in global.json; this does not change the target runtime.

Locked backend packages: EF Core / Identity 10.0.15, Npgsql EF provider
10.0.3, FluentValidation 12.2.0. Package versions were checked against the
NuGet flat-container feed. Frontend: Angular 22.2.6 with CLI/build 22.2.7,
CDK 22.2.7 and Tailwind 4.3.5. Lockfiles capture transitive dependencies.
Tests use xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.12.0, VS runner 4.0.1,
Playwright 1.65.1 and axe-core/playwright 4.14.0.

First milestone: authentication foundation and DBRT01, not all 16 programs.
Other program codes are registered but are not exposed as working menus.
The previous Docker Desktop development setup is superseded by the free-only
policy. Use PostgreSQL directly or Podman for new local setups. Existing Docker
data must be backed up and migrated before switching engines; no existing
container or volume is removed automatically. This is not a hosting decision.
