# Family Connect

Private multi-family platform for Android, iOS and web.

## Current branch
`feature/phase1-auth-family-groups`

## Phase 1 implementation
- ASP.NET Core Web API (.NET 10)
- SQL Server Express local development
- Registration and password hashing
- Hashed OTP challenges with expiry and attempt limit
- Login and JWT access token
- Persisted hashed refresh token with rotation and logout revocation
- Multiple family group creation
- Creator automatically becomes Owner
- List own family groups and membership access check
- English/Tamil language preferences

## Setup
1. Run `database/scripts/001_create_database.sql` on SQL Server instance `.\SQLEXPRESS`.
2. Select `FamilyConnectDB` and run `database/scripts/002_phase1_identity_families.sql`.
3. Install .NET 10 SDK.
4. From `src/FamilyConnect.API`, run `dotnet user-secrets init`.
5. Set a JWT key:
```powershell
$jwt = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:SigningKey" $jwt
```
6. From repository root: `dotnet restore`, `dotnet build`.
7. Run: `dotnet run --project src/FamilyConnect.API`.

Local API: http://localhost:5080 and https://localhost:7081. Health: /health. OpenAPI: /openapi/v1.json.

## Local OTP
In Development only, OTP is written to the API console. The development sender refuses to run outside Development. Configure a real SMS/email provider before deployment. Do not expose this development API publicly.

## API flow
1. POST /api/v1/auth/register
2. Read generated OTP in API console.
3. POST /api/v1/auth/otp/verify
4. POST /api/v1/auth/login
5. POST /api/v1/families with Bearer access token
6. GET /api/v1/families
7. POST /api/v1/auth/refresh to rotate refresh token
8. POST /api/v1/auth/logout to revoke refresh token

See docs/API_CONTRACTS.md and docs/PROJECT_SPECIFICATION.md.

## Security follow-up before production
Add refresh-token reuse detection and per-device sessions, real SMS/email delivery, resend cooldowns and per-IP throttling, account lockout, audit events, CORS policy and integration tests against SQL Server. Enforce membership and role checks for every family-scoped endpoint.

## Phase 2
Private cloud photo albums, upload authorization, selected-member/photo-level permissions, comments/reactions, thumbnails and media processing.
