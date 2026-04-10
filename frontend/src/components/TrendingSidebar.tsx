import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { postsApi, usersApi } from '../services/api';
import type { TrendingHashtag, UserSummary } from '../types';
import toast from 'react-hot-toast';
import { useAuthStore } from '../store/authStore';

export default function TrendingSidebar() {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const [trending, setTrending] = useState<TrendingHashtag[]>([]);
  const [suggestions, setSuggestions] = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [trendRes, suggRes] = await Promise.all([
          postsApi.getTrending(),
          usersApi.getSuggestions(1)
        ]);
        setTrending(trendRes.data);
        setSuggestions(suggRes.data.items || []);
      } catch (error) {
        console.error('Failed to load sidebar data', error);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [user]);

  const handleFollow = async (userId: number) => {
    try {
      await usersApi.follow(userId);
      toast.success('Followed successfully');
      setSuggestions(s => s.filter(u => u.id !== userId));
    } catch {
      toast.error('Failed to follow user');
    }
  };

  if (loading) return <aside className="sidebar-right"><div className="spinner"></div></aside>;

  return (
    <aside className="sidebar-right">
      {/* Search Input for Mobile/Tablet layout if needed, though usually in left nav. Here we just add a small search box */}
      <div style={{ marginBottom: 24 }}>
        <input 
          className="input-field" 
          placeholder="Search items, collections, and accounts" 
          onKeyDown={(e) => {
            if (e.key === 'Enter' && e.currentTarget.value) {
              navigate(`/search?q=${encodeURIComponent(e.currentTarget.value)}`);
            }
          }}
          style={{ borderRadius: 24, background: 'var(--bg-card)' }}
        />
      </div>

      <div className="card" style={{ marginBottom: 24 }}>
        <h3 style={{ fontSize: 16, fontWeight: 700, marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ fontSize: 20 }}>🔥</span> Trending
        </h3>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
          {trending.length > 0 ? trending.map(tag => (
            <div key={tag.name} className="trending-tag" onClick={() => navigate(`/hashtag/${tag.name}`)}>
              <div>
                <div className="trending-name">#{tag.name}</div>
                <div className="trending-count">{tag.postCount} posts</div>
              </div>
            </div>
          )) : <div style={{ color: 'var(--text-muted)', fontSize: 14 }}>No trending topics yet.</div>}
        </div>
      </div>

      <div className="card">
        <h3 style={{ fontSize: 16, fontWeight: 700, marginBottom: 16, display: 'flex', alignItems: 'center', gap: 8 }}>
          <span style={{ fontSize: 20 }}>👥</span> Suggested for you
        </h3>
        <div style={{ display: 'flex', flexDirection: 'column' }}>
          {suggestions.length > 0 ? suggestions.slice(0, 5).map(u => (
            <div key={u.id} className="user-card" style={{ padding: '8px 0' }}>
              <div 
                className="avatar avatar-sm" 
                style={{ cursor: 'pointer' }}
                onClick={() => navigate(`/profile/${u.id}`)}
              >
                {u.avatarUrl ? <img src={u.avatarUrl} alt="" /> : u.username[0].toUpperCase()}
              </div>
              <div className="user-info" style={{ cursor: 'pointer' }} onClick={() => navigate(`/profile/${u.id}`)}>
                <div className="user-name">{u.fullName || u.username}</div>
                <div className="user-handle">@{u.username}</div>
              </div>
              <button 
                className="btn btn-secondary btn-sm" 
                onClick={() => handleFollow(u.id)}
                style={{ padding: '4px 12px', fontSize: 12 }}
              >
                Follow
              </button>
            </div>
          )) : <div style={{ color: 'var(--text-muted)', fontSize: 14 }}>No suggestions available.</div>}
        </div>
      </div>
    </aside>
  );
}
