<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let searchQuery = '';
  export let selectedFilter = 'all';
  export let loading = false;
  
  const dispatch = createEventDispatcher();
  
  function handleRefresh() {
    dispatch('refresh');
  }
</script>

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
      on:click={handleRefresh} 
      disabled={loading}
      class="btn-primary md:w-auto w-full py-3 rounded-xl shadow-lg disabled:opacity-50"
    >
      {loading ? 'Loading...' : 'Refresh'}
    </button>
  </div>
</div>
