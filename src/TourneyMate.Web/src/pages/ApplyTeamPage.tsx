import { useState, useEffect } from 'react';
import { getMyTeams, createTeam, applyForTournament, getHome } from '../api/client';
import type { Team, Tournament } from '../types';

export default function ApplyTeamPage() {
  const [myTeams, setMyTeams] = useState<Team[]>([]);
  const [allTournaments, setAllTournaments] = useState<Tournament[]>([]);
  const [selectedSport, setSelectedSport] = useState<string>('');
  const [selectedTeam, setSelectedTeam] = useState<string>('');
  const [selectedTournament, setSelectedTournament] = useState<string>('');
  const [loading, setLoading] = useState(true);

  // Create team state
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newTeamName, setNewTeamName] = useState('');
  const [newTeamSport, setNewTeamSport] = useState('');

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      const [teams, home] = await Promise.all([getMyTeams(), getHome()]);
      setMyTeams(teams);
      setAllTournaments([...home.open, ...home.live]);
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTeam = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTeamName || !newTeamSport) return;

    try {
      await createTeam(newTeamName, newTeamSport);
      alert('Tim kreiran!');
      setShowCreateForm(false);
      setNewTeamName('');
      setNewTeamSport('');
      loadData(); // Refresh
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const handleApply = async () => {
    if (!selectedTeam || !selectedTournament) {
      alert('Izaberi tim i turnir!');
      return;
    }

    try {
      await applyForTournament(selectedTeam, selectedTournament);
      alert('Uspešno prijavljen tim!');
      setSelectedTeam('');
      setSelectedTournament('');
      setSelectedSport('');
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  const availableTournaments = selectedSport
    ? allTournaments.filter((t) => t.sport === selectedSport && t.status === 'Open')
    : [];

  const availableTeams = selectedSport ? myTeams.filter((t) => t.sport === selectedSport) : [];

  if (loading) return <div style={styles.container}>Učitavanje...</div>;

  return (
    <div style={styles.container}>
      <h1>Prijavi Tim za Turnir</h1>

      {/* Create Team Section */}
      <section style={styles.section}>
        <h2>Kreiraj Novi Tim</h2>
        {showCreateForm ? (
          <form onSubmit={handleCreateTeam} style={styles.form}>
            <input
              type="text"
              placeholder="Ime tima"
              value={newTeamName}
              onChange={(e) => setNewTeamName(e.target.value)}
              required
              style={styles.input}
            />
            <select
              value={newTeamSport}
              onChange={(e) => setNewTeamSport(e.target.value)}
              required
              style={styles.select}
            >
              <option value="">Izaberi sport...</option>
              <option value="Football">Football</option>
              <option value="Basketball">Basketball</option>
              <option value="Chess">Chess</option>
            </select>
            <div style={styles.formButtons}>
              <button type="submit" style={styles.submitBtn}>
                Kreiraj
              </button>
              <button
                type="button"
                onClick={() => {
                  setShowCreateForm(false);
                  setNewTeamName('');
                  setNewTeamSport('');
                }}
                style={styles.cancelBtn}
              >
                Otkaži
              </button>
            </div>
          </form>
        ) : (
          <button onClick={() => setShowCreateForm(true)} style={styles.createBtn}>
            + Napravi Tim
          </button>
        )}
      </section>

      {/* Apply Section */}
      <section style={styles.section}>
        <h2>Prijavi se za Turnir</h2>

        {/* Step 1: Select Sport */}
        <div style={styles.step}>
          <label style={styles.label}>1. Izaberi sport:</label>
          <select
            value={selectedSport}
            onChange={(e) => {
              setSelectedSport(e.target.value);
              setSelectedTeam('');
              setSelectedTournament('');
            }}
            style={styles.select}
          >
            <option value="">Izaberi sport...</option>
            <option value="Football">Football</option>
            <option value="Basketball">Basketball</option>
            <option value="Chess">Chess</option>
          </select>
        </div>

        {selectedSport && (
          <>
            {/* Step 2: Select Team */}
            <div style={styles.step}>
              <label style={styles.label}>2. Izaberi svoj tim ({selectedSport}):</label>
              {availableTeams.length === 0 ? (
                <p style={styles.noItems}>Nemaš tim za ovaj sport. Kreiraj novi!</p>
              ) : (
                <select
                  value={selectedTeam}
                  onChange={(e) => setSelectedTeam(e.target.value)}
                  style={styles.select}
                >
                  <option value="">Izaberi tim...</option>
                  {availableTeams.map((team) => (
                    <option key={team.teamId} value={team.teamId}>
                      {team.name}
                    </option>
                  ))}
                </select>
              )}
            </div>

            {/* Step 3: Select Tournament */}
            <div style={styles.step}>
              <label style={styles.label}>3. Izaberi turnir ({selectedSport}):</label>
              {availableTournaments.length === 0 ? (
                <p style={styles.noItems}>Nema otvorenih turnira za ovaj sport</p>
              ) : (
                <select
                  value={selectedTournament}
                  onChange={(e) => setSelectedTournament(e.target.value)}
                  style={styles.select}
                >
                  <option value="">Izaberi turnir...</option>
                  {availableTournaments.map((tournament) => (
                    <option key={tournament.tournamentId} value={tournament.tournamentId}>
                      {tournament.name}
                    </option>
                  ))}
                </select>
              )}
            </div>

            {/* Apply Button */}
            {selectedTeam && selectedTournament && (
              <button onClick={handleApply} style={styles.applyBtn}>
                Prijavi Tim
              </button>
            )}
          </>
        )}
      </section>

      {/* My Teams List */}
      <section style={styles.section}>
        <h2>Moji Timovi</h2>
        {myTeams.length === 0 ? (
          <p>Nemaš nijedan tim</p>
        ) : (
          <div style={styles.teamsList}>
            {myTeams.map((team) => (
              <div key={team.teamId} style={styles.teamCard}>
                <h3>{team.name}</h3>
                <p>Sport: {team.sport}</p>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '800px',
    margin: '0 auto',
  },
  section: {
    marginBottom: '3rem',
    padding: '1.5rem',
    border: '1px solid #ddd',
    borderRadius: '8px',
    backgroundColor: 'white',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1rem',
  },
  input: {
    padding: '0.75rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  select: {
    padding: '0.75rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  formButtons: {
    display: 'flex',
    gap: '1rem',
  },
  submitBtn: {
    flex: 1,
    padding: '0.75rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  cancelBtn: {
    flex: 1,
    padding: '0.75rem',
    backgroundColor: '#95a5a6',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  createBtn: {
    padding: '0.75rem 1.5rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  step: {
    marginBottom: '1.5rem',
  },
  label: {
    display: 'block',
    marginBottom: '0.5rem',
    fontWeight: 'bold',
    color: '#2c3e50',
  },
  noItems: {
    color: '#999',
    fontStyle: 'italic',
  },
  applyBtn: {
    width: '100%',
    padding: '1rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1.1rem',
    fontWeight: 'bold',
  },
  teamsList: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
    gap: '1rem',
  },
  teamCard: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1rem',
    backgroundColor: '#f9f9f9',
  },
};
