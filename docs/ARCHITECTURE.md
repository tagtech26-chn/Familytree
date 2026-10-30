# Family Connect Architecture

## Components
- MAUI client (Android/iOS)
- React + TypeScript responsive web client
- ASP.NET Core REST API
- Application/domain layers
- Infrastructure adapters for SQL Server, OTP, email and push
- Worker for scheduled reminders; media processing comes in Phase 2

## Backend modules
Identity, Families, Memberships, Invitations, Members, Relationships, Events, Dashboard, Notifications, Audit.

## Family isolation
Every family-owned entity carries FamilyGroupId. Derive user ID from the validated JWT subject. For every family-scoped request, verify active membership for that user and requested FamilyGroupId before reading or mutating data. Roles are scoped to each group.

## Identity
User account and family member profile are distinct. An account may join multiple groups. Member profiles can represent children, deceased relatives and people without logins. Link an account to a profile only after invitation acceptance/explicit verification.

## Roles
Owner, Admin, Editor, Member. The creator is inserted as Owner in the same database transaction as family creation.

## Localization
Use resource-based UI localization (en, ta). Store preference on user and default language on family. Store Tamil content in NVARCHAR. Do not translate names.

## Phase 2 media
Private Azure Blob containers; API issues short-lived scoped upload/download authorization only after server-side permission checks. SQL stores metadata and access grants, never public blob URLs.

## Local development
Windows + Visual Studio + .NET 10 + SQL Server Express instance .\SQLEXPRESS. Use user-secrets for JWT signing keys. No production secrets in appsettings or repository.
