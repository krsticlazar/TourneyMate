import { useState, useEffect } from 'react';
import { getTournament } from '../api/client';
import TournamentChat from '../components/TournamentChat';
import type { Tournament, User } from '../types';

interface TournamentPageProps {
  tournamentId: string;
  user: User | null;
  onBack: () => void;
}

export default function TournamentPage({ tournamentId, user, onBack }: TournamentPageProps) {
  const [tournament, setTournament] = useState<Tournament | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadTournament();
    const interval = setInterval(loadTournament, 2000); // ⚡ LIVE REFRESH - 2 sec
    return () => clearInterval(interval);
  }, [tournamentId]);

  const loadTournament = async () => {
    try {
      const data = await getTournament(tournamentId, 20, 50);
      setTournament(data);
    } catch (error) {
      console.error('Failed to load tournament:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje turnira...</div>;
  if (!tournament) return <div style={styles.container}>Turnir nije pronađen.</div>;

  return (
    <div style={styles.container}>
      <button onClick={onBack} style={styles.backBtn}>
        ← Nazad
      </button>

      {/* Header */}
      <div style={styles.header}>
        <h1>{tournament.name}</h1>
        <div style={styles.badges}>
          <span style={styles.sportBadge}>{tournament.sport}</span>
          <span style={getStatusStyle(tournament.status)}>{tournament.status}</span>
        </div>
      </div>

      {/* Hosts */}
      <div style={styles.hostsSection}>
        <strong>Hosts:</strong>{' '}
        {tournament.hosts.map((h) => h.displayName).join(', ')}
      </div>

      <div style={styles.content}>
        {/* Left: Leaderboard & Teams */}
        <div style={styles.leftSection}>
          {/* Live Leaderboard */}
          <section style={styles.section}>
            <h2 style={styles.sectionTitle}>
              🏆 Live Leaderboard
              {tournament.status === 'Live' && (
                <span style={styles.liveIndicator}>● LIVE</span>
              )}
            </h2>
            {tournament.leaderboard.length === 0 ? (
              <p style={styles.emptyMessage}>Turnir još nije počeo</p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr style={styles.tableHeader}>
                    <th style={styles.th}>#</th>
                    <th style={styles.th}>Tim</th>
                    <th style={styles.th}>Bodovi</th>
                  </tr>
                </thead>
                <tbody>
                  {tournament.leaderboard.map((entry, idx) => (
                    <tr
                      key={entry.teamId}
                      style={idx < 3 && entry.score > 0 ? styles.topRow : styles.row}
                    >
                      <td style={styles.td}>
                        {idx === 0 && entry.score > 0 && '🥇'}
                        {idx === 1 && entry.score > 0 && '🥈'}
                        {idx === 2 && entry.score > 0 && '🥉'}
                        {(idx > 2 || entry.score === 0) && idx + 1}
                      </td>
                      <td style={styles.td}>{entry.teamName || entry.teamId}</td>
                      <td style={styles.tdScore}>{entry.score}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

          {/* Teams List */}
          <section style={styles.section}>
            <h2 style={styles.sectionTitle}>👥 Učesnici</h2>
            {tournament.enteredTeams.length === 0 ? (
              <p style={styles.emptyMessage}>Nema timova</p>
            ) : (
              <div style={styles.teamsGrid}>
                {tournament.enteredTeams.map((team) => (
                  <div key={team.teamId} style={styles.teamCard}>
                    <strong>{team.name}</strong>
                    <p style={styles.teamSport}>{team.sport}</p>
                  </div>
                ))}
              </div>
            )}
          </section>

        </div>

        {/* Right: Chat */}
        <div style={styles.rightSection}>
          <div style={styles.chatContainer}>
            <h2 style={styles.chatTitle}>
              💬 Tournament Chat
              <span style={styles.liveIndicator}>● LIVE</span>
            </h2>
            <TournamentChat tournamentId={tournamentId} user={user} />
          </div>
        </div>
      </div>
    </div>
  );
}

function getStatusStyle(status: string): React.CSSProperties {
  const base: React.CSSProperties = {
    padding: '0.25rem 0.75rem',
    borderRadius: '12px',
    fontSize: '0.9rem',
    fontWeight: 'bold',
  };

  switch (status) {
    case 'Open':
      return { ...base, backgroundColor: '#27ae60', color: 'white' };
    case 'Live':
      return { ...base, backgroundColor: '#e74c3c', color: 'white' };
    case 'Finished':
      return { ...base, backgroundColor: '#95a5a6', color: 'white' };
    default:
      return { ...base, backgroundColor: '#3498db', color: 'white' };
  }
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '1400px',
    margin: '0 auto',
  },
  backBtn: {
    padding: '0.5rem 1rem',
    marginBottom: '1rem',
    backgroundColor: '#95a5a6',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  header: {
    marginBottom: '1rem',
  },
  badges: {
    display: 'flex',
    gap: '0.5rem',
    marginTop: '0.5rem',
  },
  sportBadge: {
    padding: '0.25rem 0.75rem',
    borderRadius: '12px',
    fontSize: '0.9rem',
    fontWeight: 'bold',
    backgroundColor: '#3498db',
    color: 'white',
  },
  hostsSection: {
    marginBottom: '2rem',
    padding: '1rem',
    backgroundColor: '#f9f9f9',
    borderRadius: '8px',
  },
  content: {
    display: 'grid',
    gridTemplateColumns: '2fr 1fr',
    gap: '2rem',
  },
  leftSection: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2rem',
  },
  rightSection: {},
  section: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1.5rem',
    backgroundColor: 'white',
  },
  sectionTitle: {
    margin: '0 0 1rem 0',
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
  },
  liveIndicator: {
    fontSize: '0.8rem',
    color: '#e74c3c',
    animation: 'pulse 2s infinite',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  tableHeader: {
    backgroundColor: '#2c3e50',
    color: 'white',
  },
  th: {
    padding: '0.75rem',
    textAlign: 'left',
  },
  row: {
    borderBottom: '1px solid #eee',
  },
  topRow: {
    borderBottom: '1px solid #eee',
    backgroundColor: '#fffbea',
  },
  td: {
    padding: '0.75rem',
  },
  tdScore: {
    padding: '0.75rem',
    fontWeight: 'bold',
    color: '#27ae60',
  },
  emptyMessage: {
    textAlign: 'center',
    color: '#999',
    fontStyle: 'italic',
  },
  teamsGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))',
    gap: '1rem',
  },
  teamCard: {
    padding: '1rem',
    border: '1px solid #ddd',
    borderRadius: '8px',
    backgroundColor: '#f9f9f9',
    textAlign: 'center',
  },
  teamSport: {
    margin: '0.5rem 0 0 0',
    fontSize: '0.9rem',
    color: '#666',
  },
  appsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem',
  },
  appCard: {
    padding: '0.75rem',
    backgroundColor: '#fff3cd',
    border: '1px solid #ffc107',
    borderRadius: '4px',
  },
  chatContainer: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1.5rem',
    backgroundColor: 'white',
    position: 'sticky',
    top: '1rem',
  },
  chatTitle: {
    margin: '0 0 1rem 0',
    display: 'flex',
    alignItems: 'center',
    gap: '0.5rem',
  },
};
