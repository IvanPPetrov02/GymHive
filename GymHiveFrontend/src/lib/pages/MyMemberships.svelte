<script lang="ts">
  import { onMount } from 'svelte';
  import { getApiBase } from '../api';
  import { getAccessToken } from '../auth';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';

  interface Membership {
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

  let memberships: Membership[] = [];
  let loading = true;
  let error = '';

  async function fetchMemberships() {
    try {
      loading = true;
      error = '';
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/memberships/my-memberships`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to fetch memberships');
      }

      memberships = await response.json();
    } catch (err: any) {
      error = err.message || 'Failed to load memberships';
      console.error('Error fetching memberships:', err);
    } finally {
      loading = false;
    }
  }

  function formatDate(dateString: string) {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  function isExpiringSoon(endDate: string) {
    const end = new Date(endDate);
    const now = new Date();
    const daysUntilExpiry = Math.ceil((end.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
    return daysUntilExpiry <= 7 && daysUntilExpiry > 0;
  }

  function isExpired(endDate: string) {
    return new Date(endDate) < new Date();
  }

  async function toggleAutoRenew(membershipId: number, autoRenew: boolean) {
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/memberships/${membershipId}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ autoRenew })
      });

      if (!response.ok) {
        throw new Error('Failed to update auto-renew');
      }

      // Update local state
      memberships = memberships.map(m => 
        m.id === membershipId ? { ...m, autoRenew } : m
      );
    } catch (err: any) {
      console.error('Error updating auto-renew:', err);
      // Revert checkbox on error
      memberships = memberships.map(m => 
        m.id === membershipId ? { ...m, autoRenew: !autoRenew } : m
      );
    }
  }

  onMount(() => {
    fetchMemberships();
  });
</script>

<div class="min-h-screen bg-gray-50 py-8">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">My Memberships</h1>
      <p class="mt-2 text-gray-600">Manage and view all your gym memberships</p>
    </div>

    {#if loading}
      <div class="flex justify-center items-center py-12">
        <LoadingSpinner />
      </div>
    {:else if error}
      <div class="bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{error}</p>
        <button on:click={fetchMemberships} class="mt-2 text-red-600 hover:text-red-800 font-medium">
          Try Again
        </button>
      </div>
    {:else if memberships.length === 0}
      <div class="bg-white rounded-lg shadow-sm p-12 text-center">
        <svg class="mx-auto h-16 w-16 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">No memberships yet</h3>
        <p class="mt-2 text-gray-500">Get started by browsing gyms and purchasing a membership</p>
        <a href="#/gyms" class="mt-6 inline-flex items-center px-4 py-2 border border-transparent rounded-lg shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700">
          Browse Gyms
        </a>
      </div>
    {:else}
      <div class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        {#each memberships as membership}
          <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden hover:shadow-md transition">
            <div class="p-6">
              <div class="flex items-start justify-between mb-4">
                <div>
                  <h3 class="text-lg font-semibold text-gray-900">{membership.gymName}</h3>
                  <p class="text-sm text-gray-500 mt-1">{membership.membershipType}</p>
                </div>
                {#if membership.isActive && !isExpired(membership.endDate)}
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                    Active
                  </span>
                {:else if isExpired(membership.endDate)}
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                    Expired
                  </span>
                {:else}
                  <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                    Inactive
                  </span>
                {/if}
              </div>

              {#if isExpiringSoon(membership.endDate) && membership.isActive}
                <div class="mb-4 p-2 bg-yellow-50 border border-yellow-200 rounded text-sm text-yellow-800">
                  ⚠️ Expiring soon!
                </div>
              {/if}

              <div class="space-y-2 text-sm">
                <div class="flex justify-between">
                  <span class="text-gray-600">Start Date:</span>
                  <span class="font-medium text-gray-900">{formatDate(membership.startDate)}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">End Date:</span>
                  <span class="font-medium text-gray-900">{formatDate(membership.endDate)}</span>
                </div>
                <div class="flex justify-between">
                  <span class="text-gray-600">Price:</span>
                  <span class="font-medium text-gray-900">${membership.price.toFixed(2)}</span>
                </div>
              </div>

              {#if membership.isActive && !isExpired(membership.endDate)}
                <div class="mt-4 flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                  <label for="autoRenew-{membership.id}" class="text-sm font-medium text-gray-700 cursor-pointer">
                    Auto-renew
                  </label>
                  <div class="relative inline-block w-10 mr-2 align-middle select-none">
                    <input
                      type="checkbox"
                      id="autoRenew-{membership.id}"
                      checked={membership.autoRenew}
                      on:change={(e) => toggleAutoRenew(membership.id, e.currentTarget.checked)}
                      class="toggle-checkbox absolute block w-6 h-6 rounded-full bg-white border-4 appearance-none cursor-pointer transition-transform duration-200 ease-in-out"
                      style="right: {membership.autoRenew ? '0' : 'calc(100% - 1.5rem)'}; border-color: {membership.autoRenew ? '#3b82f6' : '#d1d5db'};"
                    />
                    <label
                      for="autoRenew-{membership.id}"
                      class="toggle-label block overflow-hidden h-6 rounded-full cursor-pointer"
                      style="background-color: {membership.autoRenew ? '#3b82f6' : '#d1d5db'};"
                    ></label>
                  </div>
                </div>
              {/if}

              <div class="mt-6 flex gap-2">
                <a href="#/gyms/{membership.gymId}" class="flex-1 text-center px-4 py-2 border border-blue-600 rounded-lg text-sm font-medium text-blue-600 hover:bg-blue-50 transition">
                  View Gym
                </a>
                {#if membership.isActive && !isExpired(membership.endDate)}
                  <button class="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition">
                    Renew
                  </button>
                {/if}
              </div>
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>

<style>
  .toggle-checkbox:checked {
    right: 0;
    border-color: #3b82f6;
  }
  .toggle-checkbox {
    right: calc(100% - 1.5rem);
    border-color: #d1d5db;
  }
  .toggle-label {
    transition: background-color 200ms ease-in-out;
  }
</style>
