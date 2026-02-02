// Auth types
export interface LoginResponse {
  token: string;
  expiresInSeconds: number;
  user: User;
}

export interface User {
  username: string;
  displayName: string;
  role: 'Viewer' | 'Host' | 'Admin';
}

// Tournament types
export interface Tournament {
  tournamentId: string;
  name: string;
  sport: string;
  status: 'Open' | 'Live' | 'Finished';
  hosts: Host[];
  enteredTeams: Team[];
  applications: Application[];
  leaderboard: LeaderboardEntry[];
  chat?: ChatMessage[];
}

export interface Host {
  username: string;
  displayName: string;
}

export interface Team {
  teamId: string;
  name: string;
  sport: string;
}

export interface Application {
  teamId: string;
  name: string;
  sport: string;
  status: 'Pending' | 'Approved' | 'Rejected';
}

export interface LeaderboardEntry {
  teamId: string;
  teamName: string | null;
  score: number;
}

// Chat types
export interface ChatMessage {
  userId: string;
  displayName: string;
  text: string;
  timestampUtc: string;
}

// Home page types
export interface HomeResponse {
  open: Tournament[];
  live: Tournament[];
  finished: Tournament[];
  globalChat: ChatMessage[];
}

// Admin types
export interface AdminUser {
  username: string;
  displayName: string;
  role: string;
}
