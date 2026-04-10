import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { postsApi } from '../services/api';
import { useAuthStore } from '../store/authStore';
import type { Post, Comment } from '../types';
import toast from 'react-hot-toast';

const HeartIcon = ({ filled }: { filled?: boolean }) => (
  <svg viewBox="0 0 24 24" fill={filled ? 'currentColor' : 'none'} stroke="currentColor" strokeWidth="2">
    <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
  </svg>
);
const CommentIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
  </svg>
);
const ShareIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="18" cy="5" r="3"/><circle cx="6" cy="12" r="3"/><circle cx="18" cy="19" r="3"/>
    <line x1="8.59" y1="13.51" x2="15.42" y2="17.49"/><line x1="15.41" y1="6.51" x2="8.59" y2="10.49"/>
  </svg>
);
const MoreIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>
  </svg>
);
const TrashIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
  </svg>
);

interface PostCardProps {
  post: Post;
  onDeleted?: (id: number) => void;
  onNavigate?: (path: string) => void;
}

export default function PostCard({ post: initialPost, onDeleted, onNavigate }: PostCardProps) {
  const { user } = useAuthStore();
  const [post, setPost] = useState(initialPost);
  const [showComments, setShowComments] = useState(false);
  const [comments, setComments] = useState<Comment[]>([]);
  const [commentText, setCommentText] = useState('');
  const [showMenu, setShowMenu] = useState(false);
  const [loadingComments, setLoadingComments] = useState(false);

  const isOwner = user?.id === post.author.id;

  const handleLike = async () => {
    try {
      if (post.isLiked) {
        await postsApi.unlikePost(post.id);
        setPost(p => ({ ...p, isLiked: false, likesCount: p.likesCount - 1 }));
      } else {
        await postsApi.likePost(post.id);
        setPost(p => ({ ...p, isLiked: true, likesCount: p.likesCount + 1 }));
      }
    } catch { toast.error('Action failed'); }
  };

  const handleComment = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!commentText.trim()) return;
    try {
      const res = await postsApi.addComment(post.id, { content: commentText });
      setComments(c => [...c, res.data]);
      setPost(p => ({ ...p, commentsCount: p.commentsCount + 1 }));
      setCommentText('');
    } catch { toast.error('Failed to comment'); }
  };

  const loadComments = async () => {
    if (loadingComments) return;
    setLoadingComments(true);
    try {
      const res = await postsApi.getComments(post.id);
      setComments(res.data);
    } catch { toast.error('Failed to load comments'); }
    finally { setLoadingComments(false); }
  };

  const toggleComments = () => {
    if (!showComments && comments.length === 0) loadComments();
    setShowComments(v => !v);
  };

  const handleDelete = async () => {
    if (!confirm('Delete this post?')) return;
    try {
      await postsApi.deletePost(post.id);
      toast.success('Post deleted');
      onDeleted?.(post.id);
    } catch { toast.error('Delete failed'); }
  };

  const handleShare = async () => {
    try {
      await postsApi.sharePost(post.id);
      toast.success('Post shared to your profile!');
    } catch { toast.error('Share failed'); }
  };

  const renderContent = (content?: string) => {
    if (!content) return null;
    const parts = content.split(/(#\w+)/g);
    return parts.map((part, i) =>
      part.match(/^#\w+/) ? (
        <span key={i} className="hashtag" onClick={() => onNavigate?.(`/hashtag/${part.slice(1)}`)}>
          {part}
        </span>
      ) : part
    );
  };

  return (
    <div className="card post-card fade-in" style={{ position: 'relative' }}>
      {/* Header */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
        <div
          className="avatar avatar-md"
          style={{ cursor: 'pointer' }}
          onClick={() => onNavigate?.(`/profile/${post.author.id}`)}
        >
          {post.author.avatarUrl
            ? <img src={post.author.avatarUrl} alt={post.author.username} />
            : post.author.username[0].toUpperCase()}
        </div>
        <div style={{ flex: 1 }}>
          <div
            style={{ fontWeight: 600, fontSize: 14, cursor: 'pointer' }}
            onClick={() => onNavigate?.(`/profile/${post.author.id}`)}
          >
            {post.author.fullName || post.author.username}
          </div>
          <div style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
            @{post.author.username} · {formatDistanceToNow(new Date(post.createdAt), { addSuffix: true })}
          </div>
        </div>
        {isOwner && (
          <div style={{ position: 'relative' }}>
            <button className="btn btn-ghost" style={{ padding: '6px', borderRadius: '50%' }} onClick={() => setShowMenu(v => !v)}>
              <MoreIcon />
            </button>
            {showMenu && (
              <div style={{ position: 'absolute', right: 0, top: '100%', background: 'var(--bg-card)', border: '1px solid var(--border)', borderRadius: 'var(--radius-sm)', minWidth: 120, zIndex: 10, boxShadow: 'var(--shadow)' }}>
                <button
                  onClick={() => { handleDelete(); setShowMenu(false); }}
                  style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '10px 14px', width: '100%', background: 'none', border: 'none', color: 'var(--danger)', cursor: 'pointer', fontSize: 14 }}
                >
                  <TrashIcon /> Delete
                </button>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Shared post badge */}
      {post.sharedPost && (
        <div style={{ fontSize: 12, color: 'var(--text-muted)', marginBottom: 8, display: 'flex', alignItems: 'center', gap: 6 }}>
          <ShareIcon /> Shared a post
        </div>
      )}

      {/* Content */}
      {post.content && (
        <p style={{ fontSize: 15, lineHeight: 1.65, marginBottom: 8, whiteSpace: 'pre-wrap' }}>
          {renderContent(post.content)}
        </p>
      )}

      {/* Hashtags */}
      {post.hashtags?.length > 0 && (
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginBottom: 8 }}>
          {post.hashtags.map(tag => (
            <span key={tag} className="badge badge-accent" style={{ cursor: 'pointer' }} onClick={() => onNavigate?.(`/hashtag/${tag}`)}>
              #{tag}
            </span>
          ))}
        </div>
      )}

      {/* Image */}
      {post.imageUrl && (
        <img src={post.imageUrl} alt="Post" className="post-image" />
      )}

      {/* Shared post preview */}
      {post.sharedPost && (
        <div style={{ border: '1px solid var(--border)', borderRadius: 'var(--radius-sm)', padding: 12, marginTop: 8, background: 'var(--bg-secondary)' }}>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 4 }}>
            @{post.sharedPost.author.username}
          </div>
          <div style={{ fontSize: 14, color: 'var(--text-secondary)' }}>{post.sharedPost.content}</div>
          {post.sharedPost.imageUrl && <img src={post.sharedPost.imageUrl} alt="Shared" className="post-image" style={{ marginTop: 8 }} />}
        </div>
      )}

      {/* Actions */}
      <div className="post-actions">
        <button className={`action-btn ${post.isLiked ? 'liked' : ''}`} onClick={handleLike}>
          <HeartIcon filled={post.isLiked} />
          {post.likesCount > 0 && <span>{post.likesCount}</span>}
        </button>
        <button className="action-btn" onClick={toggleComments}>
          <CommentIcon />
          {post.commentsCount > 0 && <span>{post.commentsCount}</span>}
        </button>
        <button className="action-btn" onClick={handleShare}>
          <ShareIcon />
        </button>
      </div>

      {/* Comments section */}
      {showComments && (
        <div style={{ marginTop: 12, paddingTop: 12, borderTop: '1px solid var(--border)' }}>
          {loadingComments ? (
            <div style={{ display: 'flex', justifyContent: 'center', padding: 16 }}>
              <div className="spinner" style={{ width: 24, height: 24 }} />
            </div>
          ) : (
            comments.map(c => (
              <div key={c.id} className="comment-item">
                <div className="avatar avatar-xs">
                  {c.author.avatarUrl ? <img src={c.author.avatarUrl} alt="" /> : c.author.username[0].toUpperCase()}
                </div>
                <div className="comment-bubble">
                  <div className="comment-author">{c.author.fullName || c.author.username}</div>
                  <div className="comment-text">{c.content}</div>
                  <div className="comment-time">{formatDistanceToNow(new Date(c.createdAt), { addSuffix: true })}</div>
                </div>
              </div>
            ))
          )}

          {/* Add comment */}
          <form onSubmit={handleComment} style={{ display: 'flex', gap: 8, marginTop: 8 }}>
            <div className="avatar avatar-xs">
              {user?.avatarUrl ? <img src={user.avatarUrl} alt="" /> : user?.username?.[0]?.toUpperCase()}
            </div>
            <input
              className="input-field"
              placeholder="Write a comment..."
              value={commentText}
              onChange={e => setCommentText(e.target.value)}
              style={{ padding: '8px 14px', borderRadius: 20, fontSize: 13 }}
            />
            <button type="submit" className="btn btn-primary btn-sm" disabled={!commentText.trim()}>Post</button>
          </form>
        </div>
      )}
    </div>
  );
}
