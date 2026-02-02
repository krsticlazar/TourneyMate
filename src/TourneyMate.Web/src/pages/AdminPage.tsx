import { useState, useEffect } from 'react';
import { getAllUsers, setUserRole } from '../api/client';
import type { AdminUser } from '../types';

export default function AdminPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedUser, setSelectedUser] = useState<string | null>(null);
  const [newRole, setNewRole] = useState<string>('');

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      const data = await getAllUsers();
      setUsers(data);
    } catch (error) {
      console.error('Failed to load users:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleChangeRole = async (username: string) => {
    if (!newRole) return;

    try {
      await setUserRole(username, newRole);
      alert(`Promenjena rola korisnika ${username} na ${newRole}`);
      loadUsers(); // Refresh list
      setSelectedUser(null);
      setNewRole('');
    } catch (error: any) {
      alert(`Greška: ${error.message}`);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje...</div>;

  return (
    <div style={styles.container}>
      <h1>Admin Panel</h1>
      <p style={styles.subtitle}>Upravljanje korisnicima i rolama</p>

      <table style={styles.table}>
        <thead>
          <tr style={styles.headerRow}>
            <th style={styles.th}>Username</th>
            <th style={styles.th}>Display Name</th>
            <th style={styles.th}>Role</th>
            <th style={styles.th}>Akcije</th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => (
            <tr key={user.username} style={styles.row}>
              <td style={styles.td}>{user.username}</td>
              <td style={styles.td}>{user.displayName}</td>
              <td style={styles.td}>
                <span style={getRoleStyle(user.role)}>{user.role}</span>
              </td>
              <td style={styles.td}>
                {selectedUser === user.username ? (
                  <div style={styles.roleChange}>
                    <select
                      value={newRole}
                      onChange={(e) => setNewRole(e.target.value)}
                      style={styles.select}
                    >
                      <option value="">Izaberi...</option>
                      <option value="Viewer">Viewer</option>
                      <option value="Host">Host</option>
                      <option value="Admin">Admin</option>
                    </select>
                    <button
                      onClick={() => handleChangeRole(user.username)}
                      style={styles.saveBtn}
                    >
                      Sačuvaj
                    </button>
                    <button
                      onClick={() => {
                        setSelectedUser(null);
                        setNewRole('');
                      }}
                      style={styles.cancelBtn}
                    >
                      Otkaži
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={() => {
                      setSelectedUser(user.username);
                      setNewRole(user.role);
                    }}
                    style={styles.changeBtn}
                  >
                    Promeni Rolu
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function getRoleStyle(role: string): React.CSSProperties {
  const baseStyle = {
    padding: '0.25rem 0.75rem',
    borderRadius: '12px',
    fontSize: '0.9rem',
    fontWeight: 'bold' as const,
  };

  switch (role) {
    case 'Admin':
      return { ...baseStyle, backgroundColor: '#e74c3c', color: 'white' };
    case 'Host':
      return { ...baseStyle, backgroundColor: '#f39c12', color: 'white' };
    case 'Viewer':
      return { ...baseStyle, backgroundColor: '#3498db', color: 'white' };
    default:
      return { ...baseStyle, backgroundColor: '#95a5a6', color: 'white' };
  }
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    padding: '2rem',
    maxWidth: '1000px',
    margin: '0 auto',
  },
  subtitle: {
    color: '#666',
    marginBottom: '2rem',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    backgroundColor: 'white',
    boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
  },
  headerRow: {
    backgroundColor: '#2c3e50',
    color: 'white',
  },
  th: {
    padding: '1rem',
    textAlign: 'left',
    fontWeight: 'bold',
  },
  row: {
    borderBottom: '1px solid #ddd',
  },
  td: {
    padding: '1rem',
  },
  changeBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  roleChange: {
    display: 'flex',
    gap: '0.5rem',
    alignItems: 'center',
  },
  select: {
    padding: '0.5rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  saveBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#27ae60',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  cancelBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#95a5a6',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
};
