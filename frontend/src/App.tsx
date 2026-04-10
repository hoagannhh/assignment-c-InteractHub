import { useEffect, useState } from 'react';
import { Routes, Route } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import Sidebar from './components/Sidebar';
import TrendingSidebar from './components/TrendingSidebar';
import CreatePostModal from './components/CreatePostModal';
import { useAuthStore } from './store/authStore';
import { ProtectedRoute, PublicRoute } from './components/ProtectedRoute';
import * as signalR from '@microsoft/signalr';
import toast from 'react-hot-toast';

// Pages
import Home from './pages/Home';
import Login from './pages/Login';
import Register from './pages/Register';
import Profile from './pages/Profile';
import Search from './pages/Search';
import Notifications from './pages/Notifications';
import { notificationsApi } from './services/api';

function AppLayout({ children }: { children: React.ReactNode }) {
  const [showCreatePost, setShowCreatePost] = useState(false);
  const { token, isAuthenticated } = useAuthStore();
  const [unreadCount, setUnreadCount] = useState(0);

  useEffect(() => {
    if (!isAuthenticated || !token) return;

    // Fetch initial unread count
    notificationsApi.getUnreadCount().then(r => setUnreadCount(r.data.count)).catch(() => {});

    // Setup SignalR
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`http://localhost:5218/hubs/notifications?access_token=${token}`)
      .withAutomaticReconnect()
      .build();

    connection.start().catch(err => console.error('SignalR Connection Error: ', err));

    connection.on('ReceiveNotification', (notif: any) => {
      toast(`${notif.message}`, { icon: '🔔' });
      setUnreadCount(prev => prev + 1);
    });

    return () => {
      connection.stop();
    };
  }, [isAuthenticated, token]);

  return (
    <div className="app-layout">
      {/* Left Sidebar */}
      <Sidebar onCreatePost={() => setShowCreatePost(true)} unreadCount={unreadCount} />
      
      {/* Main Content */}
      {children}
      
      {/* Right Sidebar */}
      <TrendingSidebar />

      {/* Mobile Bottom Nav */}
      <nav className="mobile-nav">
        {/* Simplified mobile nav, usually just icons linking to routes */}
        <Sidebar onCreatePost={() => setShowCreatePost(true)} unreadCount={unreadCount} />
      </nav>

      {showCreatePost && <CreatePostModal onClose={() => setShowCreatePost(false)} onCreated={() => {}} />}
    </div>
  );
}

export default function App() {
  return (
    <>
      <Toaster position="top-center" toastOptions={{ 
        style: { background: 'var(--bg-card)', color: 'var(--text-primary)', border: '1px solid var(--border)' } 
      }} />
      <Routes>
        {/* Public Routes */}
        <Route element={<PublicRoute />}>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
        </Route>

        {/* Protected Routes */}
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<AppLayout><Home /></AppLayout>} />
          <Route path="/profile/:id" element={<AppLayout><Profile /></AppLayout>} />
          <Route path="/search" element={<AppLayout><Search /></AppLayout>} />
          <Route path="/hashtag/:tag" element={<AppLayout><Search /></AppLayout>} /> {/* Redirects to search for now */}
          <Route path="/notifications" element={<AppLayout><Notifications /></AppLayout>} />
          <Route path="*" element={<AppLayout><div className="main-content"><h2>404 - Not Found</h2></div></AppLayout>} />
        </Route>
      </Routes>
    </>
  );
}
