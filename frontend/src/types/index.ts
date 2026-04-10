// Types
export interface User {
  id: number;
  username: string;
  email: string;
  fullName?: string;
  bio?: string;
  avatarUrl?: string;
  coverUrl?: string;
  createdAt: string;
  followersCount: number;
  followingCount: number;
  postsCount: number;
  isFollowing: boolean;
}

export interface UserSummary {
  id: number;
  username: string;
  fullName?: string;
  avatarUrl?: string;
}

export interface Post {
  id: number;
  content?: string;
  imageUrl?: string;
  videoUrl?: string;
  visibility: string;
  createdAt: string;
  updatedAt?: string;
  author: UserSummary;
  likesCount: number;
  commentsCount: number;
  isLiked: boolean;
  hashtags: string[];
  sharedPost?: Post;
}

export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  author: UserSummary;
  parentCommentId?: number;
  replies: Comment[];
}

export interface Story {
  id: number;
  mediaUrl: string;
  caption?: string;
  createdAt: string;
  expiresAt: string;
  author: UserSummary;
  viewsCount: number;
  isViewed: boolean;
}

export interface Notification {
  id: number;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
  actor?: UserSummary;
  postId?: number;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrev: boolean;
}

export interface TrendingHashtag {
  name: string;
  postCount: number;
}
