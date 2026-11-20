# ADR-078 — Company slice implementation decisions
Status: implementation baseline, subject to review before extending SU.

- Company codes are globally unique, including inactive records. Name is a
  single required field. No TaxId is collected in this slice.
- A bigint Version is an optimistic concurrency token and is sent as a JSON
  string. An old client version is rejected; no silent write retry occurs.
- Company updates lock the parent row. DB triggers permanently lock Code when
  a Branch, company Role or CompanyAssignment first references it. References
  require active companies. Audit events alone do not lock Code.
- DB and SU contexts share one scoped NpgsqlConnection and an explicit
  serializable transaction for Company writes and allowlisted Audit.
- Each User/Company has one business Role. Its company must match via a
  composite database foreign key. Only company-level scope authorizes
  company-wide DBRT01 actions. Branch/department screens and multiple narrow
  scope grants are not yet implemented and must not inherit company access.
- Creating a company requires an explicit global DBRT01.CREATE permission.
  A separate global CompanyRegistrar Role is used for bootstrap and company
  lifecycle operations after all direct assignments have been revoked.
  It explicitly contains DBRT01 permissions only: account administration,
  shared-master administration and audit access do not imply this permission.
  Ordinary company roles cannot create a new authorization boundary.
- The company switcher lists only authorized company metadata. Search receives
  CompanyId and returns records in that single company; each Detail/Write
  contains one ID and checks access independently. Company selection is tab
  state, not a shared mutable server-session field.
- The first-user bootstrap is an explicit local administration command, requires
  an empty user store and an administrator-supplied temporary password, screens
  the password with HIBP and forces change within 24 hours. No default password,
  sign-up endpoint, production demo login or hardcoded authentication bypass.
- Sessions are server-side, checked every request against account status,
  security stamp and a fingerprint of current Role/Permission/Assignments.
  Any change invalidates the old session. Idle 30 minutes, absolute 8 hours.
- Self-service password change revokes all sessions. Successful login obtains
  a new antiforgery token because the token is bound to the authenticated user.
- Local frontend/API use separate HTTPS origins. Actual production domains,
  cookie/browser compatibility and audit retention remain deferred.
