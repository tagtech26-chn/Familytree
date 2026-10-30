# Family Connect — GitHub handoff

Repository: https://github.com/tagtech26-chn/Familytree

Current working branch: feature/phase1-auth-family-groups

Phase 1 files are committed on this branch. Keep ongoing feature work on this branch and merge to main only after local build, SQL execution and tests have been verified.

## Local Windows setup
1. Clone the repository and checkout this branch.
2. Run database/scripts/001_create_database.sql against .\SQLEXPRESS.
3. Run database/scripts/002_phase1_identity_families.sql in FamilyConnectDB.
4. Install the .NET 10 SDK.
5. Configure Jwt:SigningKey with dotnet user-secrets; never commit secrets.
6. Run dotnet restore, dotnet build and dotnet test.
7. Run the API project and exercise POST /api/v1/auth/register, POST /api/v1/auth/otp/verify, POST /api/v1/auth/login, POST /api/v1/families and GET /api/v1/families.

## Notes
The development OTP sender writes the code to the API console and must not be used outside Development. Review docs/IMPLEMENTATION_STATUS.md for known gaps. Build and tests have not yet been run in the user's Windows environment.
