// API Service for Gyms - All requests go through API Gateway
import { apiFetch, getJson, postJson, putJson, deleteRequest } from '../api';

export interface Gym {
  id: number;
  name: string;
  address: string;
  description?: string;
  phoneNumber?: string;
  email?: string;
  website?: string;
  openingTime?: string;
  closingTime?: string;
  facilities?: string[];
  rating?: number;
  memberCount?: number;
}

export interface CreateGymDTO {
  name: string;
  address: string;
  description?: string;
  phoneNumber?: string;
  email?: string;
  website?: string;
  openingTime?: string;
  closingTime?: string;
}

export interface UpdateGymDTO {
  name?: string;
  address?: string;
  description?: string;
  phoneNumber?: string;
  email?: string;
  website?: string;
  openingTime?: string;
  closingTime?: string;
}

// Gym API endpoints
export const gymsApi = {
  // Get all gyms (public)
  async getAll(): Promise<Gym[]> {
    return getJson<Gym[]>('/api/gyms');
  },

  // Get gym by ID (public)
  async getById(id: number): Promise<Gym> {
    return getJson<Gym>(`/api/gyms/${id}`);
  },

  // Create gym (Admin only)
  async create(gym: CreateGymDTO): Promise<Gym> {
    return postJson<Gym>('/api/gyms', gym);
  },

  // Update gym (Admin only)
  async update(id: number, gym: UpdateGymDTO): Promise<Gym> {
    return putJson<Gym>(`/api/gyms/${id}`, gym);
  },

  // Delete gym (Admin only)
  async delete(id: number): Promise<void> {
    return deleteRequest(`/api/gyms/${id}`);
  }
};
