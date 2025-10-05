<script lang="ts">
  import { push } from 'svelte-spa-router';

  let firstName = '';
  let lastName = '';
  let email = '';
  let password = '';
  let confirmPassword = '';
  let error = '';
  let loading = false;

  async function handleRegister() {
    error = '';
    loading = true;

    try {
      // Validate inputs
      if (!firstName || !lastName || !email || !password || !confirmPassword) {
        error = 'Please fill in all fields';
        return;
      }

      if (password !== confirmPassword) {
        error = 'Passwords do not match';
        return;
      }

      if (password.length < 8) {
        error = 'Password must be at least 8 characters long';
        return;
      }

      // TODO: Connect to IdentityService API
      // const response = await fetch('http://localhost:5001/api/auth/register', {
      //   method: 'POST',
      //   headers: { 'Content-Type': 'application/json' },
      //   body: JSON.stringify({ firstName, lastName, email, password })
      // });

      // Simulate API call
      console.log('Register attempt:', { firstName, lastName, email, password });

      // TODO: Handle successful registration (save token, redirect)
      // if (response.ok) {
      //   const data = await response.json();
      //   localStorage.setItem('token', data.token);
      //   push('/');
      // }
    } catch (err) {
      error = 'An error occurred. Please try again.';
      console.error(err);
    } finally {
      loading = false;
    }
  }
</script>

<div class="min-h-screen w-full bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
  <div class="max-w-md w-full space-y-10">
    <div>
      <h2 class="mt-2 text-center text-4xl font-bold text-gray-900">
        Create Your Account
      </h2>
      <p class="mt-2 text-center text-gray-600">
        Join GymHive and start your fitness journey
      </p>
    </div>

    <div class="card-panel p-10 shadow-xl">
      <form on:submit|preventDefault={handleRegister} class="space-y-7">
        {#if error}
          <div class="error-box">{error}</div>
        {/if}

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label for="firstName" class="block text-sm font-medium text-gray-700 mb-2">
              First Name
            </label>
            <input
              id="firstName"
              type="text"
              bind:value={firstName}
              required
              class="no-border-input w-full"
              placeholder="John"
            />
          </div>

          <div>
            <label for="lastName" class="block text-sm font-medium text-gray-700 mb-2">
              Last Name
            </label>
            <input
              id="lastName"
              type="text"
              bind:value={lastName}
              required
              class="no-border-input w-full"
              placeholder="Doe"
            />
          </div>
        </div>

        <div>
          <label for="email" class="block text-sm font-medium text-gray-700 mb-2">
            Email Address
          </label>
          <input
            id="email"
            type="email"
            bind:value={email}
            required
            class="no-border-input w-full"
            placeholder="you@example.com"
          />
        </div>

        <div>
          <label for="password" class="block text-sm font-medium text-gray-700 mb-2">
            Password
          </label>
          <input
            id="password"
            type="password"
            bind:value={password}
            required
            class="no-border-input w-full"
            placeholder="At least 8 characters"
          />
        </div>

        <div>
          <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-2">
            Confirm Password
          </label>
          <input
            id="confirmPassword"
            type="password"
            bind:value={confirmPassword}
            required
            class="no-border-input w-full"
            placeholder="Re-enter password"
          />
        </div>

        <label class="flex items-start gap-2 text-sm text-gray-700 cursor-pointer select-none">
          <input
            id="terms"
            type="checkbox"
            required
            class="mt-1 h-4 w-4 text-blue-600 rounded focus:ring-blue-500 focus:ring-2"
          />
          <span>
            I agree to the
            <a href="#/terms" class="text-blue-600 hover:text-blue-500 font-medium">
              Terms and Conditions
            </a>
          </span>
        </label>

        <button
          type="submit"
          disabled={loading}
          class="btn-primary w-full py-4 text-base rounded-xl shadow-lg"
        >
          {loading ? 'Creating account...' : 'Create Account'}
        </button>

        <p class="text-center text-sm text-gray-600">
          Already have an account?
          <a href="#/login" class="text-blue-600 font-semibold hover:text-blue-500">
            Sign in
          </a>
        </p>
      </form>
    </div>
  </div>
</div>
