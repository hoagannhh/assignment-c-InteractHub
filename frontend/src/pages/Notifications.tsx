import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { notificationsApi } from '../services/api';
import type { Notification } from '../types';
import { formatDistanceToNow } from 'date-fns';
import toast from 'react-hot-toast';

export default function Notifications() {
  const navigate = useNavigate();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchNotifs = async () => {
    setLoading(true);
    try {
      const { data } = await notificationsApi.getAll(1);
      setNotifications(data.items || []);
      await notificationsApi.markAllRead();
    } catch {
      toast.error('Failed to load notifications');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchNotifs();
  }, []);

  const handleClick = (n: Notification) => {
    if (n.postId) navigate(`/post/${n.postId}`);
    else if (n.actor) navigate(`/profile/${n.actor.id}`);
  };

  if (loading) return <div className="main-content" style={{ display: 'flex', justifyContent: 'center', padding: 40 }}><div className="spinner"></div></div>;

  return (
    <div className="main-content">
      <div className="card">
        <h2 style={{ fontSize: 20, fontWeight: 800, marginBottom: 16 }}>Notifications</h2>
        
        {notifications.length > 0 ? (
          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            {notifications.map(n => (
              <div 
                key={n.id} 
                className={`notif-item ${!n.isRead ? 'unread' : ''}`}
                onClick={() => handleClick(n)}
                style={{ cursor: n.postId || n.actor ? 'pointer' : 'default' }}
              >
                <div className="avatar avatar-md">
                   {n.actor?.avatarUrl ? <img src={n.actor.avatarUrl} alt="" /> : n.actor?.username?.[0]?.toUpperCase() || 'IH'}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: 15 }}>
                    <span style={{ fontWeight: 600 }}>{n.actor?.fullName || n.actor?.username || 'Someone'}</span> {n.message}
                  </div>
                  <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginTop: 4 }}>
                    {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true })}
                  </div>
                </div>
                {!n.isRead && <div className="notif-dot" />}
              </div>
            ))}
          </div>
        ) : (
          <div style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>
            No notifications yet.
          </div>
        )}
      </div>
    </div>
  );
}
