<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let isOpen: boolean = false;
  export let title: string = '';
  export let size: 'small' | 'medium' | 'large' | 'full' = 'medium';
  export let closeOnBackdrop: boolean = true;
  
  const dispatch = createEventDispatcher();
  
  const sizes = {
    small: 'max-w-md',
    medium: 'max-w-2xl',
    large: 'max-w-4xl',
    full: 'max-w-7xl'
  };
  
  function handleBackdropClick() {
    if (closeOnBackdrop) {
      close();
    }
  }
  
  function close() {
    dispatch('close');
    isOpen = false;
  }
  
  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && closeOnBackdrop) {
      close();
    }
  }
</script>

<svelte:window on:keydown={handleKeydown} />

{#if isOpen}
  <div class="fixed inset-0 z-50 overflow-y-auto">
    <!-- Backdrop -->
    <div 
      class="fixed inset-0 bg-black/30 backdrop-blur-sm transition-opacity"
      on:click={handleBackdropClick}
    ></div>
    
    <!-- Modal -->
    <div class="flex min-h-full items-center justify-center p-4">
      <div 
        class="relative bg-white/95 backdrop-blur-md rounded-2xl shadow-2xl w-full {sizes[size]} transform transition-all"
        on:click|stopPropagation
      >
        <!-- Header -->
        {#if title || $$slots.header}
          <div class="flex items-center justify-between p-6 border-b border-gray-200">
            {#if $$slots.header}
              <slot name="header" />
            {:else}
              <h3 class="text-2xl font-bold text-gray-900">{title}</h3>
            {/if}
            <button
              on:click={close}
              class="text-gray-400 hover:text-gray-600 transition-colors p-2 rounded-lg hover:bg-gray-100"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        {/if}
        
        <!-- Body -->
        <div class="p-6">
          <slot />
        </div>
        
        <!-- Footer -->
        {#if $$slots.footer}
          <div class="flex items-center justify-end gap-3 p-6 border-t border-gray-200 bg-gray-50 rounded-b-2xl">
            <slot name="footer" />
          </div>
        {/if}
      </div>
    </div>
  </div>
{/if}
