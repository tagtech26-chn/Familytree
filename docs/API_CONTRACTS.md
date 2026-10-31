# Phase 1 API contracts

Base URL: /api/v1. JSON request/response. Protected routes require Authorization: Bearer <access-token>.

## POST /auth/register
Request:
{
  "mobileNumber": "+919876543210",
  "email": null,
  "password": "Use-A-Long-Unique-Password",
  "preferredLanguage": "en"
}
Response 202:
{ "userId": "guid", "verificationRequired": true, "delivery": "sms" }

At least one of mobileNumber/email is required. Password minimum 12 characters. Language en or ta.

## POST /auth/otp/verify
Request:
{ "destination": "+919876543210", "purpose": "Registration", "code": "123456" }
Response 200:
{
  "accessToken": "...",
  "refreshToken": "...",
  "accessTokenExpiresAtUtc": "2026-10-30T12:00:00Z",
  "userId": "guid"
}
OTP is single-use and expires. Enforce rate limits and max failed attempts.

## POST /auth/login
Request: { "login": "+919876543210", "password": "..." }
Response: same token response.

## POST /auth/refresh
Request: { "refreshToken": "..." }
Response: same token response with rotated refresh token.

## POST /auth/logout
Request: { "refreshToken": "..." }
Response: 204. Revokes refresh token.

## POST /families (JWT)
Request:
{
  "name": "Aravind Family",
  "description": "Private family group",
  "defaultLanguage": "ta",
  "timeZoneId": "Asia/Kolkata"
}
Response 201:
{
  "familyGroupId": "guid",
  "groupCode": "FAM-ABC123",
  "name": "Aravind Family",
  "description": "Private family group",
  "role": "Owner",
  "status": "Active",
  "createdAtUtc": "..."
}
Creator's Owner membership must be created in the same database transaction as the family group.

## POST /families/join (JWT)
Request:
{ "groupCode": "FAM-ABC123" }
Response 200: FamilyResponse with role "Member".
The account must be active. Group codes are case-insensitive. Existing active members receive 409 ALREADY_A_MEMBER. Unknown or inactive families return 404. A suspended membership cannot rejoin. A prior Left or Invited membership is reactivated as Member.

## GET /families (JWT)
Returns only active family groups where the current account has active membership.

## GET /families/{familyGroupId}/access (JWT)
Returns { familyGroupId, access: true } for active members. Return 404 if not a member to avoid disclosing existence.

## Error conventions
- 400 VALIDATION_ERROR
- 401 INVALID_CREDENTIALS / INVALID_OR_EXPIRED_OTP
- 403 authenticated but insufficient permission
- 404 missing or concealed cross-family resource
- 409 ACCOUNT_ALREADY_EXISTS
- 429 rate limit reached

All IDs are GUIDs. Timestamps are UTC ISO-8601. Family event date fields later use YYYY-MM-DD.
