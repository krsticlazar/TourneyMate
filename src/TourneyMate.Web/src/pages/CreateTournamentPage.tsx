import { useState } from 'react';
import { createTournament } from '../api/client';

interface CreateTournamentPageProps {
  onBack: () => void;
  onSuccess: () => void;
}

export default function CreateTournamentPage({ onBack, onSuccess }: CreateTournamentPageProps) {
  const [name, setName] = useState('');
  const [sport, setSport] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      await createTournament(name, sport);
      alert(`Turnir "${name}" uspešno kreiran!`);
      onSuccess();
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.container}>
      <button onClick={onBack} style={styles.backBtn}>
        ← Nazad na Host Panel
      </button>

      <h1>Kreiraj Novi Turnir</h1>

      <form onSubmit={handleSubmit} style={styles.form}>
        <div style={styles.field}>
          <label style={styles.label}>Ime Turnira:</label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            placeholder="npr. Zimska Liga 2026"
            style={styles.input}
          />
        </div>

        <div style={styles.field}>
          <label style={styles.label}>Sport:</label>
          <select
            value={sport}
            onChange={(e) => setSport(e.target.value)}
            required
            style={styles.select}
          >
            <option value="">Izaberi sport...</option>
            <option value="Football">Football</option>
            <option value="Basketball">Basketball</option>
            <option value="Chess">Chess</option>
          </select>
        </div>

        <button type="submit" disabled={loading} style={styles.submitBtn}>
          {loading ? 'Kreiranje...' : 'Kreiraj Turnir'}
        </button>
      </form>
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '600px',
    margin: '0 auto',
  },
  backBtn: {
    padding: '0.5rem 1rem',
    marginBottom: '2rem',
    backgroundColor: '#95a5a6',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1.5rem',
    backgroundColor: 'white',
    padding: '2rem',
    borderRadius: '8px',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: '0.5rem',
  },
  label: {
    fontWeight: 'bold',
    color: '#2c3e50',
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
  submitBtn: {
    padding: '1rem',
    fontSize: '1.1rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontWeight: 'bold',
  },
};
