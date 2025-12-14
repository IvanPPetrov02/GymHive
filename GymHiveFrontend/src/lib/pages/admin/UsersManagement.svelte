<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { usersApi, type UserProfile } from '../../services/users';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import ConfirmDialog from '../../components/ui/ConfirmDialog.svelte';
  import { push } from 'svelte-spa-router';

  let users: UserProfile[] = [];
  let filteredUsers: UserProfile[] = [];
  let loading = false;
  let searchQuery = '';

  // Edit User Modal
  let showEditModal = false;
  let editingUser: UserProfile | null = null;
  let newRole: 'User' | 'Moderator' | 'Admin' = 'User';
  let isUpdating = false;

  // Delete Confirmation
  let deleteUserId: string | null = null;
  let isDeleting = false;

  $: if ($user?.role !== 'Admin') {
    push('/');
    showToast('error', 'Access denied. Admin only.');
  }

  $: filteredUsers = users.filter(u =>
    u.email.toLowerCase().includes(searchQuery.toLowerCase()) ||
    u.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    u.surname.toLowerCase().includes(searchQuery.toLowerCase())
  );

  onMount(() => {
    requireAuth('#/admin/users');
    if ($isAuthenticated && $user?.role === 'Admin') {
      loadUsers();
    }
  });

  async function loadUsers() {
    loading = true;
    try {
      users = await usersApi.getAll();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load users');
    } finally {
      loading = false;
    }
  }

  function openEditModal(targetUser: UserProfile) {
    editingUser = targetUser;
    newRole = targetUser.role;
    showEditModal = true;
  }

  async function handleRoleChange() {
    if (!editingUser) return;

    isUpdating = true;
    try {
      await usersApi.changeRole(editingUser.uuid, newRole);
      showToast('success', 'User role updated successfully');
      showEditModal = false;
      await loadUsers();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to update role');
    } finally {
      isUpdating = false;
    }
  }

  async function toggleUserActive(userId: string) {
    try {
      await usersApi.toggleActive(userId);
      showToast('success', 'User status updated');
      await loadUsers();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to update status');
    }
  }

  async function handleDelete() {
    if (!deleteUserId) return;

    isDeleting = true;
    try {
      await usersApi.delete(deleteUserId);
      showToast('success', 'User deleted successfully');
      await loadUsers();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to delete user');
    } finally {
      isDeleting = false;
      deleteUserId = null;
    }
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <LoadingSpinner size="large" />
  </div>
{:else if $isAuthenticated && $user?.role === 'Admin'}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-indigo-600 to-purple-600 text-white py-16">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">Users Management</h1>
        <p class="text-indigo-100">Manage user accounts, roles, and permissions</p>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Toolbar -->
      <div class="card-panel p-4 mb-6">
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Search users by name or email..."
          class="no-border-input w-full"
        />
      </div>

      <!-- Users Table -->
      {#if loading}
        <div class="flex justify-center py-12">
          <LoadingSpinner size="large" />
        </div>
      {:else if filteredUsers.length === 0}
        <div class="card-panel p-12 text-center">
          <p class="text-gray-600">No users found</p>
        </div>
      {:else}
        <div class="card-panel overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">User</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Role</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Joined</th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200">
                {#each filteredUsers as targetUser}
                  <tr class="hover:bg-gray-50">
                    <td class="px-6 py-4">
                      <div class="font-medium text-gray-900">{targetUser.name} {targetUser.surname}</div>
                    </td>
                    <td class="px-6 py-4 text-sm text-gray-600">{targetUser.email}</td>
                    <td class="px-6 py-4">
                      <span class="inline-flex px-3 py-1 text-xs font-medium rounded-full
                        {targetUser.role === 'Admin' ? 'bg-purple-100 text-purple-800' :
                         targetUser.role === 'Moderator' ? 'bg-blue-100 text-blue-800' :
                         'bg-green-100 text-green-800'}">
                        {targetUser.role}
                      </span>
                    </td>
                    <td class="px-6 py-4">
                      <button
                        on:click={() => toggleUserActive(targetUser.uuid)}
                        class="inline-flex px-3 py-1 text-xs font-medium rounded-full cursor-pointer
                          {targetUser.isActive ? 'bg-green-100 text-green-800 hover:bg-green-200' :
                           'bg-red-100 text-red-800 hover:bg-red-200'}">
                        {targetUser.isActive ? 'Active' : 'Inactive'}
                      </button>
                    </td>
                    <td class="px-6 py-4 text-sm text-gray-600">{formatDate(targetUser.createdAt)}</td>
                    <td class="px-6 py-4 text-right space-x-2">
                      <button
                        on:click={() => openEditModal(targetUser)}
                        class="text-blue-600 hover:text-blue-800"
                        disabled={targetUser.uuid === $user?.uuid}
                      >
                        Change Role
                      </button>
                      <button
                        on:click={() => deleteUserId = targetUser.uuid}
                        class="text-red-600 hover:text-red-800"
                        disabled={targetUser.uuid === $user?.uuid}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                {/each}
              </tbody>
            </table>
          </div>
        </div>
      {/if}
    </div>
  </div>

  <!-- Edit Role Modal -->
  <Modal bind:isOpen={showEditModal} title="Change User Role" size="small">
    {#if editingUser}
      <div class="space-y-4">
        <div>
          <p class="text-sm text-gray-600 mb-1">User</p>
          <p class="font-medium">{editingUser.name} {editingUser.surname}</p>
          <p class="text-sm text-gray-600">{editingUser.email}</p>
        </div>
        
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">New Role</label>
          <select
            bind:value={newRole}
            disabled={isUpdating}
            class="no-border-input w-full"
          >
            <option value="User">User</option>
            <option value="Moderator">Moderator</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
      </div>
    {/if}

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={() => showEditModal = false}
        disabled={isUpdating}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handleRoleChange}
        disabled={isUpdating}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isUpdating}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Update Role
      </button>
    </svelte:fragment>
  </Modal>

  <!-- Delete Confirmation -->
  <ConfirmDialog
    isOpen={deleteUserId !== null}
    title="Delete User?"
    message="Are you sure you want to delete this user? All their data will be permanently removed."
    confirmText="Delete"
    isLoading={isDeleting}
    on:confirm={handleDelete}
    on:cancel={() => deleteUserId = null}
  />
{/if}
