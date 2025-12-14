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

  // Delete Group
  let showDeleteGroupModal = false;
  let isDeletingGroup = false;

  // Create Group Modal
  let showCreateGroupModal = false;
  let newGroup = {
    name: '',
    description: '',
    maxMembers: 20,
    schedule: ''
  };
  let isCreatingGroup = false;

  // Edit Group Modal
  let showEditGroupModal = false;
  let editGroup = {
    description: '',
    maxMembers: 20,
    schedule: ''
  };
  let editSelectedDays: string[] = [];
  let editScheduleTime = '08:00';
  let isEditingGroup = false;

  // Schedule builder
  let selectedDays: string[] = [];
  let scheduleTime = '08:00';
  const daysOfWeek = [
    { value: 'Mon', label: 'Monday' },
    { value: 'Tue', label: 'Tuesday' },
    { value: 'Wed', label: 'Wednesday' },
    { value: 'Thu', label: 'Thursday' },
    { value: 'Fri', label: 'Friday' },
    { value: 'Sat', label: 'Saturday' },
    { value: 'Sun', label: 'Sunday' }
  ];

  function toggleDay(day: string) {
    if (selectedDays.includes(day)) {
      selectedDays = selectedDays.filter(d => d !== day);
    } else {
      selectedDays = [...selectedDays, day];
    }
    updateScheduleString();
  }

  function updateScheduleString() {
    if (selectedDays.length > 0 && scheduleTime) {
      newGroup.schedule = `${selectedDays.join('/')} ${scheduleTime}`;
    } else {
      newGroup.schedule = '';
    }
  }

  $: scheduleTime && updateScheduleString();

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

  async function handleCreateGroup() {
    if (!$user?.gymId) {
      showToast('error', 'Gym ID not found');
      return;
    }

    isCreatingGroup = true;
    try {
      await gymGroupsApi.create({
        gymId: $user.gymId,
        name: newGroup.name,
        description: newGroup.description,
        moderatorId: $user.uuid,
        maxMembers: newGroup.maxMembers,
        schedule: newGroup.schedule
      });
      showToast('success', 'Group created successfully');
      showCreateGroupModal = false;
      // Reset form
      newGroup = { name: '', description: '', maxMembers: 20, schedule: '' };
      selectedDays = [];
      scheduleTime = '08:00';
      await loadMyGroups();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to create group');
    } finally {
      isCreatingGroup = false;
    }
  }

  function closeCreateGroupModal() {
    showCreateGroupModal = false;
    newGroup = { name: '', description: '', maxMembers: 20, schedule: '' };
    selectedDays = [];
    scheduleTime = '08:00';
  }

  function openEditGroupModal() {
    if (!selectedGroup) return;
    
    editGroup = {
      description: selectedGroup.description || '',
      maxMembers: selectedGroup.maxMembers || 20,
      schedule: selectedGroup.schedule || ''
    };
    
    // Parse existing schedule
    if (selectedGroup.schedule) {
      const parts = selectedGroup.schedule.split(' ');
      if (parts.length === 2) {
        editSelectedDays = parts[0].split('/');
        editScheduleTime = parts[1];
      }
    } else {
      editSelectedDays = [];
      editScheduleTime = '08:00';
    }
    
    showEditGroupModal = true;
  }

  function toggleEditDay(day: string) {
    if (editSelectedDays.includes(day)) {
      editSelectedDays = editSelectedDays.filter(d => d !== day);
    } else {
      editSelectedDays = [...editSelectedDays, day];
    }
    updateEditScheduleString();
  }

  function updateEditScheduleString() {
    if (editSelectedDays.length > 0 && editScheduleTime) {
      editGroup.schedule = `${editSelectedDays.join('/')} ${editScheduleTime}`;
    } else {
      editGroup.schedule = '';
    }
  }

  $: editScheduleTime && updateEditScheduleString();

  async function handleEditGroup() {
    if (!selectedGroup) return;
    
    isEditingGroup = true;
    try {
      await gymGroupsApi.update(selectedGroup.id, {
        description: editGroup.description,
        maxMembers: editGroup.maxMembers,
        schedule: editGroup.schedule
      });
      showToast('success', 'Group updated successfully');
      showEditGroupModal = false;
      await loadMyGroups();
      if (selectedGroup) {
        await loadMembers(selectedGroup.id);
      }
    } catch (e: any) {
      showToast('error', e.message || 'Failed to update group');
    } finally {
      isEditingGroup = false;
    }
  }

  function closeEditGroupModal() {
    showEditGroupModal = false;
    editGroup = { description: '', maxMembers: 20, schedule: '' };
    editSelectedDays = [];
    editScheduleTime = '08:00';
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

  async function handleDeleteGroup() {
    if (!selectedGroup) return;

    isDeletingGroup = true;
    try {
      await gymGroupsApi.delete(selectedGroup.id);
      showToast('success', 'Group deleted successfully');
      showDeleteGroupModal = false;
      selectedGroup = null;
      members = [];
      await loadMyGroups();
    } catch (e: any) {
      showToast('error', e.message || 'Failed to delete group');
    } finally {
      isDeletingGroup = false;
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
          <p class="text-gray-600 mb-4">You don't have any groups yet</p>
          <button 
            on:click={() => showCreateGroupModal = true}
            class="btn-primary"
          >
            Create Your First Group
          </button>
        </div>
      {:else}
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">
          <!-- Groups Sidebar -->
          <div class="lg:col-span-1">
            <div class="card-panel p-4">
              <div class="flex justify-between items-center mb-4">
                <h2 class="font-semibold text-gray-900">My Groups</h2>
                <button
                  on:click={() => showCreateGroupModal = true}
                  class="btn-sm btn-primary"
                  title="Create New Group"
                >
                  + New
                </button>
              </div>
              <div class="space-y-2">
                {#each myGroups as group}
                  <button
                    on:click={() => selectGroup(group)}
                    class="w-full text-left p-3 rounded-lg transition-colors
                      {selectedGroup?.id === group.id ? 'bg-blue-100 text-blue-900' : 'hover:bg-gray-100'}"
                  >
                    <div class="font-medium">{group.name}</div>
                    <div class="text-sm text-gray-600">{group.gymName}</div>
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
                    <p class="text-sm text-gray-600">{members.length} members</p>
                    {#if selectedGroup.description}
                      <p class="text-sm text-gray-500 mt-1">{selectedGroup.description}</p>
                    {/if}
                    {#if selectedGroup.schedule}
                      <p class="text-xs text-blue-600 mt-1">📅 {selectedGroup.schedule}</p>
                    {/if}
                  </div>
                  <div class="flex gap-2">
                    <button
                      on:click={openEditGroupModal}
                      class="bg-gray-600 hover:bg-gray-700 text-white px-4 py-2 rounded-lg transition"
                    >
                      ✏️ Edit Group
                    </button>
                    <button
                      on:click={() => showDeleteGroupModal = true}
                      class="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-lg transition"
                    >
                      🗑️ Delete Group
                    </button>
                    <button
                      on:click={openAddModal}
                      class="btn-primary px-6 py-2 rounded-lg"
                    >
                      + Add Member
                    </button>
                  </div>
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

  <!-- Create Group Modal -->
  <Modal bind:isOpen={showCreateGroupModal} title="Create New Group">
    <form on:submit|preventDefault={handleCreateGroup} class="space-y-4">
      <div>
        <label for="groupName" class="block text-sm font-medium text-gray-700 mb-1">Group Name</label>
        <input
          id="groupName"
          type="text"
          bind:value={newGroup.name}
          required
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
          placeholder="e.g., Morning Cardio"
        />
      </div>

      <div>
        <label for="groupDescription" class="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          id="groupDescription"
          bind:value={newGroup.description}
          rows="3"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
          placeholder="Describe the group's purpose and activities"
        ></textarea>
      </div>

      <div>
        <label for="maxMembers" class="block text-sm font-medium text-gray-700 mb-1">Max Members</label>
        <input
          id="maxMembers"
          type="number"
          bind:value={newGroup.maxMembers}
          min="1"
          max="100"
          required
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">Schedule Days</label>
        <div class="grid grid-cols-2 gap-2">
          {#each daysOfWeek as day}
            <button
              type="button"
              on:click={() => toggleDay(day.value)}
              class="px-3 py-2 rounded-lg border transition-colors {selectedDays.includes(day.value) 
                ? 'bg-blue-600 text-white border-blue-600' 
                : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'}"
            >
              {day.label}
            </button>
          {/each}
        </div>
      </div>

      <div>
        <label for="scheduleTime" class="block text-sm font-medium text-gray-700 mb-1">Time</label>
        <input
          id="scheduleTime"
          type="time"
          bind:value={scheduleTime}
          on:change={updateScheduleString}
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {#if newGroup.schedule}
        <div class="bg-blue-50 border border-blue-200 rounded-lg p-3">
          <p class="text-sm text-blue-800">
            <span class="font-medium">Schedule:</span> {newGroup.schedule}
          </p>
        </div>
      {/if}
    </form>

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={closeCreateGroupModal}
        disabled={isCreatingGroup}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handleCreateGroup}
        disabled={isCreatingGroup || !newGroup.name}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isCreatingGroup}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Create Group
      </button>
    </svelte:fragment>
  </Modal>

  <!-- Edit Group Modal -->
  <Modal bind:isOpen={showEditGroupModal} title="Edit Group">
    <form on:submit|preventDefault={handleEditGroup} class="space-y-4">
      <div>
        <label for="editGroupDescription" class="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          id="editGroupDescription"
          bind:value={editGroup.description}
          rows="3"
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
          placeholder="Describe the group's purpose and activities"
        ></textarea>
      </div>

      <div>
        <label for="editMaxMembers" class="block text-sm font-medium text-gray-700 mb-1">Max Members</label>
        <input
          id="editMaxMembers"
          type="number"
          bind:value={editGroup.maxMembers}
          min="1"
          max="100"
          required
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">Schedule Days</label>
        <div class="grid grid-cols-2 gap-2">
          {#each daysOfWeek as day}
            <button
              type="button"
              on:click={() => toggleEditDay(day.value)}
              class="px-3 py-2 rounded-lg border transition-colors {editSelectedDays.includes(day.value) 
                ? 'bg-blue-600 text-white border-blue-600' 
                : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'}"
            >
              {day.label}
            </button>
          {/each}
        </div>
      </div>

      <div>
        <label for="editScheduleTime" class="block text-sm font-medium text-gray-700 mb-1">Time</label>
        <input
          id="editScheduleTime"
          type="time"
          bind:value={editScheduleTime}
          on:change={updateEditScheduleString}
          class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {#if editGroup.schedule}
        <div class="bg-blue-50 border border-blue-200 rounded-lg p-3">
          <p class="text-sm text-blue-800">
            <span class="font-medium">Schedule:</span> {editGroup.schedule}
          </p>
        </div>
      {/if}
    </form>

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={closeEditGroupModal}
        disabled={isEditingGroup}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handleEditGroup}
        disabled={isEditingGroup}
        class="btn-primary px-6 py-2 rounded-lg flex items-center gap-2"
      >
        {#if isEditingGroup}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Save Changes
      </button>
    </svelte:fragment>
  </Modal>
{/if}

<!-- Delete Group Confirmation -->
<ConfirmDialog
  show={showDeleteGroupModal}
  title="Delete Group"
  message="Are you sure you want to delete '{selectedGroup?.name}'? This action cannot be undone and will remove all members from the group."
  confirmText="Delete"
  cancelText="Cancel"
  onConfirm={handleDeleteGroup}
  onCancel={() => showDeleteGroupModal = false}
  isLoading={isDeletingGroup}
  danger={true}
/>

