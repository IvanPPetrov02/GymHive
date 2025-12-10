<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  export let name = '';
  export let surname = '';
  export let email = '';
  export let isSubmitting = false;
  export let error: string | null = null;
  export let success: string | null = null;
  
  const dispatch = createEventDispatcher();
  
  function handleSubmit() {
    dispatch('submit', { name, surname, email });
  }
  
  function handleCancel() {
    dispatch('cancel');
  }
</script>

<div class="max-w-2xl">
  <h2 class="text-2xl font-bold text-gray-800 mb-6">Edit Profile</h2>
  
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
      <label for="edit-name" class="block text-sm font-medium text-gray-700">
        First Name <span class="text-red-500">*</span>
      </label>
      <input 
        id="edit-name"
        type="text" 
        bind:value={name}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Enter your first name"
        required
      />
    </div>
    
    <div class="space-y-2">
      <label for="edit-surname" class="block text-sm font-medium text-gray-700">
        Last Name <span class="text-red-500">*</span>
      </label>
      <input 
        id="edit-surname"
        type="text" 
        bind:value={surname}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Enter your last name"
        required
      />
    </div>
    
    <div class="space-y-2">
      <label for="edit-email" class="block text-sm font-medium text-gray-700">
        Email Address <span class="text-red-500">*</span>
      </label>
      <input 
        id="edit-email"
        type="email" 
        bind:value={email}
        class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
        placeholder="Enter your email"
        required
      />
    </div>
    
    <div class="flex gap-3 pt-4">
      <button 
        type="submit"
        disabled={isSubmitting}
        class="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
      >
        {isSubmitting ? 'Saving...' : 'Save Changes'}
      </button>
      <button 
        type="button"
        on:click={handleCancel}
        class="px-6 py-3 bg-gray-200 text-gray-700 rounded-lg font-semibold hover:bg-gray-300 transition-colors"
      >
        Cancel
      </button>
    </div>
  </form>
</div>
