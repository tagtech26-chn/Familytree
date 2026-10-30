# Family Connect tests

Initial unit tests cover:
- Family group creation and Owner membership.
- Rejection of pending/unverified users.
- A single user creating multiple family groups.

Run from repository root:
`dotnet test`

The current tests use EF Core InMemory for service behavior. Add SQL Server integration tests in CI before production.
