<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated } from '../auth';
  import { requireAuth } from '../auth';
  import { gymsApi, type Gym } from '../services/gyms';
  import { membershipsApi, type Membership } from '../services/memberships';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';
  import { push } from 'svelte-spa-router';

  let gyms: Gym[] = [];
  let myMemberships: Membership[] = [];
  let loading = true;
  let error: string | null = null;

  onMount(() => {
    requireAuth('#/feed');
    if ($isAuthenticated) {
      loadFeedData();
    }
  });

  async function loadFeedData() {
    loading = true;
    error = null;
    try {
      // Load gyms and user's memberships in parallel
      const [gymsData, membershipsData] = await Promise.all([
        gymsApi.getAll().catch(() => []),
        membershipsApi.getMyMemberships().catch(() => [])
      ]);
      gyms = gymsData.slice(0, 6); // Show first 6 gyms
      myMemberships = membershipsData;
    } catch (e: any) {
      console.error('Failed to load feed data:', e);
      error = e.message || 'Failed to load feed';
    } finally {
      loading = false;
    }
  }

  function viewGym(id: number) {
    push(`/gyms/${id}`);
  }
</script>

<div class="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <!-- Welcome Header -->
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-gray-900 mb-2">
        Welcome back, {$user?.name || 'Member'}! 👋
      </h1>
      <p class="text-lg text-gray-600">Here's what's happening in your fitness journey</p>
    </div>

    {#if loading}
      <div class="flex justify-center items-center py-20">
        <LoadingSpinner size="lg" />
      </div>
    {:else if error}
      <div class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
        <p class="text-red-600 font-medium">{error}</p>
        <button on:click={loadFeedData} class="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition">
          Try Again
        </button>
      </div>
    {:else}
      <!-- Quick Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <div class="bg-white rounded-xl shadow-md p-6 border-l-4 border-blue-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-500 text-sm font-medium">Active Memberships</p>
              <p class="text-3xl font-bold text-gray-900 mt-1">{myMemberships.length}</p>
            </div>
            <div class="bg-blue-100 rounded-full p-3">
              <svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-md p-6 border-l-4 border-green-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-500 text-sm font-medium">Available Gyms</p>
              <p class="text-3xl font-bold text-gray-900 mt-1">{gyms.length}+</p>
            </div>
            <div class="bg-green-100 rounded-full p-3">
              <svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
              </svg>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-md p-6 border-l-4 border-purple-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-500 text-sm font-medium">Your Profile</p>
              <p class="text-xl font-bold text-gray-900 mt-1">{$user?.role || 'Member'}</p>
            </div>
            <div class="bg-purple-100 rounded-full p-3">
              <svg class="w-8 h-8 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
              </svg>
            </div>
          </div>
        </div>
      </div>

      <!-- My Active Memberships -->
      {#if myMemberships.length > 0}
        <div class="mb-8">
          <div class="flex justify-between items-center mb-4">
            <h2 class="text-2xl font-bold text-gray-900">My Active Memberships</h2>
            <a href="#/profile" class="text-blue-600 hover:text-blue-700 font-medium text-sm">View All →</a>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each myMemberships.slice(0, 3) as membership}
              <div class="bg-white rounded-xl shadow-md overflow-hidden hover:shadow-lg transition">
                <div class="bg-gradient-to-r from-blue-500 to-purple-600 p-4">
                  <h3 class="text-white font-bold text-lg">{membership.gym?.name || 'Gym Membership'}</h3>
                  <p class="text-blue-100 text-sm">{membership.type}</p>
                </div>
                <div class="p-4">
                  <div class="flex justify-between items-center mb-2">
                    <span class="text-gray-600 text-sm">Valid Until:</span>
                    <span class="font-semibold text-gray-900">{new Date(membership.endDate).toLocaleDateString()}</span>
                  </div>
                  <div class="flex justify-between items-center">
                    <span class="text-gray-600 text-sm">Status:</span>
                    <span class="px-2 py-1 bg-green-100 text-green-700 rounded-full text-xs font-medium">Active</span>
                  </div>
                </div>
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Discover Gyms -->
      <div class="mb-8">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-2xl font-bold text-gray-900">Discover Gyms Near You</h2>
          <a href="#/gyms" class="text-blue-600 hover:text-blue-700 font-medium text-sm">Browse All →</a>
        </div>
        {#if gyms.length > 0}
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each gyms as gym}
              <div class="bg-white rounded-xl shadow-md overflow-hidden hover:shadow-xl transition transform hover:-translate-y-1 cursor-pointer" on:click={() => viewGym(gym.id)}>
                <div class="h-48 bg-gradient-to-br from-blue-400 to-purple-500 flex items-center justify-center">
                  <svg class="w-20 h-20 text-white opacity-80" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
                  </svg>
                </div>
                <div class="p-5">
                  <h3 class="text-xl font-bold text-gray-900 mb-2">{gym.name}</h3>
                  <p class="text-gray-600 text-sm mb-3 line-clamp-2">{gym.description || 'No description available'}</p>
                  <div class="flex items-center text-gray-500 text-sm mb-2">
                    <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
                    </svg>
                    <span>{gym.address}</span>
                  </div>
                  {#if gym.openingTime && gym.closingTime}
                    <div class="flex items-center text-gray-500 text-sm">
                      <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                      </svg>
                      <span>{gym.openingTime} - {gym.closingTime}</span>
                    </div>
                  {/if}
                  <button on:click|stopPropagation={() => viewGym(gym.id)} class="mt-4 w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium">
                    View Details
                  </button>
                </div>
              </div>
            {/each}
          </div>
        {:else}
          <div class="bg-gray-50 rounded-xl p-8 text-center">
            <svg class="w-16 h-16 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
            </svg>
            <p class="text-gray-500 font-medium">No gyms available yet</p>
            <p class="text-gray-400 text-sm mt-1">Check back later for new gyms</p>
          </div>
        {/if}
      </div>

      <!-- Quick Actions -->
      <div class="bg-gradient-to-r from-blue-600 to-purple-600 rounded-xl p-8 text-white">
        <h2 class="text-2xl font-bold mb-4">Quick Actions</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <a href="#/gyms" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <div>
              <div class="font-semibold">Find Gyms</div>
              <div class="text-sm text-white/80">Discover new locations</div>
            </div>
          </a>
          <a href="#/profile" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
            </svg>
            <div>
              <div class="font-semibold">My Profile</div>
              <div class="text-sm text-white/80">Manage your account</div>
            </div>
          </a>
          <a href="#/profile" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            <div>
              <div class="font-semibold">My Memberships</div>
              <div class="text-sm text-white/80">View active plans</div>
            </div>
          </a>
        </div>
      </div>
    {/if}
  </div>
</div>
