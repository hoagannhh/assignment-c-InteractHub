import { useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { postsApi } from '../services/api';
import toast from 'react-hot-toast';

interface CreatePostModalProps {
  onClose: () => void;
  onCreated: () => void;
}

const ImageIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 20, height: 20 }}>
    <rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/>
    <polyline points="21 15 16 10 5 21"/>
  </svg>
);
const GlobeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 16, height: 16 }}>
    <circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/>
    <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/>
  </svg>
);

export default function CreatePostModal({ onClose, onCreated }: CreatePostModalProps) {
  const { user } = useAuthStore();
  const [content, setContent] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [visibility, setVisibility] = useState('Public');
  const [loading, setLoading] = useState(false);
  const [showImageInput, setShowImageInput] = useState(false);

  const extractHashtags = (text: string) =>
    (text.match(/#\w+/g) || []).map(t => t.slice(1));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim() && !imageUrl.trim()) {
      toast.error('Please add some content or an image URL');
      return;
    }
    setLoading(true);
    try {
      await postsApi.createPost({
        content: content || null,
        imageUrl: imageUrl || null,
        visibility,
        hashtags: extractHashtags(content),
      });
      toast.success('Post created!');
      onCreated();
      onClose();
    } catch {
      toast.error('Failed to create post');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal">
        <div className="modal-header">
          <span className="modal-title">Create Post</span>
          <button className="btn btn-ghost" onClick={onClose} style={{ padding: '4px 8px', fontSize: 20 }}>×</button>
        </div>
        <div className="modal-body">
          <div style={{ display: 'flex', gap: 12, alignItems: 'flex-start', marginBottom: 16 }}>
            <div className="avatar avatar-md">
              {user?.avatarUrl ? <img src={user.avatarUrl} alt="" /> : user?.username?.[0]?.toUpperCase()}
            </div>
            <div style={{ flex: 1 }}>
              <div style={{ fontWeight: 600, fontSize: 14, marginBottom: 4 }}>
                {user?.fullName || user?.username}
              </div>
              <select
                value={visibility}
                onChange={e => setVisibility(e.target.value)}
                style={{
                  background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: 8, padding: '3px 8px', fontSize: 12, color: 'var(--text-secondary)',
                  cursor: 'pointer'
                }}
              >
                <option value="Public">🌍 Public</option>
                <option value="Friends">👥 Friends</option>
                <option value="Private">🔒 Private</option>
              </select>
            </div>
          </div>

          <form onSubmit={handleSubmit}>
            <textarea
              className="input-field"
              placeholder="What's on your mind?"
              value={content}
              onChange={e => setContent(e.target.value)}
              rows={4}
              style={{
                resize: 'none', marginBottom: 12, fontSize: 16,
                background: 'transparent', border: 'none', borderBottom: '1px solid var(--border)',
                borderRadius: 0, padding: '0 0 12px', outline: 'none',
                boxShadow: 'none'
              }}
              autoFocus
            />

            {showImageInput && (
              <div style={{ marginBottom: 12 }}>
                <input
                  className="input-field"
                  placeholder="Paste image URL here..."
                  value={imageUrl}
                  onChange={e => setImageUrl(e.target.value)}
                  style={{ marginBottom: imageUrl ? 8 : 0 }}
                />
                {imageUrl && (
                  <img
                    src={imageUrl}
                    alt="Preview"
                    style={{ width: '100%', maxHeight: 200, objectFit: 'cover', borderRadius: 'var(--radius-sm)' }}
                    onError={e => { (e.target as HTMLImageElement).style.display = 'none'; }}
                  />
                )}
              </div>
            )}

            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div style={{ display: 'flex', gap: 4 }}>
                <button
                  type="button"
                  className="btn btn-ghost btn-sm"
                  onClick={() => setShowImageInput(v => !v)}
                  style={{ color: showImageInput ? 'var(--accent)' : undefined }}
                  title="Add image"
                >
                  <ImageIcon />
                </button>
                <div style={{ display: 'flex', alignItems: 'center', gap: 4, padding: '5px 10px', fontSize: 12, color: 'var(--text-muted)' }}>
                  <GlobeIcon /> Hashtags auto-detected from #tags
                </div>
              </div>
              <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <span style={{ fontSize: 12, color: content.length > 1800 ? 'var(--danger)' : 'var(--text-muted)' }}>
                  {content.length}/2000
                </span>
                <button
                  type="submit"
                  className="btn btn-primary"
                  disabled={loading || (!content.trim() && !imageUrl.trim()) || content.length > 2000}
                >
                  {loading ? 'Posting...' : 'Post'}
                </button>
              </div>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
