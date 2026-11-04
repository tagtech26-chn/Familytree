# Phase 1 implementation status

## Committed in this branch
- Solution and .NET project structure for API, Domain, Application and Infrastructure.
- SQL Server Express identity and family schema.
- Registration, password hashing, OTP verification, JWT access tokens, refresh rotation and logout.
- Multi-family creation; creator receives Owner membership.
- Owner-only family detail update.
- Family-code join requests remain Pending until Owner approval; Owner-only list, approve and reject.
- Active family listing and membership access checks.
- Family member profile entity and SQL migration 006.
- Family member list/get for active members; create/update restricted to Owner, Admin and Editor.
- Optional login-account linking, scoped to an active membership in the same family.
- API contracts and xUnit service tests.

## Local setup and checks
Run from repository root: dotnet restore; dotnet build; dotnet test.
Apply database scripts in numeric order, including database/scripts/006_family_member_profiles.sql.

## Known gaps before production
- Development OTP sender prints OTP to API logs; replace with SMS/email provider.
- Add separate verification challenges for both mobile and email when both are supplied.
- Add OTP resend cooldown, account lockout and production abuse controls.
- Add refresh-token reuse detection and revoke sessions on password reset/account suspension.
- Add SQL Server integration tests and end-to-end auth tests.
- Add audit records and reliable notification delivery.
- Add CORS, structured redacted logging and production secrets handling.
- Photo albums/media and client applications remain later phases.
