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
Request: { "destination": "+919876543210", "purpose": "Registration", "code": "123456" }
Response 200: accessToken, refreshToken, accessTokenExpiresAtUtc, userId. OTP is single-use and expires.

## POST /auth/login
Request: { "login": "+919876543210", "password": "..." }. Response: token response.

## POST /auth/refresh
Request: { "refreshToken": "..." }. Response: token response with rotated refresh token.

## POST /auth/logout
Request: { "refreshToken": "..." }. Response: 204. Revokes refresh token.

## POST /families (JWT)
Request includes name, description, defaultLanguage (en/ta), timeZoneId. Response 201 includes familyGroupId, groupCode, name, description, role, status and createdAtUtc. Creator receives Owner membership.

## PUT /families/{familyGroupId} (Owner JWT)
Updates family name, description, default language and time zone. Only active Owner can update.

## POST /families/join (JWT)
Request: { "groupCode": "FAM-ABC123" }. Response 202 with membershipStatus Pending. Only Owner can approve or reject requests:
- GET /families/{familyGroupId}/join-requests
- POST /families/{familyGroupId}/join-requests/{membershipId}/approve
- POST /families/{familyGroupId}/join-requests/{membershipId}/reject
Pending and rejected users have no family access.

## GET /families (JWT)
Returns only active family groups where the current account has active membership.

## GET /families/{familyGroupId}/access (JWT)
Returns { familyGroupId, access: true } for active members. Returns 404 otherwise.

## Family member profiles (JWT)
Family profiles are separate from login accounts. A profile can represent a child, deceased relative, or person without an app account. LinkedUserId is optional and, when supplied, must identify an active member of the same family. A login account can be linked to only one non-archived profile per family.
- GET /families/{familyGroupId}/members — list non-archived profiles; any active family member may read.
- GET /families/{familyGroupId}/members/{familyMemberId} — read one profile; any active family member may read.
- POST /families/{familyGroupId}/members — create profile; Owner, Admin or Editor only. Returns 201.
- PUT /families/{familyGroupId}/members/{familyMemberId} — update profile; Owner, Admin or Editor only. Returns 200.
Fields: FullName (required), TamilName, Gender (Male/Female/Other/PreferNotToSay), DateOfBirth, PlaceOfBirth, DateOfDeath, ContactNumber, Email, LinkedUserId (optional). Status is Active, Deceased or Archived. Archived profiles are hidden from list/get.
An active member cannot access another family's profiles. Inactive/non-members receive 404; Members attempting write operations receive 403.


## Family relationships (JWT)
Relationships belong to one family group. Both endpoint profiles must be non-archived profiles in that same family.
- GET /families/{familyGroupId}/relationships — list relationships; any active family member may read.
- POST /families/{familyGroupId}/relationships — create relationship; Owner, Admin or Editor only. Returns 201.
  Request: { "fromFamilyMemberId": "guid", "toFamilyMemberId": "guid", "relationshipType": "ParentChild" }.
  Supported types: ParentChild (directed from parent to child) and Spouse (stored as an unordered pair).
- DELETE /families/{familyGroupId}/relationships/{relationshipId} — delete relationship; Owner, Admin or Editor only. Returns 204.
Self-links, duplicate links, reverse parent-child contradictions, and cross-family profiles are rejected. Sibling relationships are derived from shared parents rather than stored as independent links.

## Error conventions
400 validation error; 401 unauthenticated/invalid token; 403 insufficient role; 404 missing or concealed cross-family resource; 409 conflict; 429 rate limit. IDs are GUIDs; timestamps are UTC ISO-8601.
