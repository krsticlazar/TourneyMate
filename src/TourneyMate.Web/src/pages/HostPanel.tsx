import { useState, useEffect } from 'react';
import { getHome, getApplications, approveApplication, rejectApplication } from '../api/client';
import type { Tournament, User } from '../types';

interface HostPanelProps {
  user: User;
  onManageTournament: (tournamentId: string) => void;
  onCreateTournament: () => void;
}

export default function HostPanel({ user, onManageTournament, onCreateTournament }: HostPanelProps) {
  const [myTournaments, setMyTournaments] = useState<Tournament[]>([]);
  const [selectedTournament, setSelectedTournament] = useState<string | null>(null);
  const [applications, setApplications] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadMyTournaments();
  }, []);

  const loadMyTournaments = async () => {
    try {
      const data = await getHome();
      const all = [...data.open, ...data.live, ...data.finished];
      
      // Filtriraj samo turnire gde je trenutni user host
      const myTournaments = all.filter((tournament) =>
        tournament.hosts.some((host) => host.username === user.username)
      );
      
      setMyTournaments(myTournaments);
    } catch (error) {
      console.error('Failed to load tournaments:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadApplications = async (tournamentId: string) => {
    try {
      const data = await getApplications(tournamentId, 'Pending');
      setApplications(data);
      setSelectedTournament(tournamentId);
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const handleApprove = async (tournamentId: string, teamId: string) => {
    try {
      await approveApplication(tournamentId, teamId);
      alert('Tim odobren!');
      loadApplications(tournamentId); // Refresh
      loadMyTournaments(); // Refresh tournaments
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const handleReject = async (tournamentId: string, teamId: string) => {
    try {
      await rejectApplication(tournamentId, teamId);
      alert('Tim odbijen!');
      loadApplications(tournamentId); // Refresh
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje...</div>;

  return (
    <div style={styles.container}>
      <h1>Host Panel</h1>
      <p style={styles.subtitle}>Upravljanje turnirima i aplikacijama</p>

      <button onClick={onCreateTournament} style={styles.createTournamentBtn}>
        + Kreiraj Novi Turnir
      </button>

      <div style={styles.content}>
        <div style={styles.tournamentsList}>
          <h2>Moji Turniri</h2>
          {myTournaments.length === 0 ? (
            <p>Ne hostuješ nijedan turnir</p>
          ) : (
            myTournaments.map((t) => (
              <div
                key={t.tournamentId}
                style={
                  selectedTournament === t.tournamentId
                    ? styles.selectedTournamentCard
                    : styles.tournamentCard
                }
              >
                <div onClick={() => loadApplications(t.tournamentId)} style={{ cursor: 'pointer' }}>
                  <h3>{t.name}</h3>
                  <p>
                    Sport: {t.sport} | Status: {t.status}
                  </p>
                  <p>Timovi: {t.enteredTeams.length}</p>
                  <p>Pending aplikacije: {t.applications.filter((a) => a.status === 'Pending').length}</p>
                </div>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    onManageTournament(t.tournamentId);
                  }}
                  style={styles.manageBtn}
                >
                  Upravljaj Turnirom
                </button>
              </div>
            ))
          )}
        </div>

        <div style={styles.applicationsSection}>
          {selectedTournament ? (
            <>
              <h2>Pending Aplikacije</h2>
              {applications.length === 0 ? (
                <p>Nema pending aplikacija</p>
              ) : (
                applications.map((app) => (
                  <div key={app.teamId} style={styles.applicationCard}>
                    <div>
                      <h3>{app.name}</h3>
                      <p>Sport: {app.sport}</p>
                      <p>Status: {app.status}</p>
                    </div>
                    <div style={styles.actions}>
                      <button
                        onClick={() => handleApprove(selectedTournament, app.teamId)}
                        style={styles.approveBtn}
                      >
                        Odobri
                      </button>
                      <button
                        onClick={() => handleReject(selectedTournament, app.teamId)}
                        style={styles.rejectBtn}
                      >
                        Odbij
                      </button>
                    </div>
                  </div>
                ))
              )}
            </>
          ) : (
            <p style={styles.placeholder}>Izaberi turnir da vidiš aplikacije</p>
          )}
        </div>
      </div>
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '1200px',
    margin: '0 auto',
  },
  subtitle: {
    color: '#666',
    marginBottom: '1rem',
  },
  createTournamentBtn: {
    padding: '0.75rem 1.5rem',
    marginBottom: '2rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
    fontWeight: 'bold',
  },
  
  content: {
    display: 'grid',
    gridTemplateColumns: '1fr 2fr',
    gap: '2rem',
  },
  tournamentsList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  tournamentCard: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1rem',
    backgroundColor: 'white',
    cursor: 'pointer',
    transition: 'all 0.2s',
  },
  selectedTournamentCard: {
    border: '2px solid #3498db',
    borderRadius: '8px',
    padding: '1rem',
    backgroundColor: '#e8f4f8',
    cursor: 'pointer',
  },
  applicationsSection: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1.5rem',
    backgroundColor: 'white',
    minHeight: '400px',
  },
  applicationCard: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1rem',
    marginBottom: '1rem',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    backgroundColor: '#f9f9f9',
  },
  actions: {
    display: 'flex',
    gap: '0.5rem',
  },
  approveBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  rejectBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#e74c3c',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  placeholder: {
    textAlign: 'center',
    color: '#999',
    marginTop: '2rem',
  },
  manageBtn: {
    marginTop: '1rem',
    padding: '0.5rem 1rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    width: '100%',
  },
};
