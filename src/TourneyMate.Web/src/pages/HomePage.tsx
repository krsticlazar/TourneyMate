import { useState, useEffect } from 'react';
import { getHome } from '../api/client';
import GlobalChat from '../components/GlobalChat';
import type { Tournament, User } from '../types';

interface HomePageProps {
  user: User | null;
  onTournamentClick: (tournamentId: string) => void;
}

export default function HomePage({ user, onTournamentClick }: HomePageProps) {
  const [open, setOpen] = useState<Tournament[]>([]);
  const [live, setLive] = useState<Tournament[]>([]);
  const [finished, setFinished] = useState<Tournament[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadHome();
  }, []);

  const loadHome = async () => {
    try {
      const data = await getHome(5, 30);
      setOpen(data.open);
      setLive(data.live);
      setFinished(data.finished);
    } catch (error) {
      console.error('Failed to load home:', error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje...</div>;

  return (
    <div style={styles.container}>
      <div style={styles.tournamentsSection}>
        <h1>Turniri</h1>

        <section style={styles.section}>
          <h2 style={styles.statusTitle}>Otvoreni Turniri</h2>
          {open.length === 0 ? (
            <p>Nema otvorenih turnira</p>
          ) : (
            <div style={styles.tournamentGrid}>
              {open.map((t) => (
                <TournamentCard
                  key={t.tournamentId}
                  tournament={t}
                  onClick={() => onTournamentClick(t.tournamentId)}
                />
              ))}
            </div>
          )}
        </section>

        <section style={styles.section}>
          <h2 style={styles.statusTitle}>Live Turniri</h2>
          {live.length === 0 ? (
            <p>Nema live turnira</p>
          ) : (
            <div style={styles.tournamentGrid}>
              {live.map((t) => (
                <TournamentCard
                  key={t.tournamentId}
                  tournament={t}
                  onClick={() => onTournamentClick(t.tournamentId)}
                />
              ))}
            </div>
          )}
        </section>

        <section style={styles.section}>
          <h2 style={styles.statusTitle}>Završeni Turniri</h2>
          {finished.length === 0 ? (
            <p>Nema završenih turnira</p>
          ) : (
            <div style={styles.tournamentGrid}>
              {finished.map((t) => (
                <TournamentCard
                  key={t.tournamentId}
                  tournament={t}
                  onClick={() => onTournamentClick(t.tournamentId)}
                />
              ))}
            </div>
          )}
        </section>
      </div>

      <div style={styles.chatSection}>
        <GlobalChat user={user} />
      </div>
    </div>
  );
}

function TournamentCard({ tournament, onClick }: { tournament: Tournament; onClick: () => void }) {
  const hosts = tournament.hosts ?? [];
  const enteredTeamsCount = tournament.enteredTeams ? tournament.enteredTeams.length : 0;
  const leaderboard = tournament.leaderboard ?? [];

  return (
    <div style={styles.card} onClick={onClick}>
      <h3 style={styles.cardTitle}>{tournament.name}</h3>
      <p>
        <strong>Sport:</strong> {tournament.sport}
      </p>
      <p>
        <strong>Status:</strong> {tournament.status}
      </p>
      <p>
        <strong>Hosts:</strong> {hosts.map((h) => h.displayName).join(', ')}
      </p>
      <p>
        <strong>Timovi:</strong> {enteredTeamsCount}
      </p>
      {leaderboard.length > 0 && (
        <div>
          <strong>Top 3:</strong>
          <ol style={styles.leaderboard}>
            {leaderboard.slice(0, 3).map((entry, idx) => (
              <li key={idx}>
                {entry.teamName || entry.teamId} - {entry.score}
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    display: 'flex',
    gap: '2rem',
    padding: '2rem',
    maxWidth: '1400px',
    margin: '0 auto',
  },
  tournamentsSection: {
    flex: 2,
  },
  chatSection: {
    flex: 1,
  },
  section: {
    marginBottom: '2rem',
  },
  statusTitle: {
    color: '#2c3e50',
    borderBottom: '2px solid #3498db',
    paddingBottom: '0.5rem',
    marginBottom: '1rem',
  },
  tournamentGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))',
    gap: '1rem',
  },
  card: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1rem',
    backgroundColor: 'white',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
    cursor: 'pointer',
    transition: 'all 0.2s',
  },
  cardTitle: {
    margin: '0 0 1rem 0',
    color: '#2c3e50',
  },
  leaderboard: {
    marginTop: '0.5rem',
    paddingLeft: '1.5rem',
  },
};
