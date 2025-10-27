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
  price: number;
}

export interface CreateMembershipDTO {
  gymId: number;
  membershipType: string;
  startDate: string;
  endDate: string;
  price: number;
}

export interface UpdateMembershipDTO {
  membershipType?: string;
  endDate?: string;
  isActive?: boolean;
}

// Membership API endpoints
export const membershipsApi = {
  // Get all memberships (Admin only)
  async getAll(): Promise<Membership[]> {
    return getJson<Membership[]>('/api/Memberships');
  },

  // Get membership by ID
  async getById(id: number): Promise<Membership> {
    return getJson<Membership>(`/api/Memberships/${id}`);
  },

  // Get memberships by user ID
  async getByUserId(userId: string): Promise<Membership[]> {
    return getJson<Membership[]>(`/api/Memberships/user/${userId}`);
  },

  // Get current user's memberships
  async getMyMemberships(): Promise<Membership[]> {
    return getJson<Membership[]>('/api/Memberships/my-memberships');
  },

  // Get memberships by gym ID (Admin/Moderator only)
  async getByGymId(gymId: number): Promise<Membership[]> {
    return getJson<Membership[]>(`/api/Memberships/gym/${gymId}`);
  },

  // Create membership
  async create(membership: CreateMembershipDTO): Promise<Membership> {
    return postJson<Membership>('/api/Memberships', membership);
  },

  // Update membership
  async update(id: number, membership: UpdateMembershipDTO): Promise<Membership> {
    return putJson<Membership>(`/api/Memberships/${id}`, membership);
  },

  // Delete membership (Admin only)
  async delete(id: number): Promise<void> {
    return deleteRequest(`/api/Memberships/${id}`);
  }
};
