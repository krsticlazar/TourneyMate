import { useState, useEffect } from 'react';
import { getTournament, updateTournamentScore, startTournament, finishTournament } from '../api/client';
import TournamentChat from '../components/TournamentChat';
import type { Tournament, User } from '../types';

interface TournamentManagementProps {
  tournamentId: string;
  user: User;
  onBack: () => void;
}

export default function TournamentManagement({ tournamentId, user, onBack }: TournamentManagementProps) {
  const [tournament, setTournament] = useState<Tournament | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedTeam, setSelectedTeam] = useState<string>('');
  const [scoreInput, setScoreInput] = useState<string>('0');

  useEffect(() => {
    loadTournament();
    const interval = setInterval(loadTournament, 2000);
    return () => clearInterval(interval);
  }, [tournamentId]);

  const loadTournament = async () => {
    try {
      const data = await getTournament(tournamentId);
      setTournament(data);
    } catch (error) {
      console.error('Failed to load tournament:', error);
    } finally {
      setLoading(false);
    }
  };

  const getCurrentScore = (): number => {
    if (!tournament || !selectedTeam) return 0;
    const entry = tournament.leaderboard.find(e => e.teamId === selectedTeam);
    return entry?.score || 0;
  };

  const handleAddPoints = (points: number) => {
    if (!selectedTeam) {
      alert('Izaberi tim prvo!');
      return;
    }
    const currentScore = getCurrentScore();
    const newScore = currentScore + points;
    setScoreInput(newScore.toString());
  };

  const handleUpdateScore = async () => {
    if (!selectedTeam || !scoreInput) {
      alert('Izaberi tim i unesi bodove!');
      return;
    }

    const score = parseFloat(scoreInput);
    if (isNaN(score) || score < 0) {
      alert('Nevažeći broj!');
      return;
    }

    try {
      await updateTournamentScore(tournamentId, selectedTeam, score);
      alert('Bodovi ažurirani!');
      setScoreInput('0');
      setSelectedTeam('');
      await loadTournament();
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const handleStart = async () => {
    if (!confirm('Da li ste sigurni da želite da startujete turnir?')) return;

    try {
      await startTournament(tournamentId);
      alert('Turnir je startovan!');
      await loadTournament();
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const handleFinish = async () => {
    if (!confirm('Da li ste sigurni da želite da završite turnir?')) return;

    try {
      await finishTournament(tournamentId);
      alert('Turnir je završen!');
      await loadTournament();
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje...</div>;
  if (!tournament) return <div style={styles.container}>Turnir nije pronađen.</div>;

  return (
    <div style={styles.container}>
      <button onClick={onBack} style={styles.backBtn}>
        ← Nazad na Host Panel
      </button>

      <h1>{tournament.name}</h1>
      <p>
        Sport: {tournament.sport} | Status: <strong>{tournament.status}</strong>
      </p>

      {/* Status Control */}
      <div style={styles.statusControl}>
        {tournament.status === 'Open' && (
          <button onClick={handleStart} style={styles.startBtn}>
            ▶ Startuj Turnir
          </button>
        )}
        {tournament.status === 'Live' && (
          <button onClick={handleFinish} style={styles.finishBtn}>
            ⏹ Završi Turnir
          </button>
        )}
        {tournament.status === 'Finished' && (
          <p style={styles.finishedMsg}>Turnir je završen</p>
        )}
      </div>

      <div style={styles.content}>
        {/* Left: Leaderboard & Score Management */}
        <div style={styles.leftSection}>
          <section style={styles.section}>
            <h2>Leaderboard</h2>
            {tournament.leaderboard.filter(e => e.score > 0).length === 0 ? (
              <p>Nema rezultata</p>
            ) : (
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>#</th>
                    <th style={styles.th}>Tim</th>
                    <th style={styles.th}>Bodovi</th>
                  </tr>
                </thead>
                <tbody>
                  {tournament.leaderboard.map((entry, idx) => (
                    <tr key={entry.teamId} style={styles.row}>
                      <td style={styles.td}>{idx + 1}</td>
                      <td style={styles.td}>{entry.teamName || entry.teamId}</td>
                      <td style={styles.td}>{entry.score}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>

          {tournament.status !== 'Finished' && (
            <section style={styles.section}>
              <h2>Dodaj/Ažuriraj Bodove</h2>
              <div style={styles.scoreForm}>
                <select
                  value={selectedTeam}
                  onChange={(e) => {
                    setSelectedTeam(e.target.value);
                    const currentScore = tournament.leaderboard.find(lb => lb.teamId === e.target.value)?.score || 0;
                    setScoreInput(currentScore.toString());
                  }}
                  style={styles.select}
                >
                  <option value="">Izaberi tim...</option>
                  {tournament.enteredTeams.map((team) => (
                    <option key={team.teamId} value={team.teamId}>
                      {team.name}
                    </option>
                  ))}
                </select>

                {selectedTeam && (
                  <>
                    <div style={styles.quickButtons}>
                      <button onClick={() => handleAddPoints(1)} style={styles.quickBtn}>
                        +1
                      </button>
                      <button onClick={() => handleAddPoints(2)} style={styles.quickBtn}>
                        +2
                      </button>
                      <button onClick={() => handleAddPoints(3)} style={styles.quickBtn}>
                        +3
                      </button>
                    </div>

                    <input
                      type="number"
                      step="0.1"
                      placeholder="Bodovi"
                      value={scoreInput}
                      onChange={(e) => setScoreInput(e.target.value)}
                      style={styles.input}
                    />
                    <button onClick={handleUpdateScore} style={styles.updateBtn}>
                      Ažuriraj Bodove
                    </button>
                  </>
                )}
              </div>
            </section>
          )}
        </div>

        {/* Right: Chat */}
        <div style={styles.rightSection}>
          <TournamentChat tournamentId={tournamentId} user={user} />
        </div>
      </div>
    </div>
  );
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
  statusControl: {
    marginBottom: '2rem',
  },
  startBtn: {
    padding: '0.75rem 1.5rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
    fontWeight: 'bold',
  },
  finishBtn: {
    padding: '0.75rem 1.5rem',
    backgroundColor: '#e74c3c',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
    fontWeight: 'bold',
  },
  finishedMsg: {
    color: '#95a5a6',
    fontStyle: 'italic',
  },
  content: {
    display: 'grid',
    gridTemplateColumns: '2fr 1fr',
    gap: '2rem',
    marginTop: '2rem',
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
  table: {
    width: '100%',
    borderCollapse: 'collapse',
  },
  th: {
    padding: '0.75rem',
    textAlign: 'left',
    borderBottom: '2px solid #ddd',
    backgroundColor: '#f9f9f9',
  },
  row: {
    borderBottom: '1px solid #eee',
  },
  td: {
    padding: '0.75rem',
  },
  scoreForm: {
    display: 'flex',
    gap: '0.5rem',
    flexDirection: 'column',
  },
  select: {
    padding: '0.75rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  quickButtons: {
    display: 'flex',
    gap: '0.5rem',
  },
  quickBtn: {
    flex: 1,
    padding: '0.75rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
    fontWeight: 'bold',
  },
  input: {
    padding: '0.75rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  updateBtn: {
    padding: '0.75rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
};
