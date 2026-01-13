<script lang="ts">
  import { onMount } from 'svelte';
  import { authError, isAuthenticated, user, isLoading } from '../auth/auth';
  import { login, register, logout } from '../auth';
  import { push, replace } from 'svelte-spa-router';

  let mode: 'login' | 'register' = 'login';
  let email = '';
  let password = '';
  let name = '';
  let surname = '';
  let submitting = false;
  let localError = '';

  onMount(() => {
    // Logged-in users should not use Login/Register
    if ($isAuthenticated) {
      replace('/feed');
    }
  });

  async function handleLogin() {
    if (!email || !password) {
      localError = 'Please fill in all fields';
      return;
    }

    submitting = true;
    localError = '';

    const success = await login({ email, password });

    submitting = false;

    if (success) {
      push('/feed');
    } else {
      localError = $authError || 'Login failed';
    }
  }

  async function handleRegister() {
    if (!email || !password || !name || !surname) {
      localError = 'Please fill in all required fields';
      return;
    }

    submitting = true;
    localError = '';

    const success = await register({ email, password, name, surname });

    submitting = false;

    if (success) {
      push('/feed');
    } else {
      localError = $authError || 'Registration failed';
    }
  }

  function toggleMode() {
    mode = mode === 'login' ? 'register' : 'login';
    localError = '';
    password = '';
    name = '';
    surname = '';
  }
</script>

<div class="min-h-full w-full bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center py-14 px-4">
  <div class="w-full max-w-3xl grid md:grid-cols-2 gap-10 items-stretch">
    <!-- Marketing / Feature panel -->
    <div class="hidden md:flex flex-col justify-between rounded-3xl p-10 bg-gradient-to-br from-blue-600 via-blue-700 to-purple-700 text-white shadow-2xl relative overflow-hidden">
      <div class="absolute inset-0 opacity-20 bg-[radial-gradient(circle_at_30%_30%,white,transparent_60%)]"></div>
      <div class="relative space-y-6">
        <h1 class="text-4xl font-bold leading-tight">Welcome to <span class="text-white">GymHive</span></h1>
        <p class="text-blue-100 text-lg leading-relaxed">Connect, track progress, discover gyms and communities. Your fitness journey starts here.</p>
        <ul class="space-y-3 text-sm text-blue-100/90">
          <li class="flex items-start gap-3"><span class="text-xl">🏋️</span><span>Find and compare gyms</span></li>
          <li class="flex items-start gap-3"><span class="text-xl">👥</span><span>Build your fitness network</span></li>
          <li class="flex items-start gap-3"><span class="text-xl">📊</span><span>Track your progress</span></li>
        </ul>
      </div>
      <div class="relative">
        <p class="text-xs text-blue-200">
          By continuing you agree to our
          <a href="#/terms" class="underline hover:text-white">Terms</a>
          &
          <a href="#/privacy" class="underline hover:text-white">Privacy Policy</a>.
        </p>
      </div>
    </div>

    <!-- Auth interaction panel -->
    <div class="card-panel p-10 rounded-3xl shadow-xl bg-white/90 backdrop-blur border border-white/40 flex flex-col">
      {#if (localError || $authError)}
        <div class="mb-6 bg-red-50 border border-red-200 text-red-700 px-5 py-3 rounded-xl text-sm leading-relaxed">
          {localError || $authError}
        </div>
      {/if}

      {#if $isLoading}
        <div class="flex items-center justify-center flex-1 py-16">
          <div class="animate-pulse text-gray-500">Initializing...</div>
        </div>
      {:else if $isAuthenticated}
        <div class="flex flex-col gap-6 flex-1">
          <div class="flex items-center gap-4">
            <div class="h-16 w-16 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-2xl font-bold">
              {($user?.name?.[0] || '') + ($user?.surname?.[0] || '')}
            </div>
            <div>
              <h2 class="text-2xl font-bold text-gray-800 leading-tight">
                {$user?.name} {$user?.surname}
              </h2>
              <p class="text-gray-500 text-sm">{$user?.email}</p>
            </div>
          </div>
          <div class="bg-blue-50 border border-blue-100 text-blue-700 px-5 py-4 rounded-xl text-sm leading-relaxed">
            You're signed in! Explore gyms, track workouts, and connect with the fitness community.
          </div>
          <div class="mt-auto flex flex-col gap-4">
            <a href="#/feed" class="btn-primary w-full text-center py-4 rounded-xl shadow">Go to Feed</a>
            <a href="#/gyms" class="w-full py-4 rounded-xl font-semibold bg-blue-600 hover:bg-blue-700 text-white transition shadow text-center">Find Gyms</a>
            <button on:click={() => logout()} class="w-full py-4 rounded-xl font-semibold bg-red-600 hover:bg-red-700 text-white transition shadow">Sign Out</button>
          </div>
        </div>
      {:else}
        <div class="flex flex-col gap-6 flex-1">
          <div class="space-y-4">
            <h2 class="text-3xl font-bold tracking-tight text-gray-900">
              {mode === 'login' ? 'Sign In' : 'Create Account'}
            </h2>
            <p class="text-gray-600 leading-relaxed text-sm">
              {mode === 'login' ? 'Welcome back! Enter your credentials to continue.' : 'Join GymHive and start your fitness journey today.'}
            </p>
          </div>

          <form on:submit|preventDefault={mode === 'login' ? handleLogin : handleRegister} class="space-y-4">
            {#if mode === 'register'}
              <div>
                <label for="name" class="block text-sm font-medium text-gray-700 mb-1">First Name *</label>
                <input
                  id="name"
                  type="text"
                  bind:value={name}
                  required
                  placeholder="John"
                  class="w-full px-4 py-3 rounded-lg border border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition"
                />
              </div>
              
              <div>
                <label for="surname" class="block text-sm font-medium text-gray-700 mb-1">Last Name *</label>
                <input
                  id="surname"
                  type="text"
                  bind:value={surname}
                  required
                  placeholder="Doe"
                  class="w-full px-4 py-3 rounded-lg border border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition"
                />
              </div>
            {/if}

            <div>
              <label for="email" class="block text-sm font-medium text-gray-700 mb-1">Email</label>
              <input
                id="email"
                type="email"
                bind:value={email}
                required
                placeholder="you@example.com"
                class="w-full px-4 py-3 rounded-lg border border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition"
              />
            </div>

            <div>
              <label for="password" class="block text-sm font-medium text-gray-700 mb-1">Password</label>
              <input
                id="password"
                type="password"
                bind:value={password}
                required
                placeholder="••••••••"
                class="w-full px-4 py-3 rounded-lg border border-gray-300 focus:border-blue-500 focus:ring-2 focus:ring-blue-200 outline-none transition"
              />
            </div>

            <button
              type="submit"
              disabled={submitting}
              class="w-full py-4 rounded-xl font-semibold bg-blue-600 hover:bg-blue-700 text-white transition shadow disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {submitting ? 'Please wait...' : mode === 'login' ? 'Sign In' : 'Create Account'}
            </button>
          </form>

          <div class="relative">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-gray-300"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-2 bg-white text-gray-500">or</span>
            </div>
          </div>

          <button
            on:click={toggleMode}
            class="w-full py-3 rounded-xl font-medium bg-gray-100 hover:bg-gray-200 text-gray-700 transition"
          >
            {mode === 'login' ? "Don't have an account? Sign up" : 'Already have an account? Sign in'}
          </button>

          <a href="#/" class="text-center text-sm font-medium text-blue-600 hover:text-blue-500">Return to Home</a>
        </div>
      {/if}
    </div>
  </div>
</div>
