export type Tokens = { accessToken: string; refreshToken: string; accessTokenExpiresAtUtc: string; userId: string };
export type Family = { familyGroupId: string; groupCode: string; name: string; description?: string | null; role: string; status: string; createdAtUtc: string };
export type FamilyRelationship = {
  familyRelationshipId: string; familyGroupId: string;
  fromFamilyMemberId: string; fromFullName: string; fromTamilName?: string | null;
  toFamilyMemberId: string; toFullName: string; toTamilName?: string | null;
  relationshipType: 'ParentChild' | 'Spouse'; createdAtUtc: string;
};
export type FamilyMember = {
  familyMemberId: string; familyGroupId: string; linkedUserId?: string | null; fullName: string;
  tamilName?: string | null; gender?: string | null; dateOfBirth?: string | null; placeOfBirth?: string | null;
  dateOfDeath?: string | null; contactNumber?: string | null; email?: string | null; status: string;
  createdAtUtc: string; updatedAtUtc: string;
};
const API = '/api/v1';
let accessToken = localStorage.getItem('fc.accessToken') || '';
let refreshToken = localStorage.getItem('fc.refreshToken') || '';
export function saveTokens(tokens: Tokens) {
  accessToken = tokens.accessToken; refreshToken = tokens.refreshToken;
  localStorage.setItem('fc.accessToken', accessToken); localStorage.setItem('fc.refreshToken', refreshToken);
}
export function clearTokens() {
  accessToken = ''; refreshToken = '';
  localStorage.removeItem('fc.accessToken'); localStorage.removeItem('fc.refreshToken');
}
async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  let response = await fetch(API + path, { ...init, headers });
  if (response.status === 401 && refreshToken && !path.includes('/auth/')) {
    const refreshed = await fetch(API + '/auth/refresh', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken }) });
    if (refreshed.ok) {
      saveTokens(await refreshed.json() as Tokens);
      headers.set('Authorization', `Bearer ${accessToken}`);
      response = await fetch(API + path, { ...init, headers });
    } else clearTokens();
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.message || body?.title || `Request failed (${response.status})`);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}
export const api = {
  login: (login: string, password: string) => request<Tokens>('/auth/login', { method: 'POST', body: JSON.stringify({ login, password }) }),
  register: (mobileNumber: string, email: string, password: string, preferredLanguage: string) =>
    request<{ userId: string; verificationRequired: boolean; delivery: string }>('/auth/register', { method: 'POST', body: JSON.stringify({ mobileNumber: mobileNumber || null, email: email || null, password, preferredLanguage }) }),
  verifyOtp: (destination: string, code: string) => request<Tokens>('/auth/otp/verify', { method: 'POST', body: JSON.stringify({ destination, purpose: 'Registration', code }) }),
  logout: async () => { try { await request<void>('/auth/logout', { method: 'POST', body: JSON.stringify({ refreshToken }) }); } finally { clearTokens(); } },
  families: () => request<Family[]>('/families'),
  createFamily: (name: string, description: string, defaultLanguage: string) =>
    request<Family>('/families', { method: 'POST', body: JSON.stringify({ name, description, defaultLanguage, timeZoneId: 'Asia/Kolkata' }) }),
  joinFamily: (groupCode: string) => request<{ name: string; membershipStatus: string }>('/families/join', { method: 'POST', body: JSON.stringify({ groupCode }) }),
  members: (familyId: string) => request<FamilyMember[]>(`/families/${familyId}/members`),
  relationships: (familyId: string) => request<FamilyRelationship[]>(`/families/${familyId}/relationships`),
  createRelationship: (familyId: string, fromFamilyMemberId: string, toFamilyMemberId: string, relationshipType: 'ParentChild' | 'Spouse') =>
    request<FamilyRelationship>(`/families/${familyId}/relationships`, { method: 'POST', body: JSON.stringify({ fromFamilyMemberId, toFamilyMemberId, relationshipType }) }),
  deleteRelationship: (familyId: string, relationshipId: string) =>
    request<void>(`/families/${familyId}/relationships/${relationshipId}`, { method: 'DELETE' }),
  createMember: (familyId: string, payload: Record<string, unknown>) =>
    request<FamilyMember>(`/families/${familyId}/members`, { method: 'POST', body: JSON.stringify(payload) }),
  updateMember: (familyId: string, memberId: string, payload: Record<string, unknown>) =>
    request<FamilyMember>(`/families/${familyId}/members/${memberId}`, { method: 'PUT', body: JSON.stringify(payload) }),
  joinRequests: (familyId: string) => request<Array<{ membershipId: string; userId: string; mobileNumber?: string; email?: string; requestedAtUtc: string }>>(`/families/${familyId}/join-requests`),
  approve: (familyId: string, membershipId: string) => request<void>(`/families/${familyId}/join-requests/${membershipId}/approve`, { method: 'POST' }),
  reject: (familyId: string, membershipId: string) => request<void>(`/families/${familyId}/join-requests/${membershipId}/reject`, { method: 'POST' })
};
export function hasSession() { return Boolean(accessToken); }