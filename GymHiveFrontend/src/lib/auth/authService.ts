import { apiFetch, getJson } from '../api';
import type { User } from '../auth';

export interface UserUpdateData {
  email?: string;
  name?: string;
  surname?: string;
}

export interface PasswordChangeData {
  oldPassword: string;
  newPassword: string;
}

/**
 * Get the currently logged-in user's information
 */
export async function getCurrentUser(): Promise<User> {
  return getJson<User>('/api/auth/GetUser');
}

/**
 * Get user by UUID
 */
export async function getUserById(uuid: string): Promise<User> {
  return getJson<User>(`/api/auth/${uuid}`);
}

/**
 * Get all users (Admin only)
 */
export async function getAllUsers(): Promise<User[]> {
  return getJson<User[]>('/api/auth/GetAllUsers');
}

/**
 * Update user details
 */
export async function updateUser(uuid: string, data: UserUpdateData): Promise<void> {
  await apiFetch(`/api/auth/${uuid}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}

/**
 * Delete user
 */
export async function deleteUser(uuid: string): Promise<void> {
  await apiFetch(`/api/auth/${uuid}`, {
    method: 'DELETE'
  });
}

/**
 * Change user password
 */
export async function changePassword(uuid: string, data: PasswordChangeData): Promise<void> {
  await apiFetch(`/api/auth/change-password/${uuid}`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

/**
 * Activate user (Admin only)
 */
export async function activateUser(uuid: string): Promise<void> {
  await apiFetch(`/api/auth/activate/${uuid}`, {
    method: 'POST'
  });
}

/**
 * Deactivate user (Admin only)
 */
export async function deactivateUser(uuid: string): Promise<void> {
  await apiFetch(`/api/auth/deactivate/${uuid}`, {
    method: 'POST'
  });
}
