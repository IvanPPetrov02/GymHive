<script lang="ts">
  import { onMount } from 'svelte';
  import { requireAuth, isLoading, isAuthenticated, user, logout } from '../auth/auth';
  import { membershipsApi, type Membership } from '../services/memberships';
  import { push } from 'svelte-spa-router';

  let memberships: Membership[] = [];
  let loading = false;
  let error = '';

  async function loadMemberships() {
    loading = true;
    error = '';
    try {
      memberships = await membershipsApi.getMyMemberships();
    } catch (e: any) {
      console.error('Failed to load memberships:', e);
      error = e.message || 'Failed to load memberships';
      memberships = []; // Clear memberships on error
    } finally {
      loading = false;
    }
  }

  async function handleLogout() {
    await logout();
    push('/login');
  }

  onMount(() => {
    requireAuth('#/profile');
    if ($isAuthenticated) {
      loadMemberships();
    }
  });
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-gray-600 animate-pulse text-lg">Loading...</div>
  </div>
{:else if $isAuthenticated && $user}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-blue-600 to-purple-600 text-white py-20">
      <div class="max-w-7xl mx-auto px-6">
        <div class="flex items-center gap-6">
          <div class="h-24 w-24 rounded-full bg-white/20 backdrop-blur flex items-center justify-center text-4xl font-bold">
            {$user.name[0]}{$user.surname[0]}
          </div>
          <div>
            <h1 class="text-4xl md:text-5xl font-bold mb-2">{$user.name} {$user.surname}</h1>
            <p class="text-lg text-blue-100">{$user.email}</p>
            <div class="flex gap-3 mt-3">
              <span class="px-4 py-1 bg-white/20 backdrop-blur rounded-full text-sm">
                {$user.role}
              </span>
              <span class="px-4 py-1 bg-white/20 backdrop-blur rounded-full text-sm">
                {$user.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="max-w-7xl mx-auto px-6 py-14">
      <!-- Actions -->
      <div class="mb-10 flex gap-4">
        <button on:click={handleLogout} class="btn-outline py-3 px-6 rounded-xl">
          Logout
        </button>
        <a href="#/gyms" class="btn-primary py-3 px-6 rounded-xl">
          Browse Gyms
        </a>
      </div>

      <!-- Error Message -->
      {#if error}
        <div class="mb-6 bg-red-50 border border-red-200 text-red-700 px-5 py-3 rounded-xl">
          {error}
        </div>
      {/if}

      <!-- Memberships Section -->
      <div class="card-panel p-8">
        <div class="flex justify-between items-center mb-6">
          <h2 class="text-3xl font-bold text-gray-800">My Memberships</h2>
          <button 
            on:click={loadMemberships} 
            disabled={loading}
            class="btn-outline py-2 px-4 rounded-xl text-sm disabled:opacity-50"
          >
            {loading ? 'Loading...' : 'Refresh'}
          </button>
        </div>

        {#if loading}
          <div class="text-center py-10">
            <div class="text-gray-600 animate-pulse">Loading memberships...</div>
          </div>
        {:else if memberships.length === 0}
          <div class="text-center py-10">
            <div class="text-6xl mb-4">💪</div>
            <h3 class="text-xl font-bold text-gray-800 mb-2">No memberships yet</h3>
            <p class="text-gray-600 mb-6">Start your fitness journey by joining a gym!</p>
            <a href="#/gyms" class="btn-primary inline-block py-3 px-6 rounded-xl">
              Find a Gym
            </a>
          </div>
        {:else}
          <div class="space-y-4">
            {#each memberships as membership (membership.id)}
              <div class="bg-gray-50 rounded-xl p-6 hover:shadow-md transition">
                <div class="flex justify-between items-start">
                  <div class="flex-1">
                    <h3 class="text-xl font-bold text-gray-800 mb-2">{membership.gymName}</h3>
                    <div class="space-y-1 text-sm text-gray-600">
                      <p><span class="font-semibold">Type:</span> {membership.membershipType}</p>
                      <p><span class="font-semibold">Start:</span> {new Date(membership.startDate).toLocaleDateString()}</p>
                      <p><span class="font-semibold">End:</span> {new Date(membership.endDate).toLocaleDateString()}</p>
                      <p><span class="font-semibold">Price:</span> ${membership.price.toFixed(2)}</p>
                    </div>
                  </div>
                  <div>
                    {#if membership.isActive}
                      <span class="px-4 py-2 bg-green-100 text-green-700 rounded-full text-sm font-semibold">
                        Active
                      </span>
                    {:else}
                      <span class="px-4 py-2 bg-gray-200 text-gray-700 rounded-full text-sm font-semibold">
                        Inactive
                      </span>
                    {/if}
                  </div>
                </div>
              </div>
            {/each}
          </div>
        {/if}
      </div>

      <!-- Account Info -->
      <div class="card-panel p-8 mt-10">
        <h2 class="text-3xl font-bold text-gray-800 mb-6">Account Information</h2>
        <div class="space-y-4 text-gray-700">
          <div class="flex justify-between py-3 border-b">
            <span class="font-semibold">Name:</span>
            <span>{$user.name} {$user.surname}</span>
          </div>
          <div class="flex justify-between py-3 border-b">
            <span class="font-semibold">Email:</span>
            <span>{$user.email}</span>
          </div>
          <div class="flex justify-between py-3 border-b">
            <span class="font-semibold">Role:</span>
            <span>{$user.role}</span>
          </div>
          <div class="flex justify-between py-3 border-b">
            <span class="font-semibold">Status:</span>
            <span>{$user.isActive ? 'Active' : 'Inactive'}</span>
          </div>
          <div class="flex justify-between py-3">
            <span class="font-semibold">Member Since:</span>
            <span>{new Date($user.createdAt).toLocaleDateString()}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
{:else}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-center">
      <h2 class="text-2xl font-bold text-gray-800 mb-4">Not Authenticated</h2>
      <a href="#/login" class="btn-primary py-3 px-6 rounded-xl">Go to Login</a>
    </div>
  </div>
{/if}
