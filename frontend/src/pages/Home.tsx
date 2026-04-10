import { useState, useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { postsApi, storiesApi } from '../services/api';
import type { Post, Story } from '../types';
import PostCard from '../components/PostCard';
import CreatePostModal from '../components/CreatePostModal';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';

export default function Home() {
  const { user } = useAuthStore();
  const navigate = useNavigate();
  const [posts, setPosts] = useState<Post[]>([]);
  const [stories, setStories] = useState<Story[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);

  const fetchFeed = async (pageNum: number, overwrite = false) => {
    try {
      const res = await postsApi.getFeed(pageNum);
      setPosts(prev => overwrite ? res.data.items : [...prev, ...res.data.items]);
      setHasMore(res.data.hasNext);
    } catch {
      toast.error('Failed to load feed');
    }
  };

  const fetchStories = async () => {
    try {
      const res = await storiesApi.getActive();
      setStories(res.data);
    } catch {
      // Ignore
    }
  };

  useEffect(() => {
    setLoading(true);
    Promise.all([fetchFeed(1, true), fetchStories()]).finally(() => setLoading(false));
  }, []);

  const handleCreated = () => {
    setPage(1);
    fetchFeed(1, true);
  };

  const handlePostDeleted = (id: number) => {
    setPosts(prev => prev.filter(p => p.id !== id));
  };

  const loadMore = () => {
    if (!hasMore || loading) return;
    const nextPage = page + 1;
    setPage(nextPage);
    fetchFeed(nextPage);
  };

  return (
    <div className="main-content">
      {/* Stories Bar */}
      <div className="stories-bar fade-in">
        <div className="story-item" onClick={() => navigate('/story/create')}>
          <div className="story-ring" style={{ background: 'var(--bg-secondary)', border: '2px dashed var(--border)', padding: 0 }}>
             <div style={{ fontSize: 24, color: 'var(--text-muted)' }}>+</div>
          </div>
          <div className="story-name">Add Story</div>
        </div>

        {stories.map(story => (
          <div key={story.id} className="story-item" onClick={() => {
            storiesApi.view(story.id);
            // In a real app we'd open a story viewer. Here just mark viewed.
            toast.success(`Viewing story by ${story.author.username}`);
          }}>
            <div className={`story-ring ${story.isViewed ? 'viewed' : ''}`}>
               <img src={story.author.avatarUrl || `https://ui-avatars.com/api/?name=${story.author.username}`} alt="" />
            </div>
            <div className="story-name">{story.author.username}</div>
          </div>
        ))}
      </div>

      {/* Create Post Input */}
      <div className="create-post-box fade-in" onClick={() => setShowCreateModal(true)}>
        <div className="avatar avatar-sm">
          {user?.avatarUrl ? <img src={user.avatarUrl} alt="" /> : user?.username?.[0]?.toUpperCase()}
        </div>
        <div className="create-post-input">
          What's on your mind, {user?.fullName || user?.username}?
        </div>
      </div>

      {/* Feed */}
      {loading && page === 1 ? (
        <div style={{ display: 'flex', justifyContent: 'center', padding: 40 }}><div className="spinner"></div></div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {posts.length > 0 ? (
            posts.map(post => (
              <PostCard 
                key={post.id} 
                post={post} 
                onDeleted={handlePostDeleted} 
                onNavigate={navigate} 
              />
            ))
          ) : (
            <div className="card" style={{ textAlign: 'center', padding: 40, color: 'var(--text-muted)' }}>
              <h3>Welcome to InteractHub!</h3>
              <p style={{ marginTop: 8 }}>Follow some people to see their posts here.</p>
            </div>
          )}

          {hasMore && posts.length > 0 && (
            <button className="btn btn-secondary" onClick={loadMore} style={{ alignSelf: 'center', marginTop: 16 }}>
              Load More
            </button>
          )}
        </div>
      )}

      {showCreateModal && <CreatePostModal onClose={() => setShowCreateModal(false)} onCreated={handleCreated} />}
    </div>
  );
}
