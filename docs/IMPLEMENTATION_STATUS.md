# Phase 1 implementation status

## Committed in this branch
- Solution and .NET project structure for API, Domain, Application, Infrastructure.
- SQL Server Express database creation and Phase 1 identity/family schema.
- Registration, password hashing, OTP challenge generation/verification.
- JWT access token issuance.
- Persisted SHA-256 refresh token hashes, 30-day expiry, refresh rotation and logout revocation.
- Multi-family creation; creator receives Owner membership.
- Family-code join requests are Pending until Owner approval.
- Owner-only pending request list, approve and reject endpoints.
- List current user's active family groups; only Active memberships are returned.
- Membership access checks require both Active membership and Active family status.
- SQL migration 003 adds Pending membership status.
- OpenAPI in Development, health endpoint, local launch profile.
- Initial xUnit family creation tests.
- Project specification, architecture and API contracts.

## Local setup and checks
See README.md. This branch has not yet been compiled against the user's Windows/.NET environment. Run:
`dotnet restore`
`dotnet build`
`dotnet test`

## Known gaps before production
- Development OTP sender prints OTP to API logs; replace with SMS/email provider.
- Registration with both mobile and email currently verifies the primary destination (mobile takes precedence); add separate verification challenges for both.
- Add OTP resend cooldown, per-IP rate limiting, account lockout and production abuse controls.
- Refresh lookup currently loads non-revoked unexpired refresh token records and compares hashes in constant time in application memory. For larger user bases, implement indexed keyed lookup safely (e.g. HMAC digest) and per-device session records.
- Add refresh-token reuse detection and revoke all sessions on password reset/account suspension.
- Family access endpoint is present; apply membership and role authorization to every future family-scoped controller.
- Add API integration tests against SQL Server and end-to-end auth tests.
- Validate family code generation collision handling at database insert and retry on unique constraint.
- Add transactional outbox/audit records and reliable notification delivery.
- Add CORS, HTTPS certificate trust on local mobile devices, structured redacted logging and secrets handling.
- No MAUI screens or React web UI have been committed in this initial backend slice.
