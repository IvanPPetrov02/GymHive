<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { gymsApi, type Gym, type CreateGymDTO, type UpdateGymDTO } from '../../services/gyms';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import ConfirmDialog from '../../components/ui/ConfirmDialog.svelte';
  import { push } from 'svelte-spa-router';

  let gyms: Gym[] = [];
  let filteredGyms: Gym[] = [];
  let loading = false;
  let searchQuery = '';

  // Create/Edit Modal
  let showModal = false;
  let editingGym: Gym | null = null;
  let gymForm: CreateGymDTO = {
    name: '',
    address: '',
    description: '',
    phoneNumber: '',
    email: '',
    website: '',
    openingTime: '06:00',
    closingTime: '22:00'
  };
  let isSubmitting = false;

  // Delete Confirmation
  let deleteGymId: number | null = null;
  let isDeleting = false;

  $: if ($user?.role !== 'Admin') {
    push('/');
    showToast('error', 'Access denied. Admin only.');
  }

  $: filteredGyms = gyms.filter(gym =>
    gym.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    gym.address.toLowerCase().includes(searchQuery.toLowerCase())
  );

  onMount(() => {
    requireAuth('#/admin/gyms');
    if ($isAuthenticated && $user?.role === 'Admin') {
      loadGyms();
    }
  });

  async function loadGyms() {
    loading = true;
    try {
      gyms = await gymsApi.getAll();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load gyms');
    } finally {
      loading = false;
    }
  }

  function openCreateModal() {
    editingGym = null;
    gymForm = {
      name: '',
      address: '',
      description: '',
      phoneNumber: '',
      email: '',
      website: '',
      openingTime: '06:00',
      closingTime: '22:00'
    };
    showModal = true;
  }

  function openEditModal(gym: Gym) {
    editingGym = gym;
    gymForm = {
      name: gym.name,
      address: gym.address,
      description: gym.description || '',
      phoneNumber: gym.phoneNumber || '',
      email: gym.email || '',
      website: gym.website || '',
      openingTime: gym.openingTime || '06:00',
      closingTime: gym.closingTime || '22:00'
    };
    showModal = true;
  }

  async function handleSubmit() {
    if (!gymForm.name.trim() || !gymForm.address.trim()) {
      showToast('error', 'Name and address are required');
      return;
    }

    isSubmitting = true;
    try {
      if (editingGym) {
        await gymsApi.update(editingGym.id, gymForm);
        showToast('success', 'Gym updated successfully');
      } else {
        await gymsApi.create(gymForm);
        showToast('success', 'Gym created successfully');
      }
      showModal = false;
      await loadGyms();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to save gym');
    } finally {
      isSubmitting = false;
    }
  }

  async function handleDelete() {
    if (!deleteGymId) return;

    isDeleting = true;
    try {
      await gymsApi.delete(deleteGymId);
      showToast('success', 'Gym deleted successfully');
      await loadGyms();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to delete gym');
    } finally {
      isDeleting = false;
      deleteGymId = null;
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
    <div class="bg-gradient-to-r from-purple-600 to-pink-600 text-white py-16">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">Gyms Management</h1>
        <p class="text-purple-100">Create, edit, and manage gym locations</p>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Toolbar -->
      <div class="card-panel p-4 mb-6 flex flex-col sm:flex-row gap-4 justify-between">
        <input
          type="text"
          bind:value={searchQuery}
          placeholder="Search gyms..."
          class="no-border-input flex-1"
        />
        <button
          on:click={openCreateModal}
          class="btn-primary px-6 py-3 rounded-lg whitespace-nowrap"
        >
          + Add Gym
        </button>
      </div>

      <!-- Gyms Table -->
      {#if loading}
        <div class="flex justify-center py-12">
          <LoadingSpinner size="large" />
        </div>
      {:else if filteredGyms.length === 0}
        <div class="card-panel p-12 text-center">
          <p class="text-gray-600">No gyms found</p>
        </div>
      {:else}
        <div class="card-panel overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full">
              <thead class="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Address</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Contact</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Members</th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200">
                {#each filteredGyms as gym}
                  <tr class="hover:bg-gray-50">
                    <td class="px-6 py-4">
                      <div class="font-medium text-gray-900">{gym.name}</div>
                    </td>
                    <td class="px-6 py-4 text-sm text-gray-600">{gym.address}</td>
                    <td class="px-6 py-4 text-sm text-gray-600">
                      {#if gym.phoneNumber}
                        <div>{gym.phoneNumber}</div>
                      {/if}
                      {#if gym.email}
                        <div>{gym.email}</div>
                      {/if}
                    </td>
                    <td class="px-6 py-4 text-sm text-gray-600">{gym.memberCount || 0}</td>
                    <td class="px-6 py-4 text-right space-x-2">
                      <button
                        on:click={() => openEditModal(gym)}
                        class="text-blue-600 hover:text-blue-800"
                        title="Edit"
                      >
                        Edit
                      </button>
                      <button
                        on:click={() => deleteGymId = gym.id}
                        class="text-red-600 hover:text-red-800"
                        title="Delete"
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

  <!-- Create/Edit Modal -->
  <Modal bind:isOpen={showModal} title={editingGym ? 'Edit Gym' : 'Create Gym'} size="large">
    <form on:submit|preventDefault={handleSubmit} class="space-y-4">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
          <input
            type="text"
            bind:value={gymForm.name}
            disabled={isSubmitting}
            class="no-border-input w-full"
            required
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Address *</label>
          <input
            type="text"
            bind:value={gymForm.address}
            disabled={isSubmitting}
            class="no-border-input w-full"
            required
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Phone</label>
          <input
            type="tel"
            bind:value={gymForm.phoneNumber}
            disabled={isSubmitting}
            class="no-border-input w-full"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Email</label>
          <input
            type="email"
            bind:value={gymForm.email}
            disabled={isSubmitting}
            class="no-border-input w-full"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Website</label>
          <input
            type="url"
            bind:value={gymForm.website}
            disabled={isSubmitting}
            class="no-border-input w-full"
          />
        </div>
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Opening</label>
            <input
              type="time"
              bind:value={gymForm.openingTime}
              disabled={isSubmitting}
              class="no-border-input w-full"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Closing</label>
            <input
              type="time"
              bind:value={gymForm.closingTime}
              disabled={isSubmitting}
              class="no-border-input w-full"
            />
          </div>
        </div>
      </div>
      <div class="md:col-span-2">
        <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          bind:value={gymForm.description}
          disabled={isSubmitting}
          rows="4"
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
        {editingGym ? 'Update' : 'Create'}
      </button>
    </svelte:fragment>
  </Modal>

  <!-- Delete Confirmation -->
  <ConfirmDialog
    isOpen={deleteGymId !== null}
    title="Delete Gym?"
    message="Are you sure you want to delete this gym? This action cannot be undone."
    confirmText="Delete"
    isLoading={isDeleting}
    on:confirm={handleDelete}
    on:cancel={() => deleteGymId = null}
  />
{/if}
