<script lang="ts">
  import { push, location } from 'svelte-spa-router';
  import { onMount } from 'svelte';

  // Auth form shared state
  let mode: 'login' | 'register' = 'login';
  let email = '';
  let password = '';
  let firstName = '';
  let lastName = '';
  let confirmPassword = '';
  let acceptTerms = false;

  let loading = false;
  let error = '';
  let successMsg = '';

  onMount(() => {
    if ($location === '/register') mode = 'register';
  });
  $: if ($location === '/register' && mode !== 'register') mode = 'register';
  $: if ($location === '/login' && mode !== 'login') mode = 'login';

  function switchMode(next: 'login' | 'register') {
    if (mode !== next) {
      mode = next;
      error = '';
      successMsg = '';
      // Clear sensitive fields when switching
      password = '';
      confirmPassword = '';
    }
  }

  function basicEmailValid(v: string) {
    return /.+@.+\..+/.test(v);
  }

  async function handleSubmit() {
    error = '';
    successMsg = '';
    loading = true;
    try {
      // Shared validation
      if (!email || !password) {
        error = 'Email and password are required.';
        return;
      }
      if (!basicEmailValid(email)) {
        error = 'Please enter a valid email address.';
        return;
      }

      if (mode === 'register') {
        if (!firstName || !lastName) { error = 'First and last name are required.'; return; }
        if (password.length < 8) { error = 'Password must be at least 8 characters.'; return; }
        if (password !== confirmPassword) { error = 'Passwords do not match.'; return; }
        if (!acceptTerms) { error = 'You must accept the terms to create an account.'; return; }
        // TODO: Call Identity microservice register endpoint
        // const resp = await fetch('/api/auth/register', { ... })
        console.log('Register attempt', { firstName, lastName, email });
        successMsg = 'Account created! You can log in now.';
        // Auto-switch to login mode after short delay (optional)
        setTimeout(() => switchMode('login'), 1200);
      } else {
        // TODO: Call Identity microservice login endpoint
        console.log('Login attempt', { email });
        // Simulate success
        // localStorage.setItem('token', 'fake-token');
        successMsg = 'Login successful! Redirecting...';
        setTimeout(() => {
          // push('/'); // Uncomment when routing to dashboard/home after auth
        }, 800);
      }
    } catch (e) {
      error = 'Unexpected error. Please try again.';
      console.error(e);
    } finally {
      loading = false;
    }
  }
</script>

<div class="min-h-screen w-full bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
  <div class="max-w-xl w-full space-y-10">
    <!-- Mode Toggle Header -->
    <div class="text-center space-y-4">
      <div class="inline-flex bg-gray-100 rounded-xl p-1 shadow-inner">
        <button on:click={() => switchMode('login')} class="px-6 py-2 rounded-lg text-sm font-semibold transition {mode === 'login' ? 'bg-white shadow text-blue-700' : 'text-gray-600 hover:text-gray-900'}">Login</button>
        <button on:click={() => switchMode('register')} class="px-6 py-2 rounded-lg text-sm font-semibold transition {mode === 'register' ? 'bg-white shadow text-blue-700' : 'text-gray-600 hover:text-gray-900'}">Register</button>
      </div>
      <h1 class="text-3xl md:text-4xl font-bold tracking-tight text-gray-900">
        {mode === 'login' ? 'Welcome Back' : 'Create Your Account'}
      </h1>
      <p class="text-gray-600 text-sm md:text-base">
        {mode === 'login' ? 'Access your GymHive account' : 'Join GymHive and start your fitness journey'}
      </p>
    </div>

    <div class="card-panel p-10 shadow-xl">
      <form on:submit|preventDefault={handleSubmit} class="space-y-7">
        {#if error}
          <div class="error-box">{error}</div>
        {/if}
        {#if successMsg}
          <div class="bg-green-100 text-green-800 px-4 py-3 rounded-lg text-sm font-medium">{successMsg}</div>
        {/if}

        {#if mode === 'register'}
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label for="firstName" class="block text-sm font-medium text-gray-700 mb-2">First Name</label>
              <input id="firstName" type="text" bind:value={firstName} class="no-border-input w-full" placeholder="John" />
            </div>
            <div>
              <label for="lastName" class="block text-sm font-medium text-gray-700 mb-2">Last Name</label>
              <input id="lastName" type="text" bind:value={lastName} class="no-border-input w-full" placeholder="Doe" />
            </div>
          </div>
        {/if}

        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 mb-2">Email Address</label>
          <input id="email" type="email" bind:value={email} class="no-border-input w-full" placeholder="you@example.com" />
        </div>

        <div class="grid gap-4 {mode === 'register' ? 'md:grid-cols-2' : ''}">
          <div>
            <label for="password" class="block text-sm font-medium text-gray-700 mb-2">Password</label>
            <input id="password" type="password" bind:value={password} class="no-border-input w-full" placeholder="••••••••" />
          </div>
          {#if mode === 'register'}
            <div>
              <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-2">Confirm</label>
              <input id="confirmPassword" type="password" bind:value={confirmPassword} class="no-border-input w-full" placeholder="Repeat" />
            </div>
          {/if}
        </div>

        {#if mode === 'register'}
          <label class="flex items-start gap-2 text-sm text-gray-700 cursor-pointer select-none">
            <input type="checkbox" bind:checked={acceptTerms} class="mt-1 h-4 w-4 text-blue-600 rounded focus:ring-blue-500 focus:ring-2" />
            <span>I agree to the <a href="#/terms" class="text-blue-600 hover:text-blue-500 font-medium">Terms & Privacy</a></span>
          </label>
        {:else}
          <div class="flex items-center justify-between text-sm">
            <label class="flex items-center gap-2 cursor-pointer select-none text-gray-700">
              <input type="checkbox" class="h-4 w-4 text-blue-600 rounded focus:ring-blue-500 focus:ring-2" />
              Remember me
            </label>
            <a href="#/forgot-password" class="text-blue-600 hover:text-blue-500 font-medium">Forgot password?</a>
          </div>
        {/if}

        <button type="submit" disabled={loading} class="btn-primary w-full py-4 text-base rounded-xl shadow-lg">
          {loading ? (mode === 'login' ? 'Signing in...' : 'Creating account...') : (mode === 'login' ? 'Sign In' : 'Create Account')}
        </button>

        <div class="text-center text-sm text-gray-600">
          {#if mode === 'login'}
            <span>Need an account? <button type="button" class="text-blue-600 font-semibold hover:text-blue-500" on:click={() => switchMode('register')}>Register</button></span>
          {:else}
            <span>Already have an account? <button type="button" class="text-blue-600 font-semibold hover:text-blue-500" on:click={() => switchMode('login')}>Login</button></span>
          {/if}
        </div>
      </form>
    </div>
  </div>
</div>
