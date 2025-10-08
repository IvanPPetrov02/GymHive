<script lang="ts">
  import { onMount } from 'svelte';
  import { requireAuth, isLoading, isAuthenticated } from '../auth/auth';

  let gyms = [
    {
      id: 1,
      name: 'PowerFit Gym',
      location: 'Downtown, City Center',
      rating: 4.5,
      members: 1250,
      image: 'https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=400',
      amenities: ['Pool', 'Sauna', 'Personal Training']
    },
    {
      id: 2,
      name: 'Iron Paradise',
      location: 'West Side, Block 5',
      rating: 4.8,
      members: 890,
      image: 'https://images.unsplash.com/photo-1571902943202-507ec2618e8f?w=400',
      amenities: ['Free Weights', 'Cardio', 'Group Classes']
    },
    {
      id: 3,
      name: 'Flex Zone',
      location: 'East District, Main Street',
      rating: 4.3,
      members: 650,
      image: 'https://images.unsplash.com/photo-1540497077202-7c8a3999166f?w=400',
      amenities: ['Yoga Studio', 'Pilates', 'Spa']
    }
  ];

  let searchQuery = '';
  let selectedFilter = 'all';

  async function loadGyms() {
    // TODO: Connect to GymsService API
    console.log('Loading gyms from API...');
  }

  $: filteredGyms = gyms.filter(gym =>
    gym.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    gym.location.toLowerCase().includes(searchQuery.toLowerCase())
  );

  onMount(() => {
    // Trigger auth requirement; if not authenticated user will be redirected to Auth0 universal login.
    requireAuth('#/gyms');
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

    <!-- Search and Filters -->
    <div class="max-w-7xl mx-auto px-6 -mt-10">
      <div class="card-panel p-6">
        <div class="flex flex-col md:flex-row gap-5">
          <div class="flex-1">
            <input
              type="text"
              bind:value={searchQuery}
              placeholder="Search gyms by name or location..."
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
          <button on:click={loadGyms} class="btn-primary md:w-auto w-full py-3 rounded-xl shadow-lg">
            Search
          </button>
        </div>
      </div>
    </div>

    <!-- Gym List -->
    <div class="max-w-7xl mx-auto px-6 py-14">
      <div class="grid md:grid-cols-2 lg:grid-cols-3 gap-10">
        {#each filteredGyms as gym (gym.id)}
          <div class="bg-white rounded-2xl shadow-lg hover:shadow-2xl transition overflow-hidden">
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
