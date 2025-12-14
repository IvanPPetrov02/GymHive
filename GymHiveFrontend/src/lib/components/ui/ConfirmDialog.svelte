<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let isOpen: boolean = false;
  export let title: string = 'Are you sure?';
  export let message: string = 'This action cannot be undone.';
  export let confirmText: string = 'Confirm';
  export let cancelText: string = 'Cancel';
  export let confirmClass: string = 'btn-danger';
  export let isLoading: boolean = false;
  
  const dispatch = createEventDispatcher();
  
  function handleConfirm() {
    dispatch('confirm');
  }
  
  function handleCancel() {
    dispatch('cancel');
    isOpen = false;
  }
  
  function handleKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape' && !isLoading) {
      handleCancel();
    }
  }
</script>

<svelte:window on:keydown={handleKeydown} />

{#if isOpen}
  <div class="fixed inset-0 z-50 overflow-y-auto">
    <!-- Backdrop -->
    <div 
      class="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
      on:click={handleCancel}
    ></div>
    
    <!-- Dialog -->
    <div class="flex min-h-full items-center justify-center p-4">
      <div 
        class="relative bg-white rounded-2xl shadow-2xl w-full max-w-md transform transition-all"
        on:click|stopPropagation
      >
        <div class="p-6">
          <!-- Icon -->
          <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100 mb-4">
            <svg class="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          
          <!-- Content -->
          <div class="text-center">
            <h3 class="text-lg font-semibold text-gray-900 mb-2">{title}</h3>
            <p class="text-sm text-gray-600">{message}</p>
          </div>
        </div>
        
        <!-- Actions -->
        <div class="flex items-center justify-end gap-3 p-6 border-t border-gray-200 bg-gray-50 rounded-b-2xl">
          <button
            on:click={handleCancel}
            disabled={isLoading}
            class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-4 py-2 rounded-lg disabled:opacity-50"
          >
            {cancelText}
          </button>
          <button
            on:click={handleConfirm}
            disabled={isLoading}
            class="{confirmClass} px-4 py-2 rounded-lg flex items-center gap-2 disabled:opacity-50"
          >
            {#if isLoading}
              <div class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></div>
            {/if}
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  </div>
{/if}


