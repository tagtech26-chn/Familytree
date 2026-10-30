# Family Connect — Phase 1 Specification

## Product decisions
- Private multi-family platform.
- Any verified registered user can create a family group.
- Family creator becomes Owner atomically with group creation.
- One login account may join several families and has a separate role in each.
- English (en) and Tamil (ta) from the start.
- Local Windows development, SQL Server Express; future cloud hosting remains possible.

## Stack
- ASP.NET Core Web API on .NET 10.
- SQL Server Express / Azure SQL compatible schema.
- .NET MAUI 10 for Android and iOS.
- React + TypeScript for web administration.
- REST API under /api/v1.
- Modular monolith.

## Phase 1 modules
1. Identity: registration, verified mobile/email, OTP, login, refresh and logout.
2. Family groups: create, list, select and update group.
3. Membership and roles: Owner, Admin, Editor, Member scoped to group.
4. Invitations and member-add requests.
5. Member profiles distinct from login accounts.
6. Directed family relationships and tree view.
7. Events and reminders.
8. Dashboard and administration portal.

## Rules
- Unverified users cannot create groups.
- FamilyGroupId is mandatory on family-owned records.
- All group resources require server-side active membership checks.
- Never accept role or current-user identity from request body as authority.
- OTP codes and refresh tokens are stored as hashes.
- Invitation tokens are single-use, hashed at rest and expiring.
- Timestamps UTC; event dates use date-only values.
- Tamil fields use Unicode-capable SQL nvarchar columns.
- Keep photo albums and media storage for Phase 2.

## Acceptance criteria
- A user registers, verifies OTP and logs in.
- A user creates two groups; both memberships are Owner.
- A user can later be Member of another group without changing role in their own group.
- A user sees only groups with active membership.
- Cross-family resource access returns 404/403 without leaking data.
- English/Tamil UI text is resource-based.
- OTP expires and is single-use; failed attempts are capped.
