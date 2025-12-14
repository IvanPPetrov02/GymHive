<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import type { Gym } from '../../services/gyms';
  
  export let gym: Gym;
  
  const dispatch = createEventDispatcher();
  
  function handleClick() {
    dispatch('view', gym.id);
  }
</script>

<div 
  class="bg-white rounded-xl shadow-md overflow-hidden hover:shadow-xl transition transform hover:-translate-y-1 cursor-pointer" 
  on:click={handleClick}
  on:keypress={(e) => e.key === 'Enter' && handleClick()}
  role="button"
  tabindex="0"
>
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
    <button 
      on:click|stopPropagation={handleClick} 
      class="mt-4 w-full px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium"
    >
      View Details
    </button>
  </div>
</div>
