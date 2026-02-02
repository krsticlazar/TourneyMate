import { useState } from 'react';
import { login, register } from '../api/client';
import type { User } from '../types';

interface AuthModalProps {
  onClose: () => void;
  onLoginSuccess: (user: User) => void;
}

export default function AuthModal({ onClose, onLoginSuccess }: AuthModalProps) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async () => {
    if (!username.trim() || !password.trim()) {
      setError('Unesite username i password');
      return;
    }

    setError('');
    setLoading(true);

    try {
      const response = await login(username, password);
      onLoginSuccess(response.user);
      onClose();
    } catch {
      setPassword('');
      setError('Neispravni kredencijali. Pokušajte ponovo.');
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async () => {
    if (!username.trim() || !password.trim() || !displayName.trim()) {
      setError('Sva polja su obavezna');
      return;
    }

    if (password.length < 6) {
      setError('Password mora imati najmanje 6 karaktera');
      return;
    }

    setError('');
    setLoading(true);

    try {
      await register(username, password, displayName);
      // Auto login nakon registracije
      const response = await login(username, password);
      onLoginSuccess(response.user);
      onClose();
    } catch (err: any) {
      setError(err.message || 'Registracija nije uspela. Username već postoji?');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (mode === 'login') {
      handleLogin();
    } else {
      handleRegister();
    }
  };

  const switchMode = () => {
    setMode(mode === 'login' ? 'register' : 'login');
    setError('');
    setPassword('');
    setDisplayName('');
  };

  return (
    <div style={styles.overlay} onClick={onClose}>
      <div style={styles.modal} onClick={(e) => e.stopPropagation()}>
        <button style={styles.closeBtn} onClick={onClose}>
          ✕
        </button>

        <h2>{mode === 'login' ? 'Prijavi se' : 'Registruj se'}</h2>

        <form onSubmit={handleSubmit} style={styles.form}>
          <input
            type="text"
            placeholder="Username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            style={styles.input}
            disabled={loading}
          />

          {mode === 'register' && (
            <input
              type="text"
              placeholder="Display Name"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              style={styles.input}
              disabled={loading}
            />
          )}

          <input
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={styles.input}
            disabled={loading}
          />

          {error && <p style={styles.error}>{error}</p>}

          <button type="submit" disabled={loading} style={styles.submitBtn}>
            {loading
              ? mode === 'login'
                ? 'Prijavljivanje...'
                : 'Registracija...'
              : mode === 'login'
              ? 'Prijavi se'
              : 'Registruj se'}
          </button>
        </form>

        <p style={styles.switchText}>
          {mode === 'login' ? (
            <>
              Nemate nalog?{' '}
              <span style={styles.switchLink} onClick={switchMode}>
                Registrujte se
              </span>
            </>
          ) : (
            <>
              Već imate nalog?{' '}
              <span style={styles.switchLink} onClick={switchMode}>
                Prijavite se
              </span>
            </>
          )}
        </p>
      </div>
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  overlay: {
    position: 'fixed',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1000,
  },
  modal: {
    backgroundColor: 'white',
    padding: '2rem',
    borderRadius: '8px',
    position: 'relative',
    minWidth: '350px',
    boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
  },
  closeBtn: {
    position: 'absolute',
    top: '10px',
    right: '10px',
    background: 'none',
    border: 'none',
    fontSize: '1.5rem',
    cursor: 'pointer',
    color: '#666',
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
  submitBtn: {
    padding: '0.75rem',
    fontSize: '1rem',
    backgroundColor: '#007bff',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontWeight: 'bold',
  },
  error: {
    color: 'red',
    fontSize: '0.9rem',
    margin: 0,
    textAlign: 'center',
  },
  switchText: {
    marginTop: '1rem',
    textAlign: 'center',
    fontSize: '0.9rem',
    color: '#666',
  },
  switchLink: {
    color: '#007bff',
    cursor: 'pointer',
    textDecoration: 'underline',
  },
};
