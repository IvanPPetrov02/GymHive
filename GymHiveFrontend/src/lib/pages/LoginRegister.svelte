<script lang="ts">
  import { authError, isAuthenticated, user, loading } from '../auth/auth';
  import { login, logout } from '../auth';
  import { location } from 'svelte-spa-router';

  function handleAuth() {
    login($location || '#/');
  }
</script>

<!-- Unified Login / Register page using Auth0 Universal Login -->
<div class="min-h-full w-full bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center py-14 px-4">
  <div class="w-full max-w-3xl grid md:grid-cols-2 gap-10 items-stretch">
    <!-- Marketing / Feature panel -->
    <div class="hidden md:flex flex-col justify-between rounded-3xl p-10 bg-gradient-to-br from-blue-600 via-blue-700 to-purple-700 text-white shadow-2xl relative overflow-hidden">
      <div class="absolute inset-0 opacity-20 bg-[radial-gradient(circle_at_30%_30%,white,transparent_60%)]"></div>
      <div class="relative space-y-6">
        <h1 class="text-4xl font-bold leading-tight">Welcome to <span class="text-white">GymHive</span></h1>
        <p class="text-blue-100 text-lg leading-relaxed">Connect, track progress, discover gyms and communities. Secure Auth0 sign in & sign up — no local password storage required.</p>
        <ul class="space-y-3 text-sm text-blue-100/90">
          <li class="flex items-start gap-3"><span class="text-xl">🏋️</span><span>Find and compare gyms</span></li>
            <li class="flex items-start gap-3"><span class="text-xl">👥</span><span>Build your fitness network</span></li>
            <li class="flex items-start gap-3"><span class="text-xl">🔐</span><span>Secure OAuth2 / OIDC authentication</span></li>
        </ul>
      </div>
      <div class="relative">
        <p class="text-xs text-blue-200">By continuing you agree to future Terms & Privacy. Tokens managed securely.</p>
      </div>
    </div>

    <!-- Auth interaction panel -->
    <div class="card-panel p-10 rounded-3xl shadow-xl bg-white/90 backdrop-blur border border-white/40 flex flex-col">
      {#if $authError}
        <div class="mb-6 bg-red-50 border border-red-200 text-red-700 px-5 py-3 rounded-xl text-sm leading-relaxed">
          {$authError}
        </div>
      {/if}
      {#if $loading}
        <div class="flex items-center justify-center flex-1 py-16">
          <div class="animate-pulse text-gray-500">Initializing authentication...</div>
        </div>
      {:else if $isAuthenticated}
        <div class="flex flex-col gap-6 flex-1">
          <div class="flex items-center gap-4">
            {#if $user?.picture}
              <img src={$user.picture} alt="avatar" class="h-16 w-16 rounded-full ring-4 ring-blue-100" referrerpolicy="no-referrer" />
            {/if}
            <div>
              <h2 class="text-2xl font-bold text-gray-800 leading-tight">{$user?.name || $user?.email}</h2>
              <p class="text-gray-500 text-sm">Authenticated via Auth0</p>
            </div>
          </div>
          <div class="bg-blue-50 border border-blue-100 text-blue-700 px-5 py-4 rounded-xl text-sm leading-relaxed">
            You’re signed in. Soon you’ll access protected microservices (Gyms, Workouts, Social Feed). Use the buttons below to explore or sign out.
          </div>
          <div class="mt-auto flex flex-col gap-4">
            <a href="#/" class="btn-primary w-full text-center py-4 rounded-xl shadow">Go to Home</a>
            <a href="#/gyms" class="w-full py-4 rounded-xl font-semibold bg-blue-600 hover:bg-blue-700 text-white transition shadow text-center">Find Gyms</a>
            <button on:click={() => logout()} class="w-full py-4 rounded-xl font-semibold bg-red-600 hover:bg-red-700 text-white transition shadow">Sign Out</button>
          </div>
        </div>
      {:else}
        <div class="flex flex-col gap-8 flex-1">
          <div class="space-y-4">
            <h2 class="text-3xl font-bold tracking-tight text-gray-900">Sign In or Create Free Account</h2>
            <p class="text-gray-600 leading-relaxed text-sm">A single Universal Login handles both. We never store your password locally.</p>
          </div>
          <div class="space-y-3 text-sm text-gray-600">
            <div class="flex items-center gap-3"><span class="text-blue-600">✔</span><span>One-click sign in & sign up</span></div>
            <div class="flex items-center gap-3"><span class="text-blue-600">✔</span><span>Refresh tokens & secure sessions</span></div>
            <div class="flex items-center gap-3"><span class="text-blue-600">✔</span><span>Ready for protected API calls</span></div>
          </div>
          <div class="mt-auto flex flex-col gap-4">
            <button on:click={handleAuth} class="btn-primary w-full py-4 text-base rounded-xl shadow-lg">Continue with Auth0</button>
            <button on:click={handleAuth} class="w-full py-4 rounded-xl font-semibold bg-gray-900 hover:bg-black text-white transition shadow">Create Free Account</button>
            <a href="#/" class="text-center text-sm font-medium text-blue-600 hover:text-blue-500">Return to Home</a>
          </div>
        </div>
      {/if}
    </div>
  </div>
</div>

