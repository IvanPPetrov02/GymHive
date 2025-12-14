<script lang="ts">
  import { onMount } from 'svelte';
  import { push } from 'svelte-spa-router';
  import { requireAuth, isLoading, isAuthenticated, user } from '../auth/auth';
  import { gymsApi, type Gym } from '../services/gyms';
  import SearchBar from '../components/gyms/SearchBar.svelte';
  import GymListCard from '../components/gyms/GymListCard.svelte';

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

  function viewGymDetails(gymId: number) {
    push(`/gyms/${gymId}`);
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
      <SearchBar 
        bind:searchQuery
        bind:selectedFilter
        {loading}
        on:refresh={loadGyms}
      />
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
            <GymListCard {gym}>
              <svelte:fragment slot="actions">
                <button 
                  on:click={() => viewGymDetails(gym.id)}
                  class="btn-primary w-full py-2 rounded-xl text-sm"
                >
                  View Details
                </button>
              </svelte:fragment>
            </GymListCard>
          {/each}
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
