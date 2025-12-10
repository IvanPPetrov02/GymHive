<script lang="ts">
  import type { Gym } from '../../services/gyms';
  import { createEventDispatcher } from 'svelte';
  
  export let gym: Gym;
  
  const dispatch = createEventDispatcher();
  
  function handlePurchase() {
    dispatch('purchase');
  }
</script>

<div class="card-panel p-6 sticky top-6">
  <h2 class="text-xl font-bold text-gray-900 mb-4">Contact Information</h2>
  
  <div class="space-y-4">
    {#if gym.phoneNumber}
      <div>
        <div class="text-sm text-gray-600 mb-1">Phone</div>
        <a href="tel:{gym.phoneNumber}" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
          </svg>
          {gym.phoneNumber}
        </a>
      </div>
    {/if}
    
    {#if gym.email}
      <div>
        <div class="text-sm text-gray-600 mb-1">Email</div>
        <a href="mailto:{gym.email}" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
          </svg>
          {gym.email}
        </a>
      </div>
    {/if}
    
    {#if gym.website}
      <div>
        <div class="text-sm text-gray-600 mb-1">Website</div>
        <a href={gym.website} target="_blank" rel="noopener noreferrer" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
          </svg>
          Visit Website
        </a>
      </div>
    {/if}
  </div>

  <button
    on:click={handlePurchase}
    class="btn-primary w-full mt-6 py-4 rounded-lg text-lg font-semibold shadow-lg hover:shadow-xl transition-shadow"
  >
    Purchase Membership
  </button>
</div>
