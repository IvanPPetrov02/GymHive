// API Service for User Management (Admin) - All requests go through API Gateway
import { getJson, putJson, deleteRequest, postJson } from '../api';

export interface UserProfile {
  uuid: string;
  email: string;
  name: string;
  surname: string;
  role: 'User' | 'Moderator' | 'Admin';
  isActive: boolean;
  createdAt: string;
  membershipCount?: number;
}

export interface UpdateUserDTO {
  name?: string;
  surname?: string;
  email?: string;
  role?: 'User' | 'Moderator' | 'Admin';
  isActive?: boolean;
}

export interface ChangeUserRoleDTO {
  userId: string;
  newRole: 'User' | 'Moderator' | 'Admin';
}

// Users API endpoints (Admin only)
export const usersApi = {
  // Get all users (Admin only)
  async getAll(): Promise<UserProfile[]> {
    return getJson<UserProfile[]>('/api/auth/GetAllUsers');
  },

  // Get user by ID (Admin only)
  async getById(userId: string): Promise<UserProfile> {
    return getJson<UserProfile>(`/api/auth/users/${userId}`);
  },

  // Update user (Admin only)
  async update(userId: string, data: UpdateUserDTO): Promise<UserProfile> {
    return putJson<UserProfile>(`/api/auth/users/${userId}`, data);
  },

  // Change user role (Admin only)
  async changeRole(userId: string, newRole: 'User' | 'Moderator' | 'Admin'): Promise<void> {
    return postJson<void>(`/api/Authentication/update-role/${userId}`, { role: newRole });
  },

  // Toggle user active status (Admin only)
  async toggleActive(userId: string): Promise<UserProfile> {
    return putJson<UserProfile>(`/api/auth/users/${userId}/toggle-active`, {});
  },

  // Delete user (Admin only)
  async delete(userId: string): Promise<void> {
    return deleteRequest(`/api/auth/users/${userId}`);
  },

  // Search users (Admin only)
  async search(query: string): Promise<UserProfile[]> {
    return getJson<UserProfile[]>(`/api/auth/users/search?q=${encodeURIComponent(query)}`);
  }
};
