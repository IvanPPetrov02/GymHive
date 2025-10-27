<script lang="ts">
  import { onMount } from 'svelte';
  import { requireAuth, isLoading, isAuthenticated, user } from '../auth/auth';
  import { gymsApi, type Gym } from '../services/gyms';

  let gyms: Gym[] = [];
  let loading = false;
  let error = '';
  let searchQuery = '';
  let selectedFilter = 'all';

  async function loadGyms() {
    loading = true;
    error = '';
    try {
      gyms = await gymsApi.getAll();
    } catch (e: any) {
      console.error('Failed to load gyms:', e);
      error = e.message || 'Failed to load gyms';
    } finally {
      loading = false;
    }
  }

  $: filteredGyms = gyms.filter(gym =>
    gym.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    gym.address.toLowerCase().includes(searchQuery.toLowerCase())
  );

  $: isAdmin = $user?.role === 'Admin';

  onMount(() => {
    // Trigger auth requirement
    requireAuth('#/gyms');
    // Load gyms data
    if ($isAuthenticated) {
      loadGyms();
    }
  });
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-gray-600 animate-pulse text-lg">Loading authentication...</div>
  </div>
{:else if $isAuthenticated}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-blue-600 to-purple-600 text-white py-20">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl md:text-5xl font-bold mb-3">Find Your Perfect Gym</h1>
        <p class="text-lg md:text-xl text-blue-100">Discover gyms in your area and join the community</p>
      </div>
    </div>

    <!-- Error Message -->
    {#if error}
      <div class="max-w-7xl mx-auto px-6 py-4">
        <div class="bg-red-50 border border-red-200 text-red-700 px-5 py-3 rounded-xl">
          {error}
        </div>
      </div>
    {/if}

    <!-- Search and Filters -->
    <div class="max-w-7xl mx-auto px-6 -mt-10">
      <div class="card-panel p-6">
        <div class="flex flex-col md:flex-row gap-5">
          <div class="flex-1">
            <input
              type="text"
              bind:value={searchQuery}
              placeholder="Search gyms by name or address..."
              class="no-border-input w-full"
            />
          </div>
          <select
            bind:value={selectedFilter}
            class="no-border-input w-full md:w-auto"
          >
            <option value="all">All Gyms</option>
            <option value="nearby">Nearby</option>
            <option value="popular">Most Popular</option>
            <option value="rating">Highest Rated</option>
          </select>
          <button 
            on:click={loadGyms} 
            disabled={loading}
            class="btn-primary md:w-auto w-full py-3 rounded-xl shadow-lg disabled:opacity-50"
          >
            {loading ? 'Loading...' : 'Refresh'}
          </button>
        </div>
      </div>
    </div>

    <!-- Gym List -->
    <div class="max-w-7xl mx-auto px-6 py-14">
      {#if loading}
        <div class="text-center py-20">
          <div class="text-gray-600 animate-pulse text-lg">Loading gyms...</div>
        </div>
      {:else if filteredGyms.length === 0}
        <div class="text-center py-20">
          <div class="text-6xl mb-4">🏋️</div>
          <h3 class="text-2xl font-bold text-gray-800 mb-2">No gyms found</h3>
          <p class="text-gray-600">
            {searchQuery ? 'Try a different search term' : 'No gyms available yet'}
          </p>
        </div>
      {:else}
        <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-10">
          {#each filteredGyms as gym (gym.id)}
            <div class="bg-white rounded-2xl shadow-lg hover:shadow-2xl transition overflow-hidden">
              <div class="h-48 bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-6xl">
                🏋️
              </div>
              <div class="p-6">
                <h3 class="text-2xl font-bold text-gray-800 mb-2">{gym.name}</h3>
                <p class="text-gray-600 mb-4 flex items-center gap-2">
                  <span class="text-lg">📍</span>
                  {gym.address}
                </p>
                
                {#if gym.description}
                  <p class="text-gray-600 text-sm mb-4 line-clamp-2">{gym.description}</p>
                {/if}

                <div class="flex flex-wrap gap-2 mb-4">
                  {#if gym.phoneNumber}
                    <span class="px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-xs">📞 {gym.phoneNumber}</span>
                  {/if}
                  {#if gym.email}
                    <span class="px-3 py-1 bg-purple-100 text-purple-700 rounded-full text-xs">✉️ {gym.email}</span>
                  {/if}
                </div>

                {#if gym.openingTime && gym.closingTime}
                  <p class="text-gray-600 text-sm mb-4">
                    ⏰ {gym.openingTime} - {gym.closingTime}
                  </p>
                {/if}

                <div class="flex gap-3">
                  <button class="btn-primary flex-1 py-2 rounded-xl text-sm">
                    View Details
                  </button>
                  <button class="btn-outline flex-1 py-2 rounded-xl text-sm">
                    Join
                  </button>
                </div>
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  </div>
            <div class="h-48 bg-gray-200">
              <img src={gym.image} alt={gym.name} class="w-full h-full object-cover" />
            </div>
            <div class="p-6 flex flex-col h-full">
              <h3 class="text-2xl font-bold text-gray-800 mb-2">{gym.name}</h3>
              <p class="text-gray-600 mb-4 flex items-center">
                <svg class="w-5 h-5 mr-2 text-blue-600" viewBox="0 0 20 20" aria-hidden="true">
                  <path fill="currentColor" fill-rule="evenodd" d="M5.05 4.05a7 7 0 119.9 9.9L10 18.9l-4.95-4.95a7 7 0 010-9.9zM10 11a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd"/>
                </svg>
                {gym.location}
              </p>
              <div class="flex items-center justify-between mb-4 text-sm">
                <div class="flex items-center gap-1 text-yellow-500 font-semibold">
                  <span>★</span>
                  <span class="text-gray-800">{gym.rating}</span>
                </div>
                <div class="text-gray-600">
                  <span class="font-semibold">{gym.members}</span> members
                </div>
              </div>
              <div class="flex flex-wrap gap-2 mb-4">
                {#each gym.amenities as amenity}
                  <span class="badge">{amenity}</span>
                {/each}
              </div>
              <button class="btn-primary w-full py-3 rounded-lg mt-auto">
                View Details
              </button>
            </div>
          </div>
        {/each}
      </div>

      {#if filteredGyms.length === 0}
        <div class="text-center py-16">
          <p class="text-gray-600 text-lg">No gyms found. Try adjusting your search.</p>
        </div>
      {/if}
    </div>
  </div>
{:else}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="text-center space-y-6">
      <h2 class="text-3xl font-bold text-gray-800">Redirecting to Login...</h2>
      <p class="text-gray-600">You need to authenticate to access gym listings.</p>
    </div>
  </div>
{/if}
