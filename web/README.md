# Family Connect Web

React + TypeScript + Vite browser client for the existing Family Connect ASP.NET Core API.

## Requirements
- Node.js 20+
- FamilyConnect.API running with its Development launch profile (HTTPS: https://localhost:7081; HTTP: http://localhost:5080).

## Run locally
```powershell
cd web
npm install
npm run dev
```
Open http://localhost:5173.

The Vite development proxy forwards /api and /health to https://localhost:7081. The proxy accepts the local development certificate; this is only for local development.

## Current scope
- Login and registration with OTP verification.
- Access-token refresh and logout.
- Family list, create family and request to join using a group code.
- Family member list, create and update.
- Owner join-request approval/rejection.
- English/Tamil language switch and responsive desktop/mobile-browser layout.
- UI hides profile write actions unless the active family role is Owner, Admin or Editor. The API remains the authoritative permission check.

OTP in Development is written to the API console. Do not use the development OTP sender in production.

## Notes
- No tokens or API secrets are committed. Tokens are stored in browser localStorage for this initial local test client; use secure HttpOnly cookies or an equivalent hardened token strategy before production deployment.
- The family tree relationship editor, event/reminder screens, profile archive controls and production OTP delivery are not part of this initial web MVP.
