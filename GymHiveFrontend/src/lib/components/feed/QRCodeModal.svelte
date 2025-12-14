<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import Modal from '../ui/Modal.svelte';
  import LoadingSpinner from '../ui/LoadingSpinner.svelte';
  
  export let isOpen: boolean;
  export let qrCodeDataUrl: string;
  export let countdown: number;
  
  const dispatch = createEventDispatcher();
  
  function handleClose() {
    dispatch('close');
  }
</script>

<Modal {isOpen} title="Gym Check-in QR Code" on:close={handleClose}>
  <div class="text-center py-6">
    <div class="mb-4">
      <p class="text-gray-600 mb-2">Show this QR code at the gym entrance</p>
      <p class="text-sm text-gray-500">Code refreshes every 30 seconds for security</p>
    </div>
    
    {#if qrCodeDataUrl}
      <div class="bg-white p-4 rounded-lg inline-block shadow-lg">
        <img src={qrCodeDataUrl} alt="Check-in QR Code" class="mx-auto" />
      </div>
      
      <div class="mt-6 bg-blue-50 rounded-lg p-4">
        <div class="flex items-center justify-center gap-2 text-blue-800 mb-3">
          <svg class="w-5 h-5 animate-pulse" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd"/>
          </svg>
          <span class="font-medium">QR code is active</span>
        </div>
        
        <div class="flex items-center justify-center gap-3">
          <div class="text-3xl font-bold text-blue-800">{countdown}s</div>
          <div class="flex-1 max-w-xs">
            <div class="h-2 bg-blue-200 rounded-full overflow-hidden">
              <div 
                class="h-full bg-blue-600 transition-all duration-1000 ease-linear"
                style="width: {(countdown / 30) * 100}%"
              ></div>
            </div>
          </div>
        </div>
        <p class="text-xs text-blue-600 mt-2">Code refreshes automatically</p>
      </div>
    {:else}
      <div class="py-8">
        <LoadingSpinner />
        <p class="text-gray-500 mt-4">Generating QR code...</p>
      </div>
    {/if}
  </div>
</Modal>
