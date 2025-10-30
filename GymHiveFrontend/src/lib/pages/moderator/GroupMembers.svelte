<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../../auth';
  import { requireAuth } from '../../auth/auth';
  import { gymGroupsApi, type GymGroup, type GymGroupMember, type AddMemberDTO } from '../../services/gymGroups';
  import { usersApi, type UserProfile } from '../../services/users';
  import { showToast } from '../../components/ui/Toast.svelte';
  import LoadingSpinner from '../../components/ui/LoadingSpinner.svelte';
  import Modal from '../../components/ui/Modal.svelte';
  import ConfirmDialog from '../../components/ui/ConfirmDialog.svelte';
  import { push } from 'svelte-spa-router';

  let myGroups: GymGroup[] = [];
  let selectedGroup: GymGroup | null = null;
  let members: GymGroupMember[] = [];
  let allUsers: UserProfile[] = [];
  let loading = false;
  let loadingMembers = false;

  // Add Member Modal
  let showAddModal = false;
  let selectedUserId = '';
  let isAdding = false;

  // Remove Member
  let removeMemberId: number | null = null;
  let isRemoving = false;

  $: if ($user?.role !== 'Moderator' && $user?.role !== 'Admin') {
    push('/');
    showToast('error', 'Access denied. Moderators only.');
  }

  onMount(() => {
    requireAuth('#/moderator/members');
    if ($isAuthenticated && ($user?.role === 'Moderator' || $user?.role === 'Admin')) {
      loadMyGroups();
      loadAllUsers();
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

  async function loadAllUsers() {
    try {
      allUsers = await usersApi.getAll();
    } catch (e: any) {
      console.error('Failed to load users:', e);
    }
  }

  async function selectGroup(group: GymGroup) {
    selectedGroup = group;
    await loadMembers(group.id);
  }

  async function loadMembers(groupId: number) {
    loadingMembers = true;
    try {
      members = await gymGroupsApi.getMembers(groupId);
    } catch (e: any) {
      showToast('error', e.message || 'Failed to load members');
    } finally {
      loadingMembers = false;
    }
  }

  function openAddModal() {
    selectedUserId = allUsers[0]?.uuid || '';
    showAddModal = true;
  }

  async function handleAddMember() {
    if (!selectedGroup || !selectedUserId) return;

    isAdding = true;
    try {
      const data: AddMemberDTO = { userId: selectedUserId };
      await gymGroupsApi.addMember(selectedGroup.id, data);
      showToast('success', 'Member added successfully');
      showAddModal = false;
      await loadMembers(selectedGroup.id);
    } catch (e: any) {
      showToast('error', e.message || 'Failed to add member');
    } finally {
      isAdding = false;
    }
  }

  async function handleRemoveMember() {
    if (!selectedGroup || !removeMemberId) return;

    isRemoving = true;
    try {
      await gymGroupsApi.removeMember(selectedGroup.id, removeMemberId);
      showToast('success', 'Member removed successfully');
      await loadMembers(selectedGroup.id);
    } catch (e: any) {
      showToast('error', e.message || 'Failed to remove member');
    } finally {
      isRemoving = false;
      removeMemberId = null;
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
{:else if $isAuthenticated && ($user?.role === 'Moderator' || $user?.role === 'Admin')}
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <div class="bg-gradient-to-r from-blue-600 to-indigo-600 text-white py-16">
      <div class="max-w-7xl mx-auto px-6">
        <h1 class="text-4xl font-bold mb-2">Group Members Management</h1>
        <p class="text-blue-100">Manage members in your moderated groups</p>
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
                      {selectedGroup?.id === group.id ? 'bg-blue-100 text-blue-900' : 'hover:bg-gray-100'}"
                  >
                    <div class="font-medium">{group.name}</div>
                    <div class="text-sm text-gray-600">{group.gymName}</div>
                    <div class="text-xs text-gray-500 mt-1">{group.memberCount} members</div>
                  </button>
                {/each}
              </div>
            </div>
          </div>

          <!-- Members List -->
          <div class="lg:col-span-3">
            {#if selectedGroup}
              <div class="card-panel p-6">
                <div class="flex items-center justify-between mb-6">
                  <div>
                    <h2 class="text-2xl font-bold text-gray-900">{selectedGroup.name}</h2>
                    <p class="text-sm text-gray-600">{selectedGroup.memberCount} members</p>
                  </div>
                  <button
                    on:click={openAddModal}
                    class="btn-primary px-6 py-2 rounded-lg"
                  >
                    + Add Member
                  </button>
                </div>

                {#if loadingMembers}
                  <div class="flex justify-center py-12">
                    <LoadingSpinner size="large" />
                  </div>
                {:else if members.length === 0}
                  <div class="text-center py-12 text-gray-600">
                    No members yet
                  </div>
                {:else}
                  <div class="overflow-x-auto">
                    <table class="w-full">
                      <thead class="bg-gray-50 border-b border-gray-200">
                        <tr>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Member</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Email</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Joined</th>
                          <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                          <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-gray-200">
                        {#each members as member}
                          <tr class="hover:bg-gray-50">
                            <td class="px-6 py-4 font-medium text-gray-900">{member.userName}</td>
                            <td class="px-6 py-4 text-sm text-gray-600">{member.userEmail}</td>
                            <td class="px-6 py-4 text-sm text-gray-600">{formatDate(member.joinedAt)}</td>
                            <td class="px-6 py-4">
                              <span class="inline-flex px-3 py-1 text-xs font-medium rounded-full
                                {member.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}">
                                {member.isActive ? 'Active' : 'Inactive'}
                              </span>
                            </td>
                            <td class="px-6 py-4 text-right">
                              <button
                                on:click={() => removeMemberId = member.id}
                                class="text-red-600 hover:text-red-800"
                              >
                                Remove
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

  <!-- Add Member Modal -->
  <Modal bind:isOpen={showAddModal} title="Add Member" size="small">
    <div class="space-y-4">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-1">Select User</label>
        <select
          bind:value={selectedUserId}
          disabled={isAdding}
          class="no-border-input w-full"
        >
          {#each allUsers as user}
            <option value={user.uuid}>{user.name} {user.surname} ({user.email})</option>
          {/each}
        </select>
      </div>
    </div>

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={() => showAddModal = false}
        disabled={isAdding}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handleAddMember}
        disabled={isAdding}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isAdding}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Add
      </button>
    </svelte:fragment>
  </Modal>

  <!-- Remove Confirmation -->
  <ConfirmDialog
    isOpen={removeMemberId !== null}
    title="Remove Member?"
    message="Are you sure you want to remove this member from the group?"
    confirmText="Remove"
    isLoading={isRemoving}
    on:confirm={handleRemoveMember}
    on:cancel={() => removeMemberId = null}
  />
{/if}
