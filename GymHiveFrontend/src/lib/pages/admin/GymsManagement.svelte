<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading, getAccessToken } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { gymsApi, type Gym, type CreateGymDTO, type UpdateGymDTO } from '../../services/gyms';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import ConfirmDialog from '../../components/ui/ConfirmDialog.svelte';
  import { push } from 'svelte-spa-router';

  interface ModeratorDTO {
    firstName: string;
    lastName: string;
  }

  let gyms: Gym[] = [];
  let filteredGyms: Gym[] = [];
  let loading = false;
  let searchQuery = '';

  // Create/Edit Modal
  let showModal = false;
  let editingGym: Gym | null = null;
  let gymForm: any = {
    name: '',
    address: '',
    description: '',
    phoneNumber: '',
    email: '',
    website: '',
    openingTime: '06:00',
    closingTime: '22:00',
    moderators: []
  };
  let isSubmitting = false;

  // Moderators
  let moderators: ModeratorDTO[] = [];
  let newModFirstName = '';
  let newModLastName = '';

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
    console.log('🟡 Opening create modal');
    alert('CREATE MODAL OPENING');
    editingGym = null;
    moderators = [];
    newModFirstName = '';
    newModLastName = '';
    gymForm = {
      name: '',
      address: '',
      description: '',
      city: '',
      country: '',
      phone: '',
      email: ''
    };
    showModal = true;
    console.log('🟡 Modal opened, moderators array:', moderators);
  }

  function openEditModal(gym: Gym) {
    editingGym = gym;
    moderators = [];
    newModFirstName = '';
    newModLastName = '';
    gymForm = {
      name: gym.name,
      address: gym.address,
      description: gym.description || '',
      city: gym.city || '',
      country: gym.country || '',
      phone: gym.phone || '',
      email: gym.email || ''
    };
    showModal = true;
  }

  function addModerator() {
    alert('ADD MODERATOR CLICKED');
    console.log('🔵 ADD MODERATOR FUNCTION CALLED');
    if (!newModFirstName.trim() || !newModLastName.trim()) {
      showToast('error', 'First name and last name are required');
      return;
    }
    console.log('🔵 Adding moderator:', newModFirstName, newModLastName);
    moderators = [...moderators, { firstName: newModFirstName, lastName: newModLastName }];
    console.log('🔵 Current moderators list:', moderators);
    alert(`Moderator added! Total: ${moderators.length}`);
    newModFirstName = '';
    newModLastName = '';
  }

  function removeModerator(index: number) {
    moderators = moderators.filter((_, i) => i !== index);
  }

  async function handleSubmit() {
    alert('SUBMIT CLICKED');
    console.log('🟢 === SUBMIT STARTED ===');
    console.log('🟢 Gym form:', gymForm);
    console.log('🟢 Moderators to create:', moderators);
    console.log('🟢 Moderators count:', moderators.length);
    
    if (!gymForm.name.trim() || !gymForm.address.trim()) {
      showToast('error', 'Name and address are required');
      return;
    }

    isSubmitting = true;
    try {
      // Save gym without moderators
      const gymData = { ...gymForm };
      console.log('🟢 Saving gym:', gymData);
      
      let savedGymId: number;
      if (editingGym) {
        await gymsApi.update(editingGym.id, gymData);
        savedGymId = editingGym.id;
        showToast('success', 'Gym updated successfully');
      } else {
        const createdGym = await gymsApi.create(gymData);
        savedGymId = createdGym.id;
        showToast('success', 'Gym created successfully');
      }
      console.log('🟢 Gym saved successfully with ID:', savedGymId);

      // Create moderators separately using the direct endpoint
      if (moderators.length > 0) {
        console.log('Creating moderators:', moderators);
        let createdCount = 0;
        
        // Get the JWT token properly
        const token = await getAccessToken();
        console.log('🔑 Token retrieved:', token ? 'YES' : 'NO');
        
        if (!token) {
          showToast('error', 'Authentication token not found. Please log in again.');
          return;
        }
        
        for (const mod of moderators) {
          try {
            console.log(`Creating moderator: ${mod.firstName} ${mod.lastName} for gym: ${gymForm.name}`);
            
            const response = await fetch('http://localhost:5000/api/auth/create-moderator', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
              },
              body: JSON.stringify({
                firstName: mod.firstName,
                lastName: mod.lastName,
                gymName: gymForm.name,
                gymId: savedGymId
              })
            });

            const result = await response.json();
            console.log('Response:', response.status, result);

            if (!response.ok) {
              throw new Error(result.message || `Failed to create moderator ${mod.firstName} ${mod.lastName}`);
            }

            console.log('Moderator created successfully:', result);
            createdCount++;
          } catch (err: any) {
            console.error('Error creating moderator:', err);
            showToast('error', `Failed to create moderator ${mod.firstName} ${mod.lastName}: ${err.message}`);
          }
        }
        
        if (createdCount > 0) {
          showToast('success', `${createdCount} moderator(s) created successfully`);
        }
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
          <label class="block text-sm font-medium text-gray-700 mb-1">City</label>
          <input
            type="text"
            bind:value={gymForm.city}
            disabled={isSubmitting}
            class="no-border-input w-full"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Country</label>
          <input
            type="text"
            bind:value={gymForm.country}
            disabled={isSubmitting}
            class="no-border-input w-full"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Phone</label>
          <input
            type="tel"
            bind:value={gymForm.phone}
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

      <!-- Moderators Section -->
      <div class="md:col-span-2">
        <label class="block text-sm font-medium text-gray-700 mb-2">Gym Moderators</label>
        <div class="bg-gray-50 p-4 rounded-lg space-y-3">
          <!-- Add Moderator Form -->
          <div class="flex gap-2">
            <input
              type="text"
              bind:value={newModFirstName}
              placeholder="First Name"
              disabled={isSubmitting}
              class="no-border-input flex-1"
            />
            <input
              type="text"
              bind:value={newModLastName}
              placeholder="Last Name"
              disabled={isSubmitting}
              class="no-border-input flex-1"
            />
            <button
              type="button"
              on:click={addModerator}
              disabled={isSubmitting}
              class="bg-blue-600 text-white hover:bg-blue-700 px-4 py-2 rounded-lg whitespace-nowrap"
            >
              + Add
            </button>
          </div>

          <!-- Moderator List -->
          {#if moderators.length > 0}
            <div class="space-y-2">
              <p class="text-xs text-gray-600">Moderators to be created:</p>
              {#each moderators as mod, index}
                <div class="flex items-center justify-between bg-white p-3 rounded-lg border border-gray-200">
                  <div>
                    <span class="font-medium">{mod.firstName} {mod.lastName}</span>
                    <span class="text-xs text-gray-500 ml-2">
                      (Email: {mod.firstName.toLowerCase()}.{mod.lastName.toLowerCase()}@{gymForm.name ? gymForm.name.toLowerCase().replace(/\s/g, '') : 'gymname'}.com)
                    </span>
                  </div>
                  <button
                    type="button"
                    on:click={() => removeModerator(index)}
                    disabled={isSubmitting}
                    class="text-red-600 hover:text-red-800 text-sm"
                  >
                    Remove
                  </button>
                </div>
              {/each}
            </div>
          {:else}
            <p class="text-sm text-gray-500 italic">No moderators added yet</p>
          {/if}
          
          <p class="text-xs text-gray-500 mt-2">
            ℹ️ Moderators will be created with email format: firstname.lastname@gymname.com (default password: Moderator123!)
          </p>
        </div>
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
