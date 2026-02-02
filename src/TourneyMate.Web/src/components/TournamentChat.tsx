import { useState, useEffect, useRef } from 'react';
import { getTournamentChat, sendTournamentMessage } from '../api/client';
import type { ChatMessage, User } from '../types';

interface TournamentChatProps {
  tournamentId: string;
  user: User | null;
}

export default function TournamentChat({ tournamentId, user }: TournamentChatProps) {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const messagesContainerRef = useRef<HTMLDivElement>(null);

  // Auto-scroll the messages container to bottom when messages change
  useEffect(() => {
    const el = messagesContainerRef.current;
    if (el) el.scrollTop = el.scrollHeight;
  }, [messages]);

  useEffect(() => {
    loadMessages();
    const interval = setInterval(loadMessages, 1000); // ⚡ INSTANT REFRESH - 1 sec
    return () => clearInterval(interval);
  }, [tournamentId]);

  const loadMessages = async () => {
    try {
      const data = await getTournamentChat(tournamentId, 50);
      setMessages(data);
    } catch (error) {
      console.error('Failed to load tournament chat:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newMessage.trim() || !user) return;

    try {
      await sendTournamentMessage(tournamentId, newMessage);
      setNewMessage('');
      await loadMessages(); // Refresh immediately
    } catch (error: any) {
      console.error('Failed to send message:', error);
      alert(`Greška: ${error.message}`);
    }
  };

  if (loading) return <div style={styles.container}>Učitavanje chata...</div>;

  return (
    <div style={styles.container}>
      <h3>Turnir Chat</h3>
      <div style={styles.messagesContainer} ref={messagesContainerRef}>
        {messages.length === 0 ? (
          <p style={styles.noMessages}>Nema poruka</p>
        ) : (
          messages.map((msg, idx) => (
            <div key={idx} style={styles.message}>
              <strong style={styles.displayName}>{msg.displayName}:</strong>
              <span style={styles.text}>{msg.text}</span>
              <span style={styles.time}>{new Date(msg.timestampUtc).toLocaleTimeString()}</span>
            </div>
          ))
        )}
        {/* spacer for accessibility if needed */}
      </div>
      {user ? (
        <form onSubmit={handleSendMessage} style={styles.form}>
          <input
            type="text"
            value={newMessage}
            onChange={(e) => setNewMessage(e.target.value)}
            placeholder="Unesite poruku..."
            style={styles.input}
          />
          <button type="submit" style={styles.sendBtn}>
            Pošalji
          </button>
        </form>
      ) : (
        <p style={styles.loginPrompt}>Prijavite se da biste pisali poruke</p>
      )}
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  container: {
    border: '1px solid #ddd',
    borderRadius: '8px',
    padding: '1rem',
    backgroundColor: '#f9f9f9',
  },
  messagesContainer: {
    maxHeight: '300px',
    overflowY: 'auto',
    marginBottom: '1rem',
    padding: '0.5rem',
    backgroundColor: 'white',
    borderRadius: '4px',
  },
  message: {
    padding: '0.5rem',
    borderBottom: '1px solid #eee',
    display: 'flex',
    flexDirection: 'column',
    gap: '0.25rem',
  },
  displayName: {
    color: '#2c3e50',
    fontSize: '0.9rem',
  },
  text: {
    fontSize: '1rem',
  },
  time: {
    fontSize: '0.75rem',
    color: '#999',
  },
  form: {
    display: 'flex',
    gap: '0.5rem',
  },
  input: {
    flex: 1,
    padding: '0.5rem',
    fontSize: '1rem',
    border: '1px solid #ccc',
    borderRadius: '4px',
  },
  sendBtn: {
    padding: '0.5rem 1rem',
    backgroundColor: '#3498db',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  },
  noMessages: {
    textAlign: 'center',
    color: '#999',
  },
  loginPrompt: {
    textAlign: 'center',
    color: '#666',
    fontStyle: 'italic',
    fontSize: '0.9rem',
  },
};
