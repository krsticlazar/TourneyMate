import { useState } from 'react';
import { getUser } from './api/client';
import Header from './components/Header';
import AuthModal from './components/AuthModal';
import HomePage from './pages/HomePage';
import AdminPage from './pages/AdminPage';
import HostPanel from './pages/HostPanel';
import ApplyTeamPage from './pages/ApplyTeamPage';
import TournamentManagement from './pages/TournamentManagement';
import TournamentPage from './pages/TournamentPage';
import CreateTournamentPage from './pages/CreateTournamentPage';
import type { User } from './types';

type Page = 'home' | 'admin' | 'host' | 'apply' | 'tournament-management' | 'tournament-view' | 'create-tournament';

function App() {
  const [user, setUser] = useState<User | null>(() => getUser());
  const [showAuthModal, setShowAuthModal] = useState(false);
  const [currentPage, setCurrentPage] = useState<Page>('home');
  const [managingTournamentId, setManagingTournamentId] = useState<string | null>(null);
  const [viewingTournamentId, setViewingTournamentId] = useState<string | null>(null);

  

  const handleLoginSuccess = (loggedInUser: User) => {
    setUser(loggedInUser);
    setCurrentPage('home');
  };

  const handleLogout = () => {
    setUser(null);
    setCurrentPage('home');
    setManagingTournamentId(null);
    setViewingTournamentId(null);
  };

  const handleNavigate = (page: Page) => {
    setCurrentPage(page);
    if (page !== 'tournament-management') {
      setManagingTournamentId(null);
    }
    if (page !== 'tournament-view') {
      setViewingTournamentId(null);
    }
  };

  const handleManageTournament = (tournamentId: string) => {
    setManagingTournamentId(tournamentId);
    setCurrentPage('tournament-management');
  };

  const handleViewTournament = (tournamentId: string) => {
    setViewingTournamentId(tournamentId);
    setCurrentPage('tournament-view');
  };

  const handleBackToHostPanel = () => {
    setCurrentPage('host');
    setManagingTournamentId(null);
  };

  const handleBackToHome = () => {
    setCurrentPage('home');
    setViewingTournamentId(null);
  };

  const handleCreateTournament = () => {
    setCurrentPage('create-tournament');
  };

  const handleCreateTournamentSuccess = () => {
    setCurrentPage('host'); // Back to Host Panel after creating
  };

  return (
    <div style={styles.app}>
      <Header
        user={user}
        onLoginClick={() => setShowAuthModal(true)}
        onLogout={handleLogout}
        onNavigate={handleNavigate}
        currentPage={currentPage}
      />

      <main style={styles.main}>
        {currentPage === 'home' && (
          <HomePage user={user} onTournamentClick={handleViewTournament} />
        )}
        {currentPage === 'admin' && user?.role === 'Admin' && <AdminPage />}
        {currentPage === 'host' && user?.role === 'Host' && (
          <HostPanel
            user={user}
            onManageTournament={handleManageTournament}
            onCreateTournament={handleCreateTournament}
          />
        )}
        {currentPage === 'apply' && user?.role === 'Viewer' && <ApplyTeamPage />}
        {currentPage === 'tournament-management' && user && managingTournamentId && (
          <TournamentManagement
            tournamentId={managingTournamentId}
            user={user}
            onBack={handleBackToHostPanel}
          />
        )}
        {currentPage === 'tournament-view' && viewingTournamentId && (
          <TournamentPage
            tournamentId={viewingTournamentId}
            user={user}
            onBack={handleBackToHome}
          />
        )}
        {currentPage === 'create-tournament' && user?.role === 'Host' && (
          <CreateTournamentPage
            onBack={handleBackToHostPanel}
            onSuccess={handleCreateTournamentSuccess}
          />
        )}
      </main>

      {showAuthModal && (
        <AuthModal
          onClose={() => setShowAuthModal(false)}
          onLoginSuccess={handleLoginSuccess}
        />
      )}
    </div>
  );
}

const styles: { [key: string]: React.CSSProperties } = {
  app: {
    minHeight: '100vh',
    backgroundColor: '#ecf0f1',
  },
  main: {
    minHeight: 'calc(100vh - 80px)',
  },
};

export default App;
