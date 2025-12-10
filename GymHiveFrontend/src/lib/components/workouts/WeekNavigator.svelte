<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let weekLabel: string;
  export let canGoBack: boolean;
  export let canGoForward: boolean;
  
  const dispatch = createEventDispatcher();
</script>

<div class="bg-white rounded-lg shadow-sm p-4 mb-6">
  <div class="flex items-center justify-between">
    <button 
      on:click={() => dispatch('previous')}
      disabled={!canGoBack}
      class="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition"
    >
      <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
      </svg>
    </button>

    <div class="text-center">
      <h2 class="text-lg font-semibold text-gray-900">{weekLabel}</h2>
      <button 
        on:click={() => dispatch('current')}
        class="text-sm text-blue-600 hover:text-blue-800 font-medium"
      >
        Go to Current Week
      </button>
    </div>

    <button 
      on:click={() => dispatch('next')}
      disabled={!canGoForward}
      class="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition"
    >
      <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
      </svg>
    </button>
  </div>
</div>
