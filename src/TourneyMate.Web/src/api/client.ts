import type { LoginResponse, User, HomeResponse, Tournament, ChatMessage, AdminUser, Team } from '../types';

const API_URL = 'http://localhost:5125';

// Helper za čuvanje/čitanje tokena iz sessionStorage (multi-tab support!)
export function getToken(): string | null {
  return sessionStorage.getItem('token');
}

export function setToken(token: string): void {
  sessionStorage.setItem('token', token);
}

export function removeToken(): void {
  sessionStorage.removeItem('token');
  sessionStorage.removeItem('user');
}

export function getUser(): User | null {
  const userStr = sessionStorage.getItem('user');
  return userStr ? JSON.parse(userStr) : null;
}

export function setUser(user: User): void {
  sessionStorage.setItem('user', JSON.stringify(user));
}

// Generic API call funkcija
async function apiCall<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();

  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
      ...(token && { Authorization: `Bearer ${token}` }),
    },
  });

  if (response.status === 401) {
    // Token expired - auto logout
    removeToken();
    window.location.href = '/';
    throw new Error('Unauthorized');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Request failed' }));
    throw new Error(error.error || 'Request failed');
  }

  return response.json();
}

// ========================
// AUTH API
// ========================

export async function login(username: string, password: string): Promise<LoginResponse> {
  const data = await apiCall<LoginResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  });

  // Save token & user to sessionStorage
  setToken(data.token);
  setUser(data.user);

  return data;
}

export async function register(username: string, password: string, displayName: string): Promise<void> {
  return apiCall('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify({ username, password, displayName }),
  });
}

export async function logout(): Promise<void> {
  await apiCall('/api/auth/logout', { method: 'POST' });
  removeToken();
}

export async function getMe(): Promise<User> {
  return apiCall<User>('/api/auth/me');
}

// ========================
// HOME API
// ========================

export async function getHome(topN = 5, chatN = 30): Promise<HomeResponse> {
  return apiCall<HomeResponse>(`/api/home?topN=${topN}&chatN=${chatN}`);
}

// ========================
// TOURNAMENT API
// ========================

export async function getTournament(id: string, topN = 10, chatN = 50): Promise<Tournament> {
  return apiCall<Tournament>(`/api/tournaments/${id}?topN=${topN}&chatN=${chatN}`);
}

export async function updateTournamentScore(
  tournamentId: string,
  teamId: string,
  score: number
): Promise<void> {
  return apiCall(`/api/tournaments/${tournamentId}/score`, {
    method: 'POST',
    body: JSON.stringify({ teamId, score }),
  });
}

export async function createTournament(name: string, sport: string): Promise<void> {
  return apiCall('/api/tournaments/create', {
    method: 'POST',
    body: JSON.stringify({ name, sport }),
  });
}

export async function startTournament(tournamentId: string): Promise<void> {
  return apiCall(`/api/tournaments/${tournamentId}/start`, {
    method: 'POST',
  });
}

export async function finishTournament(tournamentId: string): Promise<void> {
  return apiCall(`/api/tournaments/${tournamentId}/finish`, {
    method: 'POST',
  });
}

// ========================
// TEAM API
// ========================

export async function getMyTeams(): Promise<Team[]> {
  return apiCall<Team[]>('/api/teams/my-teams');
}

export async function createTeam(name: string, sport: string): Promise<Team> {
  return apiCall<Team>('/api/teams', {
    method: 'POST',
    body: JSON.stringify({ name, sport }),
  });
}

export async function applyForTournament(teamId: string, tournamentId: string): Promise<void> {
  return apiCall(`/api/teams/${teamId}/apply/${tournamentId}`, {
    method: 'POST',
  });
}

export async function getApplications(tournamentId: string, status = 'Pending'): Promise<any[]> {
  return apiCall(`/api/teams/applications/${tournamentId}?status=${status}`);
}

export async function approveApplication(tournamentId: string, teamId: string): Promise<void> {
  return apiCall(`/api/teams/applications/${tournamentId}/${teamId}/approve`, {
    method: 'POST',
  });
}

export async function rejectApplication(tournamentId: string, teamId: string): Promise<void> {
  return apiCall(`/api/teams/applications/${tournamentId}/${teamId}/reject`, {
    method: 'POST',
  });
}

// ========================
// CHAT API
// ========================

export async function sendGlobalMessage(text: string): Promise<void> {
  return apiCall('/api/chat/global', {
    method: 'POST',
    body: JSON.stringify({ text }),
  });
}

export async function getGlobalChat(last = 50): Promise<ChatMessage[]> {
  return apiCall<ChatMessage[]>(`/api/chat/global?last=${last}`);
}

export async function sendTournamentMessage(tournamentId: string, text: string): Promise<void> {
  return apiCall(`/api/chat/tournament/${tournamentId}`, {
    method: 'POST',
    body: JSON.stringify({ text }),
  });
}

export async function getTournamentChat(tournamentId: string, last = 50): Promise<ChatMessage[]> {
  return apiCall<ChatMessage[]>(`/api/chat/tournament/${tournamentId}?last=${last}`);
}

// ========================
// ADMIN API
// ========================

export async function getAllUsers(): Promise<AdminUser[]> {
  return apiCall<AdminUser[]>('/api/admin/users');
}

export async function setUserRole(username: string, role: string): Promise<void> {
  return apiCall(`/api/admin/users/${username}/role`, {
    method: 'POST',
    body: JSON.stringify({ role }),
  });
}
