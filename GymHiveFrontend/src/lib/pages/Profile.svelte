<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading, authError, login, logout } from '../auth';
  import { getUser, getAccessToken } from '../auth'; // fixed relative path
  import { getJson } from '../api'; // fixed relative path

  let apiProfile: any = null;
  let apiError: string | null = null;
  let loadingApi = false;

  async function load() {
    apiError = null; apiProfile = null; loadingApi = true;
    try {
      // Example protected call to backend (expects /api/me to return JSON)
      apiProfile = await getJson('/api/me');
    } catch (e: any) {
      apiError = e?.body?.message || e?.message || 'Failed to load /api/me';
    } finally {
      loadingApi = false;
    }
  }

  onMount(async () => {
    if ($isAuthenticated) {
      await getUser();
      await load();
    }
  });

  async function refreshToken() {
    const t = await getAccessToken();
    if (t) {
      await load();
    }
  }
</script>

<section class="max-w-3xl mx-auto px-4 py-10 space-y-8">
  <h1 class="text-3xl font-bold tracking-tight text-gray-800">Profile</h1>

  {#if $isLoading}
    <div class="text-gray-500 animate-pulse">Loading authentication state...</div>
  {:else if !$isAuthenticated}
    <div class="space-y-4">
      <p class="text-gray-700">You need to login to view your profile.</p>
      <button class="px-5 py-2 rounded-lg bg-blue-600 text-white font-semibold hover:bg-blue-700" on:click={() => login('#/profile')}>Login</button>
    </div>
  {:else}
    <div class="grid md:grid-cols-3 gap-8 items-start">
      <div class="md:col-span-1 space-y-4">
        {#if $user?.picture}
          <img src={$user.picture} alt="avatar" class="h-32 w-32 rounded-full ring-4 ring-blue-200 shadow" referrerpolicy="no-referrer" />
        {/if}
        <div class="space-y-1">
          <div class="font-semibold text-lg">{$user?.name || $user?.email}</div>
          {#if $user?.email}
            <div class="text-sm text-gray-600">{$user.email}</div>
          {/if}
          {#if $user?.updated_at}
            <div class="text-xs text-gray-400">Updated: {new Date($user.updated_at).toLocaleString()}</div>
          {/if}
        </div>
        <div class="flex flex-col gap-2 pt-4">
          <button class="px-4 py-2 rounded bg-blue-600 text-white text-sm font-semibold hover:bg-blue-700" on:click={refreshToken}>Refresh Token & Reload</button>
          <button class="px-4 py-2 rounded bg-red-600 text-white text-sm font-semibold hover:bg-red-700" on:click={logout}>Logout</button>
        </div>
      </div>

      <div class="md:col-span-2 space-y-6">
        <div>
          <h2 class="font-semibold text-gray-700 mb-2">Auth0 User (ID Token claims)</h2>
          <pre class="bg-gray-900 text-gray-100 rounded p-4 overflow-auto text-xs max-h-80">{JSON.stringify($user, null, 2)}</pre>
        </div>

        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <h2 class="font-semibold text-gray-700">Backend /api/me response</h2>
            <button class="px-3 py-1 rounded bg-gray-200 text-xs font-medium hover:bg-gray-300" on:click={load} disabled={loadingApi}>{loadingApi ? 'Loading...' : 'Reload'}</button>
          </div>
          {#if apiError}
            <div class="text-sm text-red-600 bg-red-50 border border-red-200 rounded p-3">{apiError}</div>
          {:else if loadingApi}
            <div class="text-gray-500 text-sm">Loading...</div>
          {:else if apiProfile}
            <pre class="bg-gray-900 text-gray-100 rounded p-4 overflow-auto text-xs max-h-80">{JSON.stringify(apiProfile, null, 2)}</pre>
          {:else}
            <div class="text-gray-500 text-sm">No data loaded yet.</div>
          {/if}
        </div>

        {#if $authError}
          <div class="text-sm text-red-600 bg-red-50 border border-red-200 rounded p-3">Auth Error: {$authError}</div>
        {/if}
      </div>
    </div>
  {/if}
</section>

<style>
  pre::-webkit-scrollbar{ width: 10px; }
  pre::-webkit-scrollbar-track{ background: #1f293772; }
  pre::-webkit-scrollbar-thumb{ background:#4b5563; border-radius:4px; }
  pre::-webkit-scrollbar-thumb:hover{ background:#6b7280; }
</style>
