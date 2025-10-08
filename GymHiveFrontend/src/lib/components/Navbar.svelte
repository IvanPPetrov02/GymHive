<script lang="ts">
  import { location } from 'svelte-spa-router';
  import { isAuthenticated, user, authConfigMissing } from '../auth/auth';
  import { login, logout } from '../auth/auth';

  let mobileMenuOpen = false;

  function toggleMobileMenu() {
    mobileMenuOpen = !mobileMenuOpen;
  }

  function isActive(path: string) {
    return $location === path;
  }
</script>

<nav class="bg-white shadow-md">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="flex justify-between h-16">
      <div class="flex items-center gap-10">
        <a href="#/" class="flex items-center focus:outline-none focus:ring-2 focus:ring-blue-500 rounded select-none">
          <span class="text-2xl font-bold text-blue-600 tracking-tight">GymHive</span>
        </a>
        <div class="hidden md:flex md:items-center md:space-x-2">
          <a href="#/" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Home</a>
          <a href="#/gyms" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/gyms') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Find Gyms</a>
          {#if $isAuthenticated}
            <a href="#/profile" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/profile') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Profile</a>
          {/if}
        </div>
      </div>

      <div class="hidden md:flex md:items-center md:space-x-3">
        {#if $isAuthenticated}
          <div class="flex items-center gap-3 pr-2">
            {#if $user?.picture}
              <img src={$user.picture} alt="avatar" class="h-9 w-9 rounded-full ring-2 ring-blue-200" referrerpolicy="no-referrer" />
            {/if}
            <div class="text-sm text-right">
              <div class="font-semibold text-gray-800 leading-tight max-w-[140px] truncate">{$user?.name || $user?.email}</div>
              <div class="text-gray-500 text-xs">Member</div>
            </div>
          </div>
          <button on:click={() => logout()} class="px-5 py-2 rounded-lg text-sm font-semibold text-white bg-red-600 hover:bg-red-700 transition shadow-sm">Logout</button>
        {:else}
          {#if $authConfigMissing}
            <button disabled title="Auth not configured" class="px-5 py-2 rounded-lg text-sm font-semibold text-white bg-gray-400 cursor-not-allowed opacity-70">Auth Not Configured</button>
          {:else}
            <button on:click={() => login($location || '#/')} class="px-5 py-2 rounded-lg text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow-sm">Login / Sign Up</button>
          {/if}
        {/if}
      </div>

      <div class="flex items-center md:hidden">
        <button on:click={toggleMobileMenu} type="button" aria-expanded={mobileMenuOpen} aria-controls="mobile-menu" aria-label="Toggle navigation" class="inline-flex items-center justify-center p-2 rounded-md text-gray-600 hover:text-gray-900 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500">
          {#if !mobileMenuOpen}
            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16"/></svg>
          {:else}
            <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/></svg>
          {/if}
        </button>
      </div>
    </div>
  </div>

  {#if mobileMenuOpen}
    <div id="mobile-menu" class="md:hidden px-4 pb-6 space-y-2 bg-white shadow-inner">
      <a href="#/" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Home</a>
      <a href="#/gyms" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/gyms') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Find Gyms</a>
      {#if $isAuthenticated}
        <a href="#/profile" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/profile') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Profile</a>
      {/if}
      <div class="h-px bg-gray-200 my-2"></div>
      {#if $isAuthenticated}
        <div class="flex items-center gap-3 px-4 py-2">
          {#if $user?.picture}
            <img src={$user.picture} alt="avatar" class="h-10 w-10 rounded-full ring-2 ring-blue-200" referrerpolicy="no-referrer" />
          {/if}
          <div class="text-sm">
            <div class="font-semibold text-gray-800 leading-tight">{$user?.name || $user?.email}</div>
            <div class="text-gray-500 text-xs">Member</div>
          </div>
        </div>
        <button on:click={() => logout()} class="w-full text-left px-4 py-3 rounded-lg text-sm font-semibold text-white bg-red-600 hover:bg-red-700 transition shadow">Logout</button>
      {:else}
        {#if $authConfigMissing}
          <button disabled class="block w-full text-left px-4 py-3 rounded-lg text-sm font-semibold text-white bg-gray-400 cursor-not-allowed opacity-70">Auth Not Configured</button>
        {:else}
          <button on:click={() => login($location || '#/')} class="block w-full text-left px-4 py-3 rounded-lg text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow">Login / Sign Up</button>
        {/if}
      {/if}
    </div>
  {/if}
</nav>
