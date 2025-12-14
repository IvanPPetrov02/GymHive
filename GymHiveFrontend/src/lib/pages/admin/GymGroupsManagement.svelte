<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { gymGroupsApi, type GymGroup, type CreateGymGroupDTO } from '../../services/gymGroups';
  import { gymsApi, type Gym } from '../../services/gyms';
  import { usersApi, type UserProfile } from '../../services/users';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import ConfirmDialog from '../../components/ui/ConfirmDialog.svelte';
  import { push } from 'svelte-spa-router';

  let groups: GymGroup[] = [];
  let gyms: Gym[] = [];
  let moderators: UserProfile[] = [];
  let loading = false;
  let searchQuery = '';

  // Create/Edit Modal
  let showModal = false;
  let groupForm: CreateGymGroupDTO = {
    name: '',
    description: '',
    gymId: 0,
    moderatorId: ''
  };
  let isSubmitting = false;

  // Delete Confirmation
  let deleteGroupId: number | null = null;
  let isDeleting = false;

  $: if ($user?.role !== 'Admin') {
    push('/');
    showToast('error', 'Access denied. Admin only.');
  }

  $: filteredGroups = groups.filter(g =>
    g.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    g.gymName.toLowerCase().includes(searchQuery.toLowerCase())
  );

  onMount(() => {
    requireAuth('#/admin/groups');
    if ($isAuthenticated && $user?.role === 'Admin') {
      loadData();
    }
  });

  async function loadData() {
    loading = true;
    try {
      [groups, gyms] = await Promise.all([
        gymGroupsApi.getAll(),
        gymsApi.getAll()
      ]);
      // Load all users to find moderators
      const allUsers = await usersApi.getAll();
      moderators = allUsers.filter(u => u.role === 'Moderator' || u.role === 'Admin');
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load data');
    } finally {
      loading = false;
    }
  }

  function openCreateModal() {
    groupForm = {
      name: '',
      description: '',
      gymId: gyms[0]?.id || 0,
      moderatorId: moderators[0]?.uuid || ''
    };
    showModal = true;
  }

  async function handleSubmit() {
    if (!groupForm.name.trim() || !groupForm.gymId || !groupForm.moderatorId) {
      showToast('error', 'All fields are required');
      return;
    }

    isSubmitting = true;
    try {
      await gymGroupsApi.create(groupForm);
      showToast('success', 'Group created successfully');
      showModal = false;
      await loadData();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to create group');
    } finally {
      isSubmitting = false;
    }
  }

  async function handleDelete() {
    if (!deleteGroupId) return;

    isDeleting = true;
    try {
      await gymGroupsApi.delete(deleteGroupId);
      showToast('success', 'Group deleted successfully');
      await loadData();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to delete group');
    } finally {
      isDeleting = false;
      deleteGroupId = null;
    }
  }
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <LoadingSpinner size="large" />
  </div>
{:else if $isAuthenticated && $user?.role === 'Admin'}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-teal-600 to-cyan-600 text-white py-16">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">Gym Groups Management</h1>
        <p class="text-teal-100">Create and manage gym groups with moderators</p>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Toolbar -->
      <div class="card-panel p-4 mb-6 flex flex-col sm:flex-row gap-4 justify-between">
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Search groups..."
          class="no-border-input flex-1"
        />
        <button
          on:click={openCreateModal}
          class="btn-primary px-6 py-3 rounded-lg whitespace-nowrap"
        >
          + Add Group
        </button>
      </div>

      <!-- Groups Grid -->
      {#if loading}
        <div class="flex justify-center py-12">
          <LoadingSpinner size="large" />
        </div>
      {:else if filteredGroups.length === 0}
        <div class="card-panel p-12 text-center">
          <p class="text-gray-600">No groups found</p>
        </div>
      {:else}
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {#each filteredGroups as group}
            <div class="card-panel p-6 hover:shadow-lg transition-shadow">
              <div class="flex items-start justify-between mb-4">
                <h3 class="text-xl font-bold text-gray-900">{group.name}</h3>
                <span class="inline-flex px-3 py-1 text-xs font-medium rounded-full
                  {group.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}">
                  {group.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              
              {#if group.description}
                <p class="text-sm text-gray-600 mb-4">{group.description}</p>
              {/if}
              
              <div class="space-y-2 text-sm mb-4">
                <div class="flex items-center gap-2 text-gray-700">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                  </svg>
                  {group.gymName}
                </div>
                <div class="flex items-center gap-2 text-gray-700">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                  Mod: {group.moderatorName}
                </div>
                <div class="flex items-center gap-2 text-gray-700">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                  {group.memberCount} members
                </div>
              </div>
              
              <div class="flex gap-2 pt-4 border-t border-gray-200">
                <button
                  on:click={() => deleteGroupId = group.id}
                  class="flex-1 px-4 py-2 text-sm text-red-600 border border-red-300 rounded-lg hover:bg-red-50 transition-colors"
                >
                  Delete
                </button>
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  </div>

  <!-- Create Modal -->
  <Modal bind:isOpen={showModal} title="Create Gym Group" size="medium">
    <form on:submit|preventDefault={handleSubmit} class="space-y-4">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Group Name *</label>
        <input
          type="text"
          bind:value={groupForm.name}
          disabled={isSubmitting}
          class="no-border-input w-full"
          required
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Gym *</label>
        <select
          bind:value={groupForm.gymId}
          disabled={isSubmitting}
          class="no-border-input w-full"
          required
        >
          {#each gyms as gym}
            <option value={gym.id}>{gym.name}</option>
          {/each}
        </select>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Moderator *</label>
        <select
          bind:value={groupForm.moderatorId}
          disabled={isSubmitting}
          class="no-border-input w-full"
          required
        >
          {#each moderators as mod}
            <option value={mod.uuid}>{mod.name} {mod.surname} ({mod.role})</option>
          {/each}
        </select>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          bind:value={groupForm.description}
          disabled={isSubmitting}
          rows="3"
          class="no-border-input w-full"
        ></textarea>
      </div>
    </form>

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={() => showModal = false}
        disabled={isSubmitting}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handleSubmit}
        disabled={isSubmitting}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isSubmitting}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Create
      </button>
    </svelte:fragment>
  </Modal>

  <!-- Delete Confirmation -->
  <ConfirmDialog
    isOpen={deleteGroupId !== null}
    title="Delete Group?"
    message="Are you sure you want to delete this group? This action cannot be undone."
    confirmText="Delete"
    isLoading={isDeleting}
    on:confirm={handleDelete}
    on:cancel={() => deleteGroupId = null}
  />
{/if}
