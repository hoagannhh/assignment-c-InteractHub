import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const HomeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>
  </svg>
);
const SearchIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
  </svg>
);
const BellIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/><path d="M13.73 21a2 2 0 0 1-3.46 0"/>
  </svg>
);
const UserIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/>
  </svg>
);
const LogoutIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/>
  </svg>
);
const BookmarkIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"/>
  </svg>
);

interface SidebarProps {
  onCreatePost: () => void;
  unreadCount?: number;
}

export default function Sidebar({ onCreatePost, unreadCount = 0 }: SidebarProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();
  const [collapsed] = useState(false);
  const [showLogoutConfirm, setShowLogoutConfirm] = useState(false);

  const navItems = [
    { label: 'Home', icon: <HomeIcon />, path: '/' },
    { label: 'Search', icon: <SearchIcon />, path: '/search' },
    { label: 'Notifications', icon: <BellIcon />, path: '/notifications', badge: unreadCount },
    { label: 'Profile', icon: <UserIcon />, path: `/profile/${user?.id}` },
    { label: 'Bookmarks', icon: <BookmarkIcon />, path: '/bookmarks' },
  ];

  const handleLogout = () => {
    logout();
    setShowLogoutConfirm(false);
    navigate('/login', { replace: true });
  };

  return (
    <>
      <aside className="sidebar-left">
        {/* Logo */}
        <div className="logo" onClick={() => navigate('/')} style={{ cursor: 'pointer' }}>
          <div style={{
            width: 36, height: 36, borderRadius: 12,
            background: 'var(--gradient)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 18, fontWeight: 800, color: 'white'
          }}>IH</div>
          <span className="logo-text" style={{ display: collapsed ? 'none' : undefined }}>InteractHub</span>
        </div>

        {/* Nav Items */}
        {navItems.map(item => (
          <button
            key={item.path}
            className={`nav-item ${location.pathname === item.path ? 'active' : ''}`}
            onClick={() => navigate(item.path)}
            style={{ position: 'relative' }}
          >
            {item.icon}
            <span style={{ display: collapsed ? 'none' : undefined }}>{item.label}</span>
            {item.badge ? (
              <span className="badge badge-unread" style={{ marginLeft: 'auto' }}>
                {item.badge > 9 ? '9+' : item.badge}
              </span>
            ) : null}
          </button>
        ))}

        {/* Create Post */}
        <button className="btn btn-primary" style={{ marginTop: 8, width: '100%' }} onClick={onCreatePost}>
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" style={{ width: 18, height: 18 }}>
            <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          <span style={{ display: collapsed ? 'none' : undefined }}>New Post</span>
        </button>

        {/* User Profile */}
        <div style={{ marginTop: 'auto', paddingTop: 16, borderTop: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 10, padding: '16px 8px 0' }}>
          <div className="avatar avatar-sm" onClick={() => navigate(`/profile/${user?.id}`)} style={{ cursor: 'pointer' }}>
            {user?.avatarUrl ? <img src={user.avatarUrl} alt={user.username} /> : user?.username?.[0]?.toUpperCase()}
          </div>
          {!collapsed && (
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontWeight: 600, fontSize: 13, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user?.fullName || user?.username}
              </div>
              <div style={{ fontSize: 12, color: 'var(--text-secondary)' }}>@{user?.username}</div>
            </div>
          )}
          <button
            onClick={() => setShowLogoutConfirm(true)}
            title="Đăng xuất"
            id="logout-btn"
            style={{
              padding: '8px',
              border: 'none',
              borderRadius: 10,
              cursor: 'pointer',
              background: 'rgba(239,68,68,0.12)',
              color: '#ef4444',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              transition: 'all 0.2s ease',
            }}
            onMouseEnter={e => {
              (e.currentTarget as HTMLButtonElement).style.background = 'rgba(239,68,68,0.25)';
              (e.currentTarget as HTMLButtonElement).style.transform = 'scale(1.08)';
            }}
            onMouseLeave={e => {
              (e.currentTarget as HTMLButtonElement).style.background = 'rgba(239,68,68,0.12)';
              (e.currentTarget as HTMLButtonElement).style.transform = 'scale(1)';
            }}
          >
            <LogoutIcon />
          </button>
        </div>
      </aside>

      {/* Logout Confirmation Dialog */}
      {showLogoutConfirm && (
        <div
          style={{
            position: 'fixed', inset: 0, zIndex: 9999,
            background: 'rgba(0,0,0,0.6)',
            backdropFilter: 'blur(6px)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}
          onClick={() => setShowLogoutConfirm(false)}
        >
          <div
            style={{
              background: 'var(--bg-card)',
              border: '1px solid var(--border)',
              borderRadius: 20,
              padding: '32px 28px',
              width: 360,
              boxShadow: '0 24px 60px rgba(0,0,0,0.4)',
              textAlign: 'center',
              animation: 'slideUp 0.2s ease',
            }}
            onClick={e => e.stopPropagation()}
          >
            {/* Icon */}
            <div style={{
              width: 56, height: 56, borderRadius: '50%',
              background: 'rgba(239,68,68,0.12)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              margin: '0 auto 16px',
              color: '#ef4444',
            }}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 28, height: 28 }}>
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
                <polyline points="16 17 21 12 16 7"/>
                <line x1="21" y1="12" x2="9" y2="12"/>
              </svg>
            </div>

            <h3 style={{ margin: '0 0 8px', fontSize: 18, fontWeight: 700, color: 'var(--text-primary)' }}>
              Đăng xuất?
            </h3>
            <p style={{ margin: '0 0 24px', fontSize: 14, color: 'var(--text-secondary)', lineHeight: 1.5 }}>
              Bạn có chắc muốn đăng xuất khỏi <strong>InteractHub</strong> không?
            </p>

            <div style={{ display: 'flex', gap: 10 }}>
              <button
                className="btn btn-ghost"
                style={{ flex: 1, justifyContent: 'center' }}
                onClick={() => setShowLogoutConfirm(false)}
                id="cancel-logout-btn"
              >
                Hủy
              </button>
              <button
                className="btn"
                style={{
                  flex: 1, justifyContent: 'center',
                  background: '#ef4444',
                  color: 'white',
                  border: 'none',
                }}
                onClick={handleLogout}
                id="confirm-logout-btn"
              >
                Đăng xuất
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
