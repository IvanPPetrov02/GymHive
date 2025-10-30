<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { gymGroupsApi, type GymGroup } from '../../services/gymGroups';
  import { membershipsApi, type Membership, type UpdateMembershipDTO } from '../../services/memberships';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import { push } from 'svelte-spa-router';

  let myGroups: GymGroup[] = [];
  let selectedGroup: GymGroup | null = null;
  let memberships: Membership[] = [];
  let loading = false;
  let loadingMemberships = false;
  let searchQuery = '';

  // Edit Membership Modal
  let showEditModal = false;
  let editingMembership: Membership | null = null;
  let editForm: UpdateMembershipDTO = {
    membershipType: '',
    endDate: '',
    isActive: true
  };
  let isUpdating = false;

  $: if ($user?.role !== 'Moderator' && $user?.role !== 'Admin') {
    push('/');
    showToast('error', 'Access denied. Moderators only.');
  }

  $: filteredMemberships = memberships.filter(m =>
    m.gymName.toLowerCase().includes(searchQuery.toLowerCase()) ||
    m.membershipType.toLowerCase().includes(searchQuery.toLowerCase())
  );

  onMount(() => {
    requireAuth('#/moderator/memberships');
    if ($isAuthenticated && ($user?.role === 'Moderator' || $user?.role === 'Admin')) {
      loadMyGroups();
    }
  });

  async function loadMyGroups() {
    loading = true;
    try {
      myGroups = await gymGroupsApi.getModeratedGroups();
      if (myGroups.length > 0) {
        selectGroup(myGroups[0]);
      }
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load groups');
    } finally {
      loading = false;
    }
  }

  async function selectGroup(group: GymGroup) {
    selectedGroup = group;
    await loadMemberships(group.gymId);
  }

  async function loadMemberships(gymId: number) {
    loadingMemberships = true;
    try {
      memberships = await membershipsApi.getByGymId(gymId);
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load memberships');
    } finally {
      loadingMemberships = false;
    }
  }

  function openEditModal(membership: Membership) {
    editingMembership = membership;
    editForm = {
      membershipType: membership.membershipType,
      endDate: membership.endDate.split('T')[0],
      isActive: membership.isActive
    };
    showEditModal = true;
  }

  async function handleUpdate() {
    if (!editingMembership) return;

    isUpdating = true;
    try {
      await membershipsApi.update(editingMembership.id, editForm);
      showToast('success', 'Membership updated successfully');
      showEditModal = false;
      if (selectedGroup) {
        await loadMemberships(selectedGroup.gymId);
      }
    } catch (e: any) {
      showToast('error', e.message || 'Failed to update membership');
    } finally {
      isUpdating = false;
    }
  }

  function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  function isExpired(endDate: string): boolean {
    return new Date(endDate) < new Date();
  }
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <LoadingSpinner size="large" />
  </div>
{:else if $isAuthenticated && ($user?.role === 'Moderator' || $user?.role === 'Admin')}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-green-600 to-teal-600 text-white py-16">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">Memberships Management</h1>
        <p class="text-green-100">Manage memberships for your moderated groups</p>
      </div>
    </div>

    <div class="max-w-7xl mx-auto px-6 py-8">
      {#if loading}
        <div class="flex justify-center py-12">
          <LoadingSpinner size="large" />
        </div>
      {:else if myGroups.length === 0}
        <div class="card-panel p-12 text-center">
          <p class="text-gray-600">You are not moderating any groups yet</p>
        </div>
      {:else}
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
          <!-- Groups Sidebar -->
          <div class="lg:col-span-1">
            <div class="card-panel p-4">
              <h2 class="font-semibold text-gray-900 mb-4">My Groups</h2>
              <div class="space-y-2">
                {#each myGroups as group}
                  <button
                    on:click={() => selectGroup(group)}
                    class="w-full text-left p-3 rounded-lg transition-colors
                      {selectedGroup?.id === group.id ? 'bg-green-100 text-green-900' : 'hover:bg-gray-100'}"
                  >
                    <div class="font-medium">{group.name}</div>
                    <div class="text-sm text-gray-600">{group.gymName}</div>
                  </button>
                {/each}
              </div>
            </div>
          </div>

          <!-- Memberships List -->
          <div class="lg:col-span-3">
            {#if selectedGroup}
              <div class="card-panel p-6">
                <div class="flex items-center justify-between mb-6">
                  <div>
                    <h2 class="text-2xl font-bold text-gray-900">Memberships</h2>
                    <p class="text-sm text-gray-600">{selectedGroup.gymName}</p>
                  </div>
                  <input
                    type="text"
                    bind:value={searchQuery}
                    placeholder="Search..."
                    class="no-border-input w-64"
                  />
                </div>

                {#if loadingMemberships}
                  <div class="flex justify-center py-12">
                    <LoadingSpinner size="large" />
                  </div>
                {:else if filteredMemberships.length === 0}
                  <div class="text-center py-12 text-gray-600">
                    No memberships found
                  </div>
                {:else}
                  <div class="overflow-x-auto">
                    <table class="w-full">
                      <thead class="bg-gray-50 border-b border-gray-200">
                        <tr>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">User</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Start Date</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">End Date</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Price</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                          <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-gray-200">
                        {#each filteredMemberships as membership}
                          <tr class="hover:bg-gray-50 {isExpired(membership.endDate) ? 'bg-red-50' : ''}">
                            <td class="px-6 py-4 text-sm text-gray-900">User #{membership.userId.substring(0, 8)}</td>
                            <td class="px-6 py-4 text-sm text-gray-600">{membership.membershipType}</td>
                            <td class="px-6 py-4 text-sm text-gray-600">{formatDate(membership.startDate)}</td>
                            <td class="px-6 py-4 text-sm text-gray-600">{formatDate(membership.endDate)}</td>
                            <td class="px-6 py-4 text-sm font-medium text-green-600">${membership.price.toFixed(2)}</td>
                            <td class="px-6 py-4">
                              <span class="inline-flex px-3 py-1 text-xs font-medium rounded-full
                                {membership.isActive && !isExpired(membership.endDate) ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}">
                                {membership.isActive && !isExpired(membership.endDate) ? 'Active' : 'Inactive'}
                              </span>
                            </td>
                            <td class="px-6 py-4 text-right">
                              <button
                                on:click={() => openEditModal(membership)}
                                class="text-blue-600 hover:text-blue-800"
                              >
                                Edit
                              </button>
                            </td>
                          </tr>
                        {/each}
                      </tbody>
                    </table>
                  </div>
                {/if}
              </div>
            {/if}
          </div>
        </div>
      {/if}
    </div>
  </div>

  <!-- Edit Membership Modal -->
  <Modal bind:isOpen={showEditModal} title="Edit Membership" size="medium">
    {#if editingMembership}
      <form on:submit|preventDefault={handleUpdate} class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Membership Type</label>
          <select
            bind:value={editForm.membershipType}
            disabled={isUpdating}
            class="no-border-input w-full"
          >
            <option value="Monthly">Monthly</option>
            <option value="Quarterly">Quarterly</option>
            <option value="Annual">Annual</option>
            <option value="Premium">Premium</option>
          </select>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">End Date</label>
          <input
            type="date"
            bind:value={editForm.endDate}
            disabled={isUpdating}
            class="no-border-input w-full"
          />
        </div>

        <div class="flex items-center gap-2">
          <input
            type="checkbox"
            id="isActive"
            bind:checked={editForm.isActive}
            disabled={isUpdating}
            class="rounded"
          />
          <label for="isActive" class="text-sm font-medium text-gray-700">
            Active
          </label>
        </div>
      </form>
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
        on:click={handleUpdate}
        disabled={isUpdating}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isUpdating}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Update
      </button>
    </svelte:fragment>
  </Modal>
{/if}
