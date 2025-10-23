<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading, authError } from '../auth';
  import { getUser, logout } from '../auth';
  import { push } from 'svelte-spa-router';
  import { getCurrentUser, updateUser, changePassword, type UserUpdateData, type PasswordChangeData } from '../auth/authService';

  let activeTab: 'profile' | 'edit' | 'password' | 'settings' = 'profile';
  
  // Edit profile form
  let editForm = {
    name: '',
    surname: '',
    email: ''
  };
  let editError: string | null = null;
  let editSuccess: string | null = null;
  let isEditSubmitting = false;

  // Password change form
  let passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  let passwordError: string | null = null;
  let passwordSuccess: string | null = null;
  let isPasswordSubmitting = false;

  onMount(async () => {
    if ($isAuthenticated) {
      await loadUserData();
    }
  });

  async function loadUserData() {
    try {
      await getUser();
      if ($user) {
        editForm = {
          name: $user.name || '',
          surname: $user.surname || '',
          email: $user.email || ''
        };
      }
    } catch (e: any) {
      console.error('Failed to load user data:', e);
    }
  }

  async function handleEditSubmit() {
    editError = null;
    editSuccess = null;

    if (!editForm.name.trim() || !editForm.surname.trim() || !editForm.email.trim()) {
      editError = 'All fields are required';
      return;
    }

    if (!$user?.uuid) {
      editError = 'User ID not found';
      return;
    }

    isEditSubmitting = true;
    try {
      const updateData: UserUpdateData = {
        name: editForm.name,
        surname: editForm.surname,
        email: editForm.email
      };
      
      await updateUser($user.uuid, updateData);
      await loadUserData();
      editSuccess = 'Profile updated successfully!';
      
      setTimeout(() => {
        editSuccess = null;
        activeTab = 'profile';
      }, 2000);
    } catch (e: any) {
      editError = e?.body?.message || e?.message || 'Failed to update profile';
    } finally {
      isEditSubmitting = false;
    }
  }

  async function handlePasswordSubmit() {
    passwordError = null;
    passwordSuccess = null;

    if (!passwordForm.oldPassword || !passwordForm.newPassword || !passwordForm.confirmPassword) {
      passwordError = 'All fields are required';
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      passwordError = 'New passwords do not match';
      return;
    }

    if (passwordForm.newPassword.length < 6) {
      passwordError = 'New password must be at least 6 characters';
      return;
    }

    if (!$user?.uuid) {
      passwordError = 'User ID not found';
      return;
    }

    isPasswordSubmitting = true;
    try {
      const changeData: PasswordChangeData = {
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword
      };
      
      await changePassword($user.uuid, changeData);
      passwordSuccess = 'Password changed successfully!';
      passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      
      setTimeout(() => {
        passwordSuccess = null;
      }, 3000);
    } catch (e: any) {
      passwordError = e?.body?.message || e?.message || 'Failed to change password';
    } finally {
      isPasswordSubmitting = false;
    }
  }

  function handleLogin() {
    push('/login');
  }

  function formatDate(dateString: string | undefined) {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
</script>

<section class="max-w-5xl mx-auto px-4 py-10">
  {#if $isLoading}
    <div class="flex items-center justify-center min-h-screen">
      <div class="text-gray-500 animate-pulse">Loading...</div>
    </div>
  {:else if !$isAuthenticated}
    <div class="flex flex-col items-center justify-center min-h-screen space-y-4">
      <div class="text-center space-y-2">
        <h1 class="text-3xl font-bold text-gray-800">Access Denied</h1>
        <p class="text-gray-600">You need to login to view your profile.</p>
      </div>
      <button 
        class="px-6 py-3 rounded-lg bg-blue-600 text-white font-semibold hover:bg-blue-700 transition-colors" 
        on:click={handleLogin}
      >
        Login Now
      </button>
    </div>
  {:else}
    <!-- Header -->
    <div class="bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl p-8 mb-8 text-white shadow-xl">
      <div class="flex items-center gap-6">
        <div class="h-24 w-24 rounded-full bg-white/20 backdrop-blur-sm flex items-center justify-center text-4xl font-bold">
          {($user?.name?.[0] || '') + ($user?.surname?.[0] || '')}
        </div>
        <div class="flex-1">
          <h1 class="text-3xl font-bold mb-1">{$user?.name} {$user?.surname}</h1>
          <p class="text-blue-100 text-sm">{$user?.email}</p>
          <div class="flex items-center gap-4 mt-3 text-sm">
            <span class="flex items-center gap-1">
              <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd"/>
              </svg>
              Joined {formatDate($user?.createdAt)}
            </span>
            <span class="flex items-center gap-1 px-3 py-1 rounded-full bg-white/20 backdrop-blur-sm">
              <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
              </svg>
              {$user?.role || 'User'}
            </span>
          </div>
        </div>
        <button 
          class="px-6 py-3 rounded-lg bg-white/10 backdrop-blur-sm hover:bg-white/20 transition-colors font-medium"
          on:click={logout}
        >
          Logout
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden mb-6">
      <div class="flex border-b border-gray-200">
        <button 
          class="flex-1 px-6 py-4 font-medium transition-colors relative {activeTab === 'profile' ? 'text-blue-600 bg-blue-50' : 'text-gray-600 hover:bg-gray-50'}"
          on:click={() => activeTab = 'profile'}
        >
          <span class="flex items-center justify-center gap-2">
            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
            </svg>
            Profile
          </span>
          {#if activeTab === 'profile'}
            <div class="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600"></div>
          {/if}
        </button>
        <button 
          class="flex-1 px-6 py-4 font-medium transition-colors relative {activeTab === 'edit' ? 'text-blue-600 bg-blue-50' : 'text-gray-600 hover:bg-gray-50'}"
          on:click={() => activeTab = 'edit'}
        >
          <span class="flex items-center justify-center gap-2">
            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z"/>
            </svg>
            Edit Profile
          </span>
          {#if activeTab === 'edit'}
            <div class="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600"></div>
          {/if}
        </button>
        <button 
          class="flex-1 px-6 py-4 font-medium transition-colors relative {activeTab === 'password' ? 'text-blue-600 bg-blue-50' : 'text-gray-600 hover:bg-gray-50'}"
          on:click={() => activeTab = 'password'}
        >
          <span class="flex items-center justify-center gap-2">
            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
            </svg>
            Password
          </span>
          {#if activeTab === 'password'}
            <div class="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600"></div>
          {/if}
        </button>
        <button 
          class="flex-1 px-6 py-4 font-medium transition-colors relative {activeTab === 'settings' ? 'text-blue-600 bg-blue-50' : 'text-gray-600 hover:bg-gray-50'}"
          on:click={() => activeTab = 'settings'}
        >
          <span class="flex items-center justify-center gap-2">
            <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd"/>
            </svg>
            Settings
          </span>
          {#if activeTab === 'settings'}
            <div class="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600"></div>
          {/if}
        </button>
      </div>
    </div>

    <!-- Tab Content -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-8">
      {#if activeTab === 'profile'}
        <!-- Profile View -->
        <div class="space-y-6">
          <h2 class="text-2xl font-bold text-gray-800 mb-6">Personal Information</h2>
          
          <div class="grid md:grid-cols-2 gap-6">
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">First Name</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p class="text-gray-800 font-medium">{$user?.name || 'N/A'}</p>
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">Last Name</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p class="text-gray-800 font-medium">{$user?.surname || 'N/A'}</p>
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">Email Address</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p class="text-gray-800 font-medium">{$user?.email || 'N/A'}</p>
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">Account Status</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <span class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium {$user?.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
                  <span class="w-2 h-2 rounded-full {$user?.isActive ? 'bg-green-600' : 'bg-red-600'}"></span>
                  {$user?.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">User ID</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p class="text-gray-600 font-mono text-xs break-all">{$user?.uuid || 'N/A'}</p>
              </div>
            </div>
            
            <div class="space-y-2">
              <label class="text-sm font-medium text-gray-500">Member Since</label>
              <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <p class="text-gray-800 font-medium">{formatDate($user?.createdAt)}</p>
              </div>
            </div>
          </div>
        </div>

      {:else if activeTab === 'edit'}
        <!-- Edit Profile Form -->
        <div class="max-w-2xl">
          <h2 class="text-2xl font-bold text-gray-800 mb-6">Edit Profile</h2>
          
          {#if editSuccess}
            <div class="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 flex items-center gap-2">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
              </svg>
              {editSuccess}
            </div>
          {/if}
          
          {#if editError}
            <div class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 flex items-center gap-2">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
              </svg>
              {editError}
            </div>
          {/if}
          
          <form on:submit|preventDefault={handleEditSubmit} class="space-y-5">
            <div class="space-y-2">
              <label for="edit-name" class="block text-sm font-medium text-gray-700">
                First Name <span class="text-red-500">*</span>
              </label>
              <input 
                id="edit-name"
                type="text" 
                bind:value={editForm.name}
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
                bind:value={editForm.surname}
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
                bind:value={editForm.email}
                class="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-shadow"
                placeholder="Enter your email"
                required
              />
            </div>
            
            <div class="flex gap-3 pt-4">
              <button 
                type="submit"
                disabled={isEditSubmitting}
                class="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
              >
                {isEditSubmitting ? 'Saving...' : 'Save Changes'}
              </button>
              <button 
                type="button"
                on:click={() => {
                  activeTab = 'profile';
                  editError = null;
                  editSuccess = null;
                }}
                class="px-6 py-3 bg-gray-200 text-gray-700 rounded-lg font-semibold hover:bg-gray-300 transition-colors"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>

      {:else if activeTab === 'password'}
        <!-- Change Password Form -->
        <div class="max-w-2xl">
          <h2 class="text-2xl font-bold text-gray-800 mb-6">Change Password</h2>
          
          {#if passwordSuccess}
            <div class="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700 flex items-center gap-2">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
              </svg>
              {passwordSuccess}
            </div>
          {/if}
          
          {#if passwordError}
            <div class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 flex items-center gap-2">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
              </svg>
              {passwordError}
            </div>
          {/if}
          
          <form on:submit|preventDefault={handlePasswordSubmit} class="space-y-5">
            <div class="space-y-2">
              <label for="old-password" class="block text-sm font-medium text-gray-700">
                Current Password <span class="text-red-500">*</span>
              </label>
              <input 
                id="old-password"
                type="password" 
                bind:value={passwordForm.oldPassword}
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
                bind:value={passwordForm.newPassword}
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
                bind:value={passwordForm.confirmPassword}
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
                disabled={isPasswordSubmitting}
                class="flex-1 px-6 py-3 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
              >
                {isPasswordSubmitting ? 'Changing...' : 'Change Password'}
              </button>
              <button 
                type="button"
                on:click={() => {
                  passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
                  passwordError = null;
                  passwordSuccess = null;
                }}
                class="px-6 py-3 bg-gray-200 text-gray-700 rounded-lg font-semibold hover:bg-gray-300 transition-colors"
              >
                Clear
              </button>
            </div>
          </form>
        </div>

      {:else if activeTab === 'settings'}
        <!-- Account Settings -->
        <div class="max-w-2xl space-y-8">
          <div>
            <h2 class="text-2xl font-bold text-gray-800 mb-2">Account Settings</h2>
            <p class="text-gray-600">Manage your account preferences and settings</p>
          </div>
          
          <div class="space-y-4">
            <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
              <div class="flex items-start justify-between">
                <div class="flex-1">
                  <h3 class="font-semibold text-gray-800 mb-1">Account Status</h3>
                  <p class="text-sm text-gray-600">Your account is currently {$user?.isActive ? 'active' : 'inactive'}</p>
                </div>
                <span class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium {$user?.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
                  <span class="w-2 h-2 rounded-full {$user?.isActive ? 'bg-green-600' : 'bg-red-600'}"></span>
                  {$user?.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
            </div>
            
            <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
              <div class="flex items-start justify-between">
                <div class="flex-1">
                  <h3 class="font-semibold text-gray-800 mb-1">Email Notifications</h3>
                  <p class="text-sm text-gray-600">Receive updates about your account and activities</p>
                </div>
                <label class="relative inline-flex items-center cursor-pointer">
                  <input type="checkbox" checked class="sr-only peer">
                  <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>
            </div>
            
            <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
              <div class="flex items-start justify-between">
                <div class="flex-1">
                  <h3 class="font-semibold text-gray-800 mb-1">Two-Factor Authentication</h3>
                  <p class="text-sm text-gray-600">Add an extra layer of security to your account</p>
                </div>
                <button class="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors">
                  Enable
                </button>
              </div>
            </div>
            
            <div class="p-6 border border-red-200 rounded-lg bg-red-50">
              <h3 class="font-semibold text-red-800 mb-2">Danger Zone</h3>
              <p class="text-sm text-red-600 mb-4">Once you delete your account, there is no going back. Please be certain.</p>
              <button class="px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 transition-colors">
                Delete Account
              </button>
            </div>
          </div>
        </div>
      {/if}
    </div>

    {#if $authError}
      <div class="mt-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
        Auth Error: {$authError}
      </div>
    {/if}
  {/if}
</section>
