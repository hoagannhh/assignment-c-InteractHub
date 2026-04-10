import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5218/api',
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default api;

// Auth
export const authApi = {
  register: (data: { username: string; email: string; password: string; fullName?: string }) =>
    api.post('/auth/register', data),
  login: (data: { email: string; password: string }) =>
    api.post('/auth/login', data),
};

// Users
export const usersApi = {
  getMe: () => api.get('/users/me'),
  getProfile: (userId: number) => api.get(`/users/${userId}`),
  updateMe: (data: object) => api.put('/users/me', data),
  follow: (userId: number) => api.post(`/users/${userId}/follow`),
  unfollow: (userId: number) => api.delete(`/users/${userId}/follow`),
  getSuggestions: (page = 1) => api.get(`/users/suggestions?page=${page}&pageSize=8`),
  getFollowers: (userId: number, page = 1) => api.get(`/users/${userId}/followers?page=${page}`),
  getFollowing: (userId: number, page = 1) => api.get(`/users/${userId}/following?page=${page}`),
  search: (q: string) => api.get(`/users/search?q=${encodeURIComponent(q)}`),
};

// Posts
export const postsApi = {
  getFeed: (page = 1) => api.get(`/posts/feed?page=${page}&pageSize=10`),
  getPost: (id: number) => api.get(`/posts/${id}`),
  getUserPosts: (userId: number, page = 1) => api.get(`/posts/user/${userId}?page=${page}`),
  createPost: (data: object) => api.post('/posts', data),
  updatePost: (id: number, data: object) => api.put(`/posts/${id}`, data),
  deletePost: (id: number) => api.delete(`/posts/${id}`),
  likePost: (id: number) => api.post(`/posts/${id}/like`),
  unlikePost: (id: number) => api.delete(`/posts/${id}/like`),
  sharePost: (id: number, content?: string) => api.post(`/posts/${id}/share`, JSON.stringify(content)),
  getComments: (id: number) => api.get(`/posts/${id}/comments`),
  addComment: (id: number, data: object) => api.post(`/posts/${id}/comments`, data),
  deleteComment: (commentId: number) => api.delete(`/posts/comments/${commentId}`),
  search: (q: string, page = 1) => api.get(`/posts/search?q=${encodeURIComponent(q)}&page=${page}`),
  getTrending: () => api.get('/posts/trending'),
  getHashtagPosts: (tag: string, page = 1) => api.get(`/posts/hashtag/${tag}?page=${page}`),
};

// Notifications
export const notificationsApi = {
  getAll: (page = 1) => api.get(`/notifications?page=${page}`),
  getUnreadCount: () => api.get('/notifications/unread-count'),
  markAllRead: () => api.put('/notifications/read'),
  markRead: (id: number) => api.put(`/notifications/${id}/read`),
};

// Stories
export const storiesApi = {
  getActive: () => api.get('/stories'),
  create: (data: object) => api.post('/stories', data),
  view: (id: number) => api.post(`/stories/${id}/view`),
  delete: (id: number) => api.delete(`/stories/${id}`),
};
