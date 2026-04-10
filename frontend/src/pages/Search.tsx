import { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { usersApi, postsApi } from '../services/api';
import type { User, Post } from '../types';
import PostCard from '../components/PostCard';
import toast from 'react-hot-toast';

export default function Search() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const query = searchParams.get('q') || '';
  
  const [users, setUsers] = useState<User[]>([]);
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<'posts'|'users'>('posts');

  useEffect(() => {
    if (!query) return;
    const searchAll = async () => {
      setLoading(true);
      try {
        const [postsRes, usersRes] = await Promise.all([
          postsApi.search(query, 1),
          usersApi.search(query)
        ]);
        setPosts(postsRes.data.items || []);
        setUsers(usersRes.data.items || []);
      } catch {
        toast.error('Search failed');
      } finally {
        setLoading(false);
      }
    };
    searchAll();
  }, [query]);

  return (
    <div className="main-content">
      <div className="card" style={{ marginBottom: 16 }}>
        <input
          className="input-field"
          placeholder="Search items, collections, and accounts"
          defaultValue={query}
          onKeyDown={e => {
            if (e.key === 'Enter' && e.currentTarget.value) {
              setSearchParams({ q: e.currentTarget.value });
            }
          }}
          autoFocus
        />
      </div>

      {query && (
        <>
          <div className="card" style={{ padding: 0, marginBottom: 16 }}>
            <div style={{ display: 'flex', borderBottom: '1px solid var(--border)' }}>
              <button 
                className="nav-item" 
                style={{ borderRadius: 0, borderBottom: activeTab === 'posts' ? '2px solid var(--accent)' : '2px solid transparent', color: activeTab === 'posts' ? 'var(--accent)' : '' }}
                onClick={() => setActiveTab('posts')}
              >
                Posts
              </button>
              <button 
                className="nav-item" 
                style={{ borderRadius: 0, borderBottom: activeTab === 'users' ? '2px solid var(--accent)' : '2px solid transparent', color: activeTab === 'users' ? 'var(--accent)' : '' }}
                onClick={() => setActiveTab('users')}
              >
                People
              </button>
            </div>
          </div>

          {loading ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 40 }}><div className="spinner"></div></div>
          ) : activeTab === 'posts' ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {posts.length > 0 ? (
                posts.map(p => <PostCard key={p.id} post={p} onNavigate={navigate} />)
              ) : (
                <div className="card" style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>No posts found for "{query}"</div>
              )}
            </div>
          ) : (
            <div className="card">
              <div style={{ display: 'flex', flexDirection: 'column' }}>
                {users.length > 0 ? (
                  users.map(u => (
                    <div key={u.id} className="user-card">
                      <div className="avatar avatar-md" style={{ cursor: 'pointer' }} onClick={() => navigate(`/profile/${u.id}`)}>
                        {u.avatarUrl ? <img src={u.avatarUrl} alt="" /> : u.username[0].toUpperCase()}
                      </div>
                      <div className="user-info" style={{ cursor: 'pointer' }} onClick={() => navigate(`/profile/${u.id}`)}>
                        <div className="user-name">{u.fullName || u.username}</div>
                        <div className="user-handle">@{u.username} • {u.followersCount} followers</div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>No people found for "{query}"</div>
                )}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
