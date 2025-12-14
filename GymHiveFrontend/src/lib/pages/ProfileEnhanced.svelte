<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../auth';
  import { requireAuth } from '../auth/auth';
  import { membershipsApi, type Membership } from '../services/memberships';
  import { showToast } from '../components/ui/Toast.svelte';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';
  import Modal from '../components/ui/Modal.svelte';
  import ConfirmDialog from '../components/ui/ConfirmDialog.svelte';
  import { updateUser, changePassword, type UserUpdateData, type PasswordChangeData } from '../auth/authService';

  let activeTab: 'profile' | 'edit' | 'password' | 'memberships' = 'profile';
  
  // Edit profile form
  let editForm = {
    name: '',
    surname: '',
    email: ''
  };
  let editError: string | null = null;
  let isEditSubmitting = false;

  // Password change form
  let passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  let passwordError: string | null = null;
  let isPasswordSubmitting = false;

  // Memberships
  let memberships: Membership[] = [];
  let loadingMemberships = false;
  let membershipError: string | null = null;
  
  // Cancel membership
  let cancelMembershipId: number | null = null;
  let isCancelling = false;

  onMount(() => {
    requireAuth('#/profile');
    if ($isAuthenticated && $user) {
      loadUserData();
      loadMemberships();
    }
  });

  function loadUserData() {
    if ($user) {
      editForm = {
        name: $user.name || '',
        surname: $user.surname || '',
        email: $user.email || ''
      };
    }
  }

  async function loadMemberships() {
    if (!$user) return;
    
    loadingMemberships = true;
    membershipError = null;
    try {
      memberships = await membershipsApi.getMyMemberships();
    } catch (e: any) {
      console.error('Failed to load memberships:', e);
      membershipError = e.message || 'Failed to load memberships';
    } finally {
      loadingMemberships = false;
    }
  }

  async function handleEditSubmit() {
    editError = null;
    
    if (!editForm.name.trim() || !editForm.surname.trim() || !editForm.email.trim()) {
      editError = 'All fields are required';
      return;
    }

    if (!$user) return;

    isEditSubmitting = true;
    try {
      const updateData: UserUpdateData = {
        name: editForm.name.trim(),
        surname: editForm.surname.trim(),
        email: editForm.email.trim()
      };
      
      await updateUser($user.uuid, updateData);
      showToast('success', 'Profile updated successfully!');
      activeTab = 'profile';
    } catch (e: any) {
      editError = e.message || 'Failed to update profile';
      if (editError) showToast('error', editError);
    } finally {
      isEditSubmitting = false;
    }
  }

  async function handlePasswordSubmit() {
    passwordError = null;

    if (!passwordForm.oldPassword || !passwordForm.newPassword || !passwordForm.confirmPassword) {
      passwordError = 'All fields are required';
      return;
    }

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      passwordError = 'New passwords do not match';
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      passwordError = 'New password must be at least 8 characters';
      return;
    }

    if (!$user) return;

    isPasswordSubmitting = true;
    try {
      const passwordData: PasswordChangeData = {
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword
      };
      
      await changePassword($user.uuid, passwordData);
      showToast('success', 'Password changed successfully!');
      passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      activeTab = 'profile';
    } catch (e: any) {
      passwordError = e.message || 'Failed to change password';
      if (passwordError) showToast('error', passwordError);
    } finally {
      isPasswordSubmitting = false;
    }
  }

  async function handleCancelMembership() {
    if (!cancelMembershipId) return;

    isCancelling = true;
    try {
      await membershipsApi.update(cancelMembershipId, { isActive: false });
      showToast('success', 'Membership cancelled successfully');
      await loadMemberships();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to cancel membership');
    } finally {
      isCancelling = false;
      cancelMembershipId = null;
    }
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }

  function isExpiringSoon(endDate: string): boolean {
    const end = new Date(endDate);
    const today = new Date();
    const daysLeft = Math.ceil((end.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    return daysLeft <= 30 && daysLeft > 0;
  }

  function isExpired(endDate: string): boolean {
    return new Date(endDate) < new Date();
  }
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <LoadingSpinner size="large" />
  </div>
{:else if $isAuthenticated && $user}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-purple-600 to-blue-600 text-white py-16">
      <div class="max-w-5xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">My Profile</h1>
        <p class="text-blue-100">Manage your account settings and memberships</p>
      </div>
    </div>

    <div class="max-w-5xl mx-auto px-6 -mt-8 pb-12">
      <!-- Tabs -->
      <div class="card-panel mb-6">
        <div class="flex flex-wrap border-b border-gray-200">
          <button
            class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 border-transparent transition-colors {activeTab === 'profile' ? 'text-blue-600 border-blue-600' : 'text-gray-600 hover:text-gray-900 hover:border-gray-300'}"
            on:click={() => activeTab = 'profile'}
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
            </svg>
            Profile
          </button>
          <button
            class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 border-transparent transition-colors {activeTab === 'edit' ? 'text-blue-600 border-blue-600' : 'text-gray-600 hover:text-gray-900 hover:border-gray-300'}"
            on:click={() => activeTab = 'edit'}
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
            Edit Profile
          </button>
          <button
            class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 border-transparent transition-colors {activeTab === 'password' ? 'text-blue-600 border-blue-600' : 'text-gray-600 hover:text-gray-900 hover:border-gray-300'}"
            on:click={() => activeTab = 'password'}
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            Change Password
          </button>
          <button
            class="flex items-center gap-2 px-6 py-3 text-sm font-medium border-b-2 border-transparent transition-colors {activeTab === 'memberships' ? 'text-blue-600 border-blue-600' : 'text-gray-600 hover:text-gray-900 hover:border-gray-300'}"
            on:click={() => activeTab = 'memberships'}
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
            My Memberships
          </button>
        </div>
      </div>

      <!-- Tab Content -->
      <div class="card-panel p-6">
        {#if activeTab === 'profile'}
          <!-- View Profile -->
          <div class="space-y-6">
            <h2 class="text-2xl font-bold text-gray-900 mb-6">Profile Information</h2>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                <div class="p-3 bg-gray-50 rounded-lg text-gray-900">{$user.name}</div>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                <div class="p-3 bg-gray-50 rounded-lg text-gray-900">{$user.surname}</div>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <div class="p-3 bg-gray-50 rounded-lg text-gray-900">{$user.email}</div>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Role</label>
                <div class="p-3 bg-gray-50 rounded-lg">
                  <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium
                    {$user.role === 'Admin' ? 'bg-purple-100 text-purple-800' : 
                     $user.role === 'Moderator' ? 'bg-blue-100 text-blue-800' : 
                     'bg-green-100 text-green-800'}">
                    {$user.role}
                  </span>
                </div>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Account Status</label>
                <div class="p-3 bg-gray-50 rounded-lg">
                  <span class="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium
                    {$user.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
                    {$user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
              </div>
              
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Member Since</label>
                <div class="p-3 bg-gray-50 rounded-lg text-gray-900">{formatDate($user.createdAt)}</div>
              </div>
            </div>
          </div>

        {:else if activeTab === 'edit'}
          <!-- Edit Profile -->
          <div class="space-y-6">
            <h2 class="text-2xl font-bold text-gray-900 mb-6">Edit Profile</h2>
            
            {#if editError}
              <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
                {editError}
              </div>
            {/if}
            
            <form on:submit|preventDefault={handleEditSubmit} class="space-y-4">
              <div>
                <label for="name" class="block text-sm font-medium text-gray-700 mb-1">First Name</label>
                <input
                  id="name"
                  type="text"
                  bind:value={editForm.name}
                  disabled={isEditSubmitting}
                  class="no-border-input w-full"
                  required
                />
              </div>
              
              <div>
                <label for="surname" class="block text-sm font-medium text-gray-700 mb-1">Last Name</label>
                <input
                  id="surname"
                  type="text"
                  bind:value={editForm.surname}
                  disabled={isEditSubmitting}
                  class="no-border-input w-full"
                  required
                />
              </div>
              
              <div>
                <label for="email" class="block text-sm font-medium text-gray-700 mb-1">Email</label>
                <input
                  id="email"
                  type="email"
                  bind:value={editForm.email}
                  disabled={isEditSubmitting}
                  class="no-border-input w-full"
                  required
                />
              </div>
              
              <div class="flex gap-3 pt-4">
                <button
                  type="submit"
                  disabled={isEditSubmitting}
                  class="btn-primary px-6 py-3 rounded-lg disabled:opacity-50 flex items-center gap-2"
                >
                  {#if isEditSubmitting}
                    <LoadingSpinner size="small" color="white" />
                  {/if}
                  Save Changes
                </button>
                <button
                  type="button"
                  on:click={() => { activeTab = 'profile'; loadUserData(); }}
                  disabled={isEditSubmitting}
                  class="btn-secondary px-6 py-3 rounded-lg disabled:opacity-50"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>

        {:else if activeTab === 'password'}
          <!-- Change Password -->
          <div class="space-y-6">
            <h2 class="text-2xl font-bold text-gray-900 mb-6">Change Password</h2>
            
            {#if passwordError}
              <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
                {passwordError}
              </div>
            {/if}
            
            <form on:submit|preventDefault={handlePasswordSubmit} class="space-y-4">
              <div>
                <label for="oldPassword" class="block text-sm font-medium text-gray-700 mb-1">Current Password</label>
                <input
                  id="oldPassword"
                  type="password"
                  bind:value={passwordForm.oldPassword}
                  disabled={isPasswordSubmitting}
                  class="no-border-input w-full"
                  required
                />
              </div>
              
              <div>
                <label for="newPassword" class="block text-sm font-medium text-gray-700 mb-1">New Password</label>
                <input
                  id="newPassword"
                  type="password"
                  bind:value={passwordForm.newPassword}
                  disabled={isPasswordSubmitting}
                  class="no-border-input w-full"
                  required
                  minlength="8"
                />
                <p class="text-sm text-gray-500 mt-1">Must be at least 8 characters</p>
              </div>
              
              <div>
                <label for="confirmPassword" class="block text-sm font-medium text-gray-700 mb-1">Confirm New Password</label>
                <input
                  id="confirmPassword"
                  type="password"
                  bind:value={passwordForm.confirmPassword}
                  disabled={isPasswordSubmitting}
                  class="no-border-input w-full"
                  required
                />
              </div>
              
              <div class="flex gap-3 pt-4">
                <button
                  type="submit"
                  disabled={isPasswordSubmitting}
                  class="btn-primary px-6 py-3 rounded-lg disabled:opacity-50 flex items-center gap-2"
                >
                  {#if isPasswordSubmitting}
                    <LoadingSpinner size="small" color="white" />
                  {/if}
                  Change Password
                </button>
                <button
                  type="button"
                  on:click={() => { activeTab = 'profile'; passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' }; }}
                  disabled={isPasswordSubmitting}
                  class="btn-secondary px-6 py-3 rounded-lg disabled:opacity-50"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>

        {:else if activeTab === 'memberships'}
          <!-- My Memberships -->
          <div class="space-y-6">
            <div class="flex items-center justify-between mb-6">
              <h2 class="text-2xl font-bold text-gray-900">My Memberships</h2>
              <button
                on:click={loadMemberships}
                disabled={loadingMemberships}
                class="btn-secondary px-4 py-2 rounded-lg disabled:opacity-50"
              >
                Refresh
              </button>
            </div>
            
            {#if membershipError}
              <div class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
                {membershipError}
              </div>
            {/if}
            
            {#if loadingMemberships}
              <div class="flex justify-center py-12">
                <LoadingSpinner size="large" />
              </div>
            {:else if memberships.length === 0}
              <div class="text-center py-12">
                <svg class="mx-auto h-12 w-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                </svg>
                <p class="text-gray-600">You don't have any memberships yet</p>
                <a href="/#/gyms" class="btn-primary inline-block px-6 py-3 rounded-lg mt-4">
                  Browse Gyms
                </a>
              </div>
            {:else}
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                {#each memberships as membership (membership.id)}
                  <div class="border border-gray-200 rounded-xl p-6 hover:shadow-lg transition-shadow
                    {!membership.isActive ? 'opacity-60' : ''}
                    {isExpired(membership.endDate) ? 'bg-red-50' : isExpiringSoon(membership.endDate) ? 'bg-yellow-50' : 'bg-white'}">
                    
                    <div class="flex items-start justify-between mb-4">
                      <div>
                        <h3 class="font-semibold text-lg text-gray-900">{membership.gymName}</h3>
                        <p class="text-sm text-gray-600">{membership.membershipType}</p>
                      </div>
                      <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium
                        {membership.isActive && !isExpired(membership.endDate) ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}">
                        {membership.isActive && !isExpired(membership.endDate) ? 'Active' : 'Inactive'}
                      </span>
                    </div>
                    
                    <div class="space-y-2 text-sm">
                      <div class="flex justify-between">
                        <span class="text-gray-600">Start Date:</span>
                        <span class="font-medium">{formatDate(membership.startDate)}</span>
                      </div>
                      <div class="flex justify-between">
                        <span class="text-gray-600">End Date:</span>
                        <span class="font-medium">{formatDate(membership.endDate)}</span>
                      </div>
                      <div class="flex justify-between">
                        <span class="text-gray-600">Price:</span>
                        <span class="font-medium text-green-600">${membership.price.toFixed(2)}</span>
                      </div>
                    </div>
                    
                    {#if isExpiringSoon(membership.endDate) && membership.isActive}
                      <div class="mt-4 p-3 bg-yellow-100 border border-yellow-200 rounded-lg">
                        <p class="text-sm text-yellow-800">⚠️ Expiring soon! Consider renewing.</p>
                      </div>
                    {/if}
                    
                    {#if isExpired(membership.endDate)}
                      <div class="mt-4 p-3 bg-red-100 border border-red-200 rounded-lg">
                        <p class="text-sm text-red-800">❌ Membership expired</p>
                      </div>
                    {/if}
                    
                    {#if membership.isActive && !isExpired(membership.endDate)}
                      <button
                        on:click={() => cancelMembershipId = membership.id}
                        class="w-full mt-4 bg-red-600 text-white hover:bg-red-700 transition-colors px-4 py-2 rounded-lg text-sm"
                      >
                        Cancel Membership
                      </button>
                    {/if}
                  </div>
                {/each}
              </div>
            {/if}
          </div>
        {/if}
      </div>
    </div>
  </div>

  <!-- Cancel Membership Confirmation -->
  <ConfirmDialog
    isOpen={cancelMembershipId !== null}
    title="Cancel Membership?"
    message="Are you sure you want to cancel this membership? This action cannot be undone."
    confirmText="Cancel Membership"
    confirmClass="bg-red-600 text-white hover:bg-red-700 transition-colors"
    isLoading={isCancelling}
    on:confirm={handleCancelMembership}
    on:cancel={() => cancelMembershipId = null}
  />
{/if}


