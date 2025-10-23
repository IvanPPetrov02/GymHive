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
  return getJson<User>('/api/Authentication/GetUser');
}

/**
 * Get user by UUID
 */
export async function getUserById(uuid: string): Promise<User> {
  return getJson<User>(`/api/Authentication/${uuid}`);
}

/**
 * Get all users (Admin only)
 */
export async function getAllUsers(): Promise<User[]> {
  return getJson<User[]>('/api/Authentication/GetAllUsers');
}

/**
 * Update user details
 */
export async function updateUser(uuid: string, data: UserUpdateData): Promise<void> {
  await apiFetch(`/api/Authentication/${uuid}`, {
    method: 'PUT',
    body: JSON.stringify(data)
  });
}

/**
 * Delete user
 */
export async function deleteUser(uuid: string): Promise<void> {
  await apiFetch(`/api/Authentication/${uuid}`, {
    method: 'DELETE'
  });
}

/**
 * Change user password
 */
export async function changePassword(uuid: string, data: PasswordChangeData): Promise<void> {
  await apiFetch(`/api/Authentication/change-password/${uuid}`, {
    method: 'POST',
    body: JSON.stringify(data)
  });
}

/**
 * Activate user (Admin only)
 */
export async function activateUser(uuid: string): Promise<void> {
  await apiFetch(`/api/Authentication/activate/${uuid}`, {
    method: 'POST'
  });
}

/**
 * Deactivate user (Admin only)
 */
export async function deactivateUser(uuid: string): Promise<void> {
  await apiFetch(`/api/Authentication/deactivate/${uuid}`, {
    method: 'POST'
  });
}
