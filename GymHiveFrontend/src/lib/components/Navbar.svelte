<script lang="ts">
  import { onMount } from 'svelte';
  import { location, push } from 'svelte-spa-router';
  import { isAuthenticated, user, getAccessToken } from '../auth';
  import { logout } from '../auth';
  import { getApiBase } from '../api';

  let mobileMenuOpen = false;
  let unreadCount = 0;

  function toggleMobileMenu() {
    mobileMenuOpen = !mobileMenuOpen;
  }

  function isActive(path: string) {
    return $location === path;
  }

  function handleLogin() {
    push('/login');
  }

  async function fetchUnreadCount() {
    if (!$isAuthenticated) return;
    
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();
      
      const response = await fetch(`${apiBase}/api/notifications/unread-count`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (response.ok) {
        unreadCount = await response.json();
      }
    } catch (err) {
      console.error('Error fetching unread count:', err);
    }
  }

  // Fetch unread count on mount and every 30 seconds
  onMount(() => {
    if ($isAuthenticated) {
      fetchUnreadCount();
      const interval = setInterval(fetchUnreadCount, 30000);
      return () => clearInterval(interval);
    }
  });

  // Refetch when authentication status changes
  $: if ($isAuthenticated) {
    fetchUnreadCount();
  }
</script>

<nav class="bg-white shadow-md sticky top-0 z-50">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="flex justify-between h-16">
      <div class="flex items-center gap-10">
        <a href="#/" class="flex items-center focus:outline-none focus:ring-2 focus:ring-blue-500 rounded select-none">
          <span class="text-2xl font-bold text-blue-600 tracking-tight">GymHive</span>
        </a>
        <div class="hidden md:flex md:items-center md:space-x-2">
          {#if $isAuthenticated}
            <a href="#/feed" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/feed') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Feed</a>
          {:else}
            <a href="#/" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Home</a>
          {/if}
          <a href="#/gyms" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/gyms') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Find Gyms</a>
          {#if $isAuthenticated}
            <a href="#/memberships" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/memberships') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">My Memberships</a>
            <a href="#/workouts" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/workouts') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">My Workouts</a>
            <a href="#/notifications" class="px-4 py-2 rounded-lg text-sm font-medium transition relative {isActive('/notifications') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">
              Notifications
              {#if unreadCount > 0}
                <span class="absolute -top-1 -right-1 bg-red-500 text-white text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
                  {unreadCount > 99 ? '99+' : unreadCount}
                </span>
              {/if}
            </a>
            <a href="#/profile" class="px-4 py-2 rounded-lg text-sm font-medium transition {isActive('/profile') ? 'bg-blue-600 text-white shadow-sm' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Profile</a>
            {#if $user?.role === 'Admin'}
              <a href="#/admin/gyms" class="px-4 py-2 rounded-lg text-sm font-medium transition {$location?.startsWith('/admin') ? 'bg-purple-600 text-white shadow-sm' : 'text-purple-600 hover:text-purple-900 hover:bg-purple-50'}">
                <span class="flex items-center gap-1">
                  Admin
                  <span class="text-xs bg-purple-100 text-purple-700 px-1.5 py-0.5 rounded">Panel</span>
                </span>
              </a>
            {:else if $user?.role === 'Moderator'}
              <a href="#/moderator/members" class="px-4 py-2 rounded-lg text-sm font-medium transition {$location?.startsWith('/moderator') ? 'bg-teal-600 text-white shadow-sm' : 'text-teal-600 hover:text-teal-900 hover:bg-teal-50'}">
                <span class="flex items-center gap-1">
                  Moderator
                  <span class="text-xs bg-teal-100 text-teal-700 px-1.5 py-0.5 rounded">Panel</span>
                </span>
              </a>
            {/if}
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
          <button on:click={handleLogin} class="px-5 py-2 rounded-lg text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow-sm">Login / Sign Up</button>
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
      {#if $isAuthenticated}
        <a href="#/feed" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/feed') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Feed</a>
      {:else}
        <a href="#/" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Home</a>
      {/if}
      <a href="#/gyms" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/gyms') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Find Gyms</a>
      {#if $isAuthenticated}
        <a href="#/memberships" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/memberships') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">My Memberships</a>
        <a href="#/workouts" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/workouts') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">My Workouts</a>
        <a href="#/notifications" class="block px-4 py-3 rounded-lg text-sm font-medium transition relative {isActive('/notifications') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">
          Notifications
          {#if unreadCount > 0}
            <span class="absolute top-2 right-4 bg-red-500 text-white text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
              {unreadCount > 99 ? '99+' : unreadCount}
            </span>
          {/if}
        </a>
        <a href="#/profile" class="block px-4 py-3 rounded-lg text-sm font-medium transition {isActive('/profile') ? 'bg-blue-600 text-white shadow' : 'text-gray-600 hover:text-gray-900 hover:bg-gray-100'}">Profile</a>
        {#if $user?.role === 'Admin'}
          <div class="bg-purple-50 rounded-lg p-2 space-y-1">
            <div class="text-xs font-semibold text-purple-700 px-2 py-1">Admin Panel</div>
            <a href="#/admin/gyms" class="block px-4 py-2 rounded text-sm font-medium transition {isActive('/admin/gyms') ? 'bg-purple-600 text-white' : 'text-purple-700 hover:bg-purple-100'}">Manage Gyms</a>
            <a href="#/admin/users" class="block px-4 py-2 rounded text-sm font-medium transition {isActive('/admin/users') ? 'bg-purple-600 text-white' : 'text-purple-700 hover:bg-purple-100'}">Manage Users</a>
            <a href="#/admin/groups" class="block px-4 py-2 rounded text-sm font-medium transition {isActive('/admin/groups') ? 'bg-purple-600 text-white' : 'text-purple-700 hover:bg-purple-100'}">Manage Groups</a>
          </div>
        {:else if $user?.role === 'Moderator'}
          <div class="bg-teal-50 rounded-lg p-2 space-y-1">
            <div class="text-xs font-semibold text-teal-700 px-2 py-1">Moderator Panel</div>
            <a href="#/moderator/members" class="block px-4 py-2 rounded text-sm font-medium transition {isActive('/moderator/members') ? 'bg-teal-600 text-white' : 'text-teal-700 hover:bg-teal-100'}">Group Members</a>
            <a href="#/moderator/memberships" class="block px-4 py-2 rounded text-sm font-medium transition {isActive('/moderator/memberships') ? 'bg-teal-600 text-white' : 'text-teal-700 hover:bg-teal-100'}">Memberships</a>
          </div>
        {/if}
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
        <button on:click={handleLogin} class="block w-full text-left px-4 py-3 rounded-lg text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 transition shadow">Login / Sign Up</button>
      {/if}
    </div>
  {/if}
</nav>
