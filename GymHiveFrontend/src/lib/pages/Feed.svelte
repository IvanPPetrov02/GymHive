<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { user, isAuthenticated } from '../auth';
  import { requireAuth } from '../auth';
  import { gymsApi, type Gym } from '../services/gyms';
  import { membershipsApi, type Membership } from '../services/memberships';
  import { gymGroupsApi, type GymGroup } from '../services/gymGroups';
  import { showToast } from '../components/ui/Toast.svelte';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';
  import QRCodeButton from '../components/feed/QRCodeButton.svelte';
  import QRCodeModal from '../components/feed/QRCodeModal.svelte';
  import StatCard from '../components/feed/StatCard.svelte';
  import MembershipCard from '../components/feed/MembershipCard.svelte';
  import GymCard from '../components/feed/GymCard.svelte';
  import { push } from 'svelte-spa-router';
  import QRCode from 'qrcode';

  let gyms: Gym[] = [];
  let myMemberships: Membership[] = [];
  let gymGroups: GymGroup[] = [];
  let userGroupIds: Set<number> = new Set(); // Track which groups user has joined
  let loading = true;
  let error: string | null = null;
  let joiningGroupId: number | null = null;
  let showQRModal = false;
  let qrCodeDataUrl = '';
  let qrInterval: number | null = null;
  let countdown = 30;
  let countdownInterval: number | null = null;
  
  const membershipIcon = '<svg class="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>';
  const gymsIcon = '<svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/></svg>';
  const profileIcon = '<svg class="w-8 h-8 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/></svg>';

  onMount(() => {
    requireAuth('#/feed');
    if ($isAuthenticated) {
      loadFeedData();
    }
  });

  onDestroy(() => {
    if (qrInterval) {
      clearInterval(qrInterval);
    }
    if (countdownInterval) {
      clearInterval(countdownInterval);
    }
  });

  async function generateQRCode() {
    try {
      const userId = $user?.uuid;
      const now = new Date();
      const timestamp = now.getTime();
      const date = now.toISOString().split('T')[0]; // YYYY-MM-DD
      const time = now.toTimeString().split(' ')[0]; // HH:MM:SS
      
      const qrData = JSON.stringify({ 
        userId, 
        timestamp,
        date,
        time
      });
      
      // QR code generated (removed noisy debug log)
      qrCodeDataUrl = await QRCode.toDataURL(qrData, { width: 300, margin: 2 });
    } catch (err) {
      console.error('Failed to generate QR code:', err);
    }
  }

  function startCountdown() {
    countdown = 30;
    countdownInterval = setInterval(() => {
      countdown--;
      if (countdown <= 0) {
        generateQRCode();
        countdown = 30;
      }
    }, 1000);
  }

  function openQRModal() {
    showQRModal = true;
    generateQRCode();
    startCountdown();
    // Regenerate QR code every 30 seconds
    qrInterval = setInterval(() => {
      generateQRCode();
      countdown = 30;
    }, 30000);
  }

  function closeQRModal() {
    showQRModal = false;
    if (qrInterval) {
      clearInterval(qrInterval);
      qrInterval = null;
    }
    if (countdownInterval) {
      clearInterval(countdownInterval);
      countdownInterval = null;
    }
  }

  async function loadFeedData() {
    loading = true;
    error = null;
    try {
      // Load gyms, user's memberships, and gym groups in parallel
      const [gymsData, membershipsData, groupsData] = await Promise.all([
        gymsApi.getAll().catch(() => []),
        membershipsApi.getMyMemberships().catch(() => []),
        gymGroupsApi.getAll().catch(() => [])
      ]);
      gyms = gymsData.slice(0, 6); // Show first 6 gyms
      myMemberships = membershipsData;
      gymGroups = groupsData;
      
      // Load which groups the user is in
      await loadUserGroups();
    } catch (e: any) {
      console.error('Failed to load feed data:', e);
      error = e.message || 'Failed to load feed';
    } finally {
      loading = false;
    }
  }

  async function loadUserGroups() {
    if (!$user?.uuid) return;
    
    // Check each group to see if user is a member
    const membershipChecks = await Promise.all(
      gymGroups.map(async (group) => {
        try {
          const members = await gymGroupsApi.getMembers(group.id);
          return members.some(m => m.userId === $user?.uuid) ? group.id : null;
        } catch {
          return null;
        }
      })
    );
    
    userGroupIds = new Set(membershipChecks.filter(id => id !== null) as number[]);
  }

  async function handleJoinGroup(groupId: number) {
    if (!$user?.uuid) {
      showToast('error', 'User not authenticated');
      return;
    }
    joiningGroupId = groupId;
    try {
      await gymGroupsApi.joinGroup(groupId, $user.uuid);
      userGroupIds.add(groupId);
      userGroupIds = userGroupIds; // Trigger reactivity
      showToast('success', 'Successfully joined the group!');
    } catch (e: any) {
      showToast('error', e.message || 'Failed to join group');
    } finally {
      joiningGroupId = null;
    }
  }

  async function handleLeaveGroup(groupId: number) {
    if (!$user?.uuid) {
      showToast('error', 'User not authenticated');
      return;
    }
    joiningGroupId = groupId;
    try {
      await gymGroupsApi.leaveGroup(groupId, $user.uuid);
      userGroupIds.delete(groupId);
      userGroupIds = userGroupIds; // Trigger reactivity
      showToast('success', 'Successfully left the group');
    } catch (e: any) {
      showToast('error', e.message || 'Failed to leave group');
    } finally {
      joiningGroupId = null;
    }
  }

  function canJoinGroup(group: GymGroup): boolean {
    // Check if user has active membership at this gym
    return myMemberships.some(m => m.gymId === group.gymId && m.isActive);
  }

  function isUserInGroup(groupId: number): boolean {
    return userGroupIds.has(groupId);
  }

  function viewGym(id: number) {
    push(`/gyms/${id}`);
  }
</script>

<div class="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <!-- Welcome Header -->
    <div class="mb-8">
      <h1 class="text-4xl font-bold text-gray-900 mb-2">
        Welcome back, {$user?.name || 'Member'}! 👋
      </h1>
      <p class="text-lg text-gray-600">Here's what's happening in your fitness journey</p>
    </div>

    <!-- Gym Check-in QR Code Button - Only show if user has memberships -->
    {#if myMemberships.length > 0}
      <div class="mb-8">
        <QRCodeButton on:click={openQRModal} />
      </div>
    {/if}

    {#if loading}
      <div class="flex justify-center items-center py-20">
        <LoadingSpinner size="lg" />
      </div>
    {:else if error}
      <div class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
        <p class="text-red-600 font-medium">{error}</p>
        <button on:click={loadFeedData} class="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition">
          Try Again
        </button>
      </div>
    {:else}
      <!-- Quick Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <StatCard title="Active Memberships" value={myMemberships.length} color="blue" icon={membershipIcon} />
        <StatCard title="Available Gyms" value="{gyms.length}+" color="green" icon={gymsIcon} />
        <StatCard title="Your Profile" value={$user?.role || 'Member'} color="purple" icon={profileIcon} />
      </div>

      <!-- My Active Memberships -->
      {#if myMemberships.length > 0}
        <div class="mb-8">
          <div class="flex justify-between items-center mb-4">
            <h2 class="text-2xl font-bold text-gray-900">My Active Memberships</h2>
            <a href="#/profile" class="text-blue-600 hover:text-blue-700 font-medium text-sm">View All →</a>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each myMemberships.slice(0, 3) as membership}
              <MembershipCard {membership} />
            {/each}
          </div>
        </div>
      {/if}

      <!-- Gym Groups -->
      {#if gymGroups.length > 0}
        <div class="mb-8">
          <div class="flex justify-between items-center mb-4">
            <h2 class="text-2xl font-bold text-gray-900">Join Gym Groups</h2>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each gymGroups.slice(0, 6) as group}
              <div class="bg-white rounded-xl shadow-md hover:shadow-lg transition-shadow p-6">
                <div class="mb-4">
                  <h3 class="text-xl font-bold text-gray-900 mb-1">{group.name}</h3>
                  <p class="text-sm text-blue-600 mb-2">{group.gymName}</p>
                  {#if group.description}
                    <p class="text-sm text-gray-600 mb-2">{group.description}</p>
                  {/if}
                  {#if group.schedule}
                    <p class="text-xs text-gray-500">📅 {group.schedule}</p>
                  {/if}
                </div>
                <div class="flex items-center justify-between mb-4">
                  <span class="text-sm text-gray-600">Max {group.maxMembers} members</span>
                </div>
                {#if isUserInGroup(group.id)}
                  <button
                    on:click={() => handleLeaveGroup(group.id)}
                    disabled={joiningGroupId === group.id}
                    class="w-full bg-red-600 hover:bg-red-700 text-white py-2 rounded-lg transition disabled:opacity-50"
                  >
                    {#if joiningGroupId === group.id}
                      <LoadingSpinner size="small" color="white" />
                    {:else}
                      Leave Group
                    {/if}
                  </button>
                {:else if canJoinGroup(group)}
                  <button
                    on:click={() => handleJoinGroup(group.id)}
                    disabled={joiningGroupId === group.id}
                    class="w-full bg-blue-600 hover:bg-blue-700 text-white py-2 rounded-lg transition disabled:opacity-50"
                  >
                    {#if joiningGroupId === group.id}
                      <LoadingSpinner size="small" color="white" />
                    {:else}
                      Join Group
                    {/if}
                  </button>
                {:else}
                  <div class="text-sm text-gray-500 text-center py-2 bg-gray-100 rounded-lg">
                    Membership required at {group.gymName}
                  </div>
                {/if}
              </div>
            {/each}
          </div>
        </div>
      {/if}

      <!-- Discover Gyms -->
      <div class="mb-8">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-2xl font-bold text-gray-900">Discover Gyms Near You</h2>
          <a href="#/gyms" class="text-blue-600 hover:text-blue-700 font-medium text-sm">Browse All →</a>
        </div>
        {#if gyms.length > 0}
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {#each gyms as gym}
              <GymCard {gym} on:view={(e) => viewGym(e.detail)} />
            {/each}
          </div>
        {:else}
          <div class="bg-gray-50 rounded-xl p-8 text-center">
            <svg class="w-16 h-16 text-gray-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>
            </svg>
            <p class="text-gray-500 font-medium">No gyms available yet</p>
            <p class="text-gray-400 text-sm mt-1">Check back later for new gyms</p>
          </div>
        {/if}
      </div>

      <!-- Quick Actions -->
      <div class="bg-gradient-to-r from-blue-600 to-purple-600 rounded-xl p-8 text-white">
        <h2 class="text-2xl font-bold mb-4">Quick Actions</h2>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <a href="#/gyms" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
            <div>
              <div class="font-semibold">Find Gyms</div>
              <div class="text-sm text-white/80">Discover new locations</div>
            </div>
          </a>
          <a href="#/profile" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"/>
            </svg>
            <div>
              <div class="font-semibold">My Profile</div>
              <div class="text-sm text-white/80">Manage your account</div>
            </div>
          </a>
          <a href="#/profile" class="bg-white/10 backdrop-blur-sm rounded-lg p-4 hover:bg-white/20 transition flex items-center gap-3">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            <div>
              <div class="font-semibold">My Memberships</div>
              <div class="text-sm text-white/80">View active plans</div>
            </div>
          </a>
        </div>
      </div>
    {/if}
  </div>
</div>

<!-- QR Code Modal -->
<QRCodeModal 
  isOpen={showQRModal} 
  {qrCodeDataUrl} 
  {countdown} 
  on:close={closeQRModal} 
/>
