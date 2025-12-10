// API Service for Memberships - All requests go through API Gateway
import { getJson, postJson, putJson, deleteRequest } from '../api';

export interface Membership {
  id: number;
  userId: string;
  gymId: number;
  gymName: string;
  membershipType: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
  autoRenew: boolean;
  price: number;
}

export interface CreateMembershipDTO {
  gymId: number;
  membershipType: string;
  startDate: string;
  endDate: string;
  price: number;
  autoRenew: boolean;
}

export interface UpdateMembershipDTO {
  membershipType?: string;
  endDate?: string;
  isActive?: boolean;
  autoRenew?: boolean;
}

// Membership API endpoints
export const membershipsApi = {
  // Get all memberships (Admin only)
  async getAll(): Promise<Membership[]> {
    return getJson<Membership[]>('/api/memberships');
  },

  // Get membership by ID
  async getById(id: number): Promise<Membership> {
    return getJson<Membership>(`/api/memberships/${id}`);
  },

  // Get memberships by user ID
  async getByUserId(userId: string): Promise<Membership[]> {
    return getJson<Membership[]>(`/api/memberships/user/${userId}`);
  },

  // Get current user's memberships
  async getMyMemberships(): Promise<Membership[]> {
    return getJson<Membership[]>('/api/memberships/my-memberships');
  },

  // Get memberships by gym ID (Admin/Moderator only)
  async getByGymId(gymId: number): Promise<Membership[]> {
    return getJson<Membership[]>(`/api/memberships/gym/${gymId}`);
  },

  // Create membership
  async create(membership: CreateMembershipDTO): Promise<Membership> {
    return postJson<Membership>('/api/memberships', membership);
  },

  // Update membership
  async update(id: number, membership: UpdateMembershipDTO): Promise<Membership> {
    return putJson<Membership>(`/api/memberships/${id}`, membership);
  },

  // Delete membership (Admin only)
  async delete(id: number): Promise<void> {
    return deleteRequest(`/api/memberships/${id}`);
  }
};
