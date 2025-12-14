<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let oldPassword = '';
  export let newPassword = '';
  export let confirmPassword = '';
  export let isSubmitting = false;
  export let error: string | null = null;
  export let success: string | null = null;
  
  const dispatch = createEventDispatcher();
  
  function handleSubmit() {
    dispatch('submit', { oldPassword, newPassword, confirmPassword });
  }
  
  function handleClear() {
    dispatch('clear');
  }
</script>

<div class="max-w-2xl">
  <h2 class="text-2xl font-bold text-gray-800 mb-6">Change Password</h2>
  
  {#if success}
    <div class="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 flex items-center gap-2">
      <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
      </svg>
      {success}
    </div>
  {/if}
  
  {#if error}
    <div class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 flex items-center gap-2">
      <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
        <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
      </svg>
      {error}
    </div>
  {/if}
  
  <form on:submit|preventDefault={handleSubmit} class="space-y-5">
    <div class="space-y-2">
      <label for="old-password" class="block text-sm font-medium text-gray-700">
        Current Password <span class="text-red-500">*</span>
      </label>
      <input 
        id="old-password"
        type="password" 
        bind:value={oldPassword}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Enter current password"
        required
      />
    </div>
    
    <div class="space-y-2">
      <label for="new-password" class="block text-sm font-medium text-gray-700">
        New Password <span class="text-red-500">*</span>
      </label>
      <input 
        id="new-password"
        type="password" 
        bind:value={newPassword}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Enter new password (min 6 characters)"
        required
        minlength="6"
      />
    </div>
    
    <div class="space-y-2">
      <label for="confirm-password" class="block text-sm font-medium text-gray-700">
        Confirm New Password <span class="text-red-500">*</span>
      </label>
      <input 
        id="confirm-password"
        type="password" 
        bind:value={confirmPassword}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Confirm new password"
        required
      />
    </div>
    
    <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 text-sm text-blue-700">
      <div class="flex gap-2">
        <svg class="w-5 h-5 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
        </svg>
        <div>
          <p class="font-medium mb-1">Password Requirements:</p>
          <ul class="list-disc list-inside space-y-1">
            <li>Must be at least 6 characters long</li>
            <li>Use a combination of letters, numbers, and symbols for better security</li>
          </ul>
        </div>
      </div>
    </div>
    
    <div class="flex gap-3 pt-4">
      <button 
        type="submit"
        disabled={isSubmitting}
        class="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
      >
        {isSubmitting ? 'Changing...' : 'Change Password'}
      </button>
      <button 
        type="button"
        on:click={handleClear}
        class="px-6 py-3 bg-gray-200 text-gray-700 rounded-lg font-semibold hover:bg-gray-300 transition-colors"
      >
        Clear
      </button>
    </div>
  </form>
</div>
