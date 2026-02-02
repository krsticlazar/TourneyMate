import { logout } from '../api/client';
import type { User } from '../types';

interface HeaderProps {
  user: User | null;
  onLoginClick: () => void;
  onLogout: () => void;
  onNavigate: (page: 'home' | 'admin' | 'host' | 'apply') => void;
  currentPage: string;
}

export default function Header({ user, onLoginClick, onLogout, onNavigate, currentPage }: HeaderProps) {
  const handleLogout = async () => {
    await logout();
    onLogout();
  };

  return (
    <header style={styles.header}>
      <div style={styles.left}>
        <h1 style={styles.title} onClick={() => onNavigate('home')}>
          TourneyMate
        </h1>
        {user && (
          <nav style={styles.nav}>
            <button
              style={currentPage === 'home' ? styles.activeNavBtn : styles.navBtn}
              onClick={() => onNavigate('home')}
            >
              Home
            </button>
            {user.role === 'Admin' && (
              <button
                style={currentPage === 'admin' ? styles.activeNavBtn : styles.navBtn}
                onClick={() => onNavigate('admin')}
              >
                Admin Panel
              </button>
            )}
            {user.role === 'Host' && (
              <button
                style={currentPage === 'host' ? styles.activeNavBtn : styles.navBtn}
                onClick={() => onNavigate('host')}
              >
                Host Panel
              </button>
            )}
            {user.role === 'Viewer' && (
              <button
                style={currentPage === 'apply' ? styles.activeNavBtn : styles.navBtn}
                onClick={() => onNavigate('apply')}
              >
                Prijavi Tim
              </button>
            )}
          </nav>
        )}
      </div>
      <div style={styles.right}>
        {user ? (
          <>
            <span style={styles.welcome}>Dobrodošli, {user.displayName}</span>
            <button style={styles.logoutBtn} onClick={handleLogout}>
              Logout
            </button>
          </>
        ) : (
          <button style={styles.loginBtn} onClick={onLoginClick}>
            Prijavi se
          </button>
        )}
      </div>
    </header>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '1rem 2rem',
    backgroundColor: '#2c3e50',
    color: 'white',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
  },
  left: {
    display: 'flex',
    alignItems: 'center',
    gap: '2rem',
  },
  title: {
    margin: 0,
    cursor: 'pointer',
    fontSize: '1.5rem',
  },
  nav: {
    display: 'flex',
    gap: '1rem',
  },
  navBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: 'transparent',
    color: 'white',
    border: '1px solid transparent',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  activeNavBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#34495e',
    color: 'white',
    border: '1px solid white',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  right: {
    display: 'flex',
    alignItems: 'center',
    gap: '1rem',
  },
  welcome: {
    fontSize: '1rem',
  },
  loginBtn: {
    padding: '0.5rem 1.5rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
  logoutBtn: {
    padding: '0.5rem 1.5rem',
    backgroundColor: '#e74c3c',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontSize: '1rem',
  },
};
