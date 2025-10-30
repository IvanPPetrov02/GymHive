// API Service for Gym Groups - All requests go through API Gateway
import { getJson, postJson, putJson, deleteRequest } from '../api';

export interface GymGroup {
  id: number;
  name: string;
  description?: string;
  gymId: number;
  gymName: string;
  moderatorId: string;
  moderatorName: string;
  memberCount: number;
  createdAt: string;
  isActive: boolean;
}

export interface GymGroupMember {
  id: number;
  groupId: number;
  userId: string;
  userName: string;
  userEmail: string;
  joinedAt: string;
  isActive: boolean;
}

export interface CreateGymGroupDTO {
  name: string;
  description?: string;
  gymId: number;
  moderatorId: string;
}

export interface UpdateGymGroupDTO {
  name?: string;
  description?: string;
  moderatorId?: string;
  isActive?: boolean;
}

export interface AddMemberDTO {
  userId: string;
}

// Gym Groups API endpoints
export const gymGroupsApi = {
  // Get all gym groups (Admin/Moderator)
  async getAll(): Promise<GymGroup[]> {
    return getJson<GymGroup[]>('/api/gymgroups');
  },

  // Get gym group by ID
  async getById(id: number): Promise<GymGroup> {
    return getJson<GymGroup>(`/api/gymgroups/${id}`);
  },

  // Get gym groups by gym ID
  async getByGymId(gymId: number): Promise<GymGroup[]> {
    return getJson<GymGroup[]>(`/api/gymgroups/gym/${gymId}`);
  },

  // Get groups where user is moderator
  async getModeratedGroups(): Promise<GymGroup[]> {
    return getJson<GymGroup[]>('/api/gymgroups/my-moderated');
  },

  // Create gym group (Admin only)
  async create(group: CreateGymGroupDTO): Promise<GymGroup> {
    return postJson<GymGroup>('/api/gymgroups', group);
  },

  // Update gym group (Admin/Moderator)
  async update(id: number, group: UpdateGymGroupDTO): Promise<GymGroup> {
    return putJson<GymGroup>(`/api/gymgroups/${id}`, group);
  },

  // Delete gym group (Admin only)
  async delete(id: number): Promise<void> {
    return deleteRequest(`/api/gymgroups/${id}`);
  },

  // Get members of a gym group
  async getMembers(groupId: number): Promise<GymGroupMember[]> {
    return getJson<GymGroupMember[]>(`/api/gymgroups/${groupId}/members`);
  },

  // Add member to gym group (Moderator/Admin)
  async addMember(groupId: number, data: AddMemberDTO): Promise<GymGroupMember> {
    return postJson<GymGroupMember>(`/api/gymgroups/${groupId}/members`, data);
  },

  // Remove member from gym group (Moderator/Admin)
  async removeMember(groupId: number, memberId: number): Promise<void> {
    return deleteRequest(`/api/gymgroups/${groupId}/members/${memberId}`);
  }
};
