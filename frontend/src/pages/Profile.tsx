import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usersApi, postsApi } from '../services/api';
import { useAuthStore } from '../store/authStore';
import type { User, Post } from '../types';
import PostCard from '../components/PostCard';
import toast from 'react-hot-toast';

export default function Profile() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user: currentUser } = useAuthStore();
  const [profile, setProfile] = useState<User | null>(null);
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'posts' | 'grid'>('posts');

  const isOwner = currentUser?.id === profile?.id;

  useEffect(() => {
    if (!id) return;
    const fetchProfile = async () => {
      setLoading(true);
      try {
        const [profileRes, postsRes] = await Promise.all([
          usersApi.getProfile(Number(id)),
          postsApi.getUserPosts(Number(id))
        ]);
        setProfile(profileRes.data);
        setPosts(postsRes.data.items || []);
      } catch {
        toast.error('Failed to load profile');
      } finally {
        setLoading(false);
      }
    };
    fetchProfile();
  }, [id]);

  const handleFollow = async () => {
    if (!profile) return;
    try {
      if (profile.isFollowing) {
        await usersApi.unfollow(profile.id);
        setProfile({ ...profile, isFollowing: false, followersCount: profile.followersCount - 1 });
      } else {
        await usersApi.follow(profile.id);
        setProfile({ ...profile, isFollowing: true, followersCount: profile.followersCount + 1 });
      }
    } catch {
      toast.error('Action failed');
    }
  };

  if (loading) return <div className="main-content" style={{ display: 'flex', justifyContent: 'center', padding: 40 }}><div className="spinner"></div></div>;
  if (!profile) return <div className="main-content">Profile not found</div>;

  return (
    <div className="main-content">
      <div className="card fade-in" style={{ padding: 0, overflow: 'hidden' }}>
        <img src={profile.coverUrl || 'https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=2564&auto=format&fit=crop'} alt="Cover" className="profile-cover" />
        
        <div className="profile-header">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
            <div className="profile-avatar-wrap">
              <div className="avatar avatar-xl" style={{ border: '4px solid var(--bg-card)' }}>
                {profile.avatarUrl ? <img src={profile.avatarUrl} alt="" /> : profile.username[0].toUpperCase()}
              </div>
            </div>
            
            <div style={{ marginBottom: 12 }}>
              {isOwner ? (
                <button className="btn btn-secondary">Edit Profile</button>
              ) : (
                <button 
                  className={`btn ${profile.isFollowing ? 'btn-secondary' : 'btn-primary'}`}
                  onClick={handleFollow}
                >
                  {profile.isFollowing ? 'Following' : 'Follow'}
                </button>
              )}
            </div>
          </div>

          <div>
            <h1 style={{ fontSize: 24, fontWeight: 800, lineHeight: 1.2 }}>{profile.fullName || profile.username}</h1>
            <div style={{ color: 'var(--text-secondary)' }}>@{profile.username}</div>
            
            {profile.bio && <p style={{ marginTop: 12, fontSize: 15 }}>{profile.bio}</p>}

            <div className="profile-stats">
              <div className="profile-stat">
                <div className="profile-stat-num">{profile.postsCount}</div>
                <div className="profile-stat-label">Posts</div>
              </div>
              <div className="profile-stat">
                <div className="profile-stat-num">{profile.followersCount}</div>
                <div className="profile-stat-label">Followers</div>
              </div>
              <div className="profile-stat">
                <div className="profile-stat-num">{profile.followingCount}</div>
                <div className="profile-stat-label">Following</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="card" style={{ padding: 0, marginTop: 16 }}>
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
            style={{ borderRadius: 0, borderBottom: activeTab === 'grid' ? '2px solid var(--accent)' : '2px solid transparent', color: activeTab === 'grid' ? 'var(--accent)' : '' }}
            onClick={() => setActiveTab('grid')}
          >
            Media Grid
          </button>
        </div>
      </div>

      <div style={{ marginTop: 16, display: 'flex', flexDirection: 'column', gap: 16 }}>
        {activeTab === 'posts' ? (
          posts.length > 0 ? (
            posts.map(post => <PostCard key={post.id} post={post} onNavigate={navigate} />)
          ) : (
            <div className="card" style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>No posts yet.</div>
          )
        ) : (
          <div className="posts-grid">
            {posts.filter(p => p.imageUrl).map(post => (
              <div key={post.id} className="grid-post">
                <img src={post.imageUrl} alt="" />
                <div className="grid-post-overlay">
                  ❤️ {post.likesCount} 💬 {post.commentsCount}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
