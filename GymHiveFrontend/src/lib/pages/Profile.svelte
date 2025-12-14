<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading, authError } from '../auth';
  import { getUser, logout } from '../auth';
  import { push } from 'svelte-spa-router';
  import { getCurrentUser, updateUser, changePassword, type UserUpdateData, type PasswordChangeData } from '../auth/authService';
  import ProfileHeader from '../components/profile/ProfileHeader.svelte';
  import ProfileInfoCard from '../components/profile/ProfileInfoCard.svelte';
  import EditProfileForm from '../components/profile/EditProfileForm.svelte';
  import PasswordChangeForm from '../components/profile/PasswordChangeForm.svelte';
  import SettingsPanel from '../components/profile/SettingsPanel.svelte';

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
    <ProfileHeader user={$user}>
      <svelte:fragment slot="actions">
        <button 
          class="px-6 py-3 rounded-lg bg-white/10 backdrop-blur-sm hover:bg-white/20 transition-colors font-medium"
          on:click={logout}
        >
          Logout
        </button>
      </svelte:fragment>
    </ProfileHeader>

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
        <ProfileInfoCard user={$user} />

      {:else if activeTab === 'edit'}
        <!-- Edit Profile Form -->
        <EditProfileForm
          bind:name={editForm.name}
          bind:surname={editForm.surname}
          bind:email={editForm.email}
          isSubmitting={isEditSubmitting}
          error={editError}
          success={editSuccess}
          on:submit={handleEditSubmit}
          on:cancel={() => {
            activeTab = 'profile';
            editError = null;
            editSuccess = null;
          }}
        />

      {:else if activeTab === 'password'}
        <!-- Change Password Form -->
        <PasswordChangeForm
          bind:oldPassword={passwordForm.oldPassword}
          bind:newPassword={passwordForm.newPassword}
          bind:confirmPassword={passwordForm.confirmPassword}
          isSubmitting={isPasswordSubmitting}
          error={passwordError}
          success={passwordSuccess}
          on:submit={handlePasswordSubmit}
          on:clear={() => {
            passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
            passwordError = null;
            passwordSuccess = null;
          }}
        />

      {:else if activeTab === 'settings'}
        <!-- Account Settings -->
        <SettingsPanel user={$user} />
      {/if}
    </div>

    {#if $authError}
      <div class="mt-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
        Auth Error: {$authError}
      </div>
    {/if}
  {/if}
</section>
