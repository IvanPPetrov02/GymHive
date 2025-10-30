<script lang="ts">
  import { onMount } from 'svelte';
  import { user, isAuthenticated, isLoading } from '../auth';
  import { requireAuth } from '../auth/auth';
  import { gymsApi, type Gym } from '../services/gyms';
  import { membershipsApi, type CreateMembershipDTO } from '../services/memberships';
  import { gymGroupsApi, type GymGroup } from '../services/gymGroups';
  import { showToast } from '../components/ui/Toast.svelte';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';
  import Modal from '../components/ui/Modal.svelte';
  import { push } from 'svelte-spa-router';

  export let params: { id: string } = { id: '' };
  
  let gym: Gym | null = null;
  let groups: GymGroup[] = [];
  let loading = false;
  let error = '';
  
  // Purchase membership modal
  let showPurchaseModal = false;
  let membershipForm = {
    type: 'Monthly',
    startDate: new Date().toISOString().split('T')[0],
    duration: 1 // months
  };
  let isPurchasing = false;

  $: gymId = parseInt(params.id);
  $: membershipPrice = calculatePrice(membershipForm.type, membershipForm.duration);

  onMount(() => {
    requireAuth(`#/gyms/${params.id}`);
    if ($isAuthenticated) {
      loadGymDetails();
    }
  });

  async function loadGymDetails() {
    if (!gymId || isNaN(gymId)) {
      error = 'Invalid gym ID';
      return;
    }

    loading = true;
    error = '';
    try {
      gym = await gymsApi.getById(gymId);
      // Load gym groups
      groups = await gymGroupsApi.getByGymId(gymId);
    } catch (e: any) {
      console.error('Failed to load gym details:', e);
      error = e.message || 'Failed to load gym details';
    } finally {
      loading = false;
    }
  }

  function calculatePrice(type: string, duration: number): number {
    const basePrice = type === 'Monthly' ? 50 : type === 'Quarterly' ? 120 : 400;
    return basePrice * duration;
  }

  function calculateEndDate(startDate: string, type: string, duration: number): string {
    const start = new Date(startDate);
    const months = type === 'Monthly' ? duration : type === 'Quarterly' ? duration * 3 : duration * 12;
    start.setMonth(start.getMonth() + months);
    return start.toISOString();
  }

  async function handlePurchase() {
    if (!gym || !$user) return;

    isPurchasing = true;
    try {
      const endDate = calculateEndDate(membershipForm.startDate, membershipForm.type, membershipForm.duration);
      
      const membershipData: CreateMembershipDTO = {
        gymId: gym.id,
        membershipType: membershipForm.type,
        startDate: new Date(membershipForm.startDate).toISOString(),
        endDate: endDate,
        price: membershipPrice
      };

      await membershipsApi.create(membershipData);
      showToast('success', 'Membership purchased successfully!');
      showPurchaseModal = false;
      // Redirect to profile to see memberships
      setTimeout(() => push('/profile'), 1000);
    } catch (e: any) {
      showToast('error', e.message || 'Failed to purchase membership');
    } finally {
      isPurchasing = false;
    }
  }

  function formatHours(time?: string): string {
    if (!time) return 'N/A';
    return time;
  }
</script>

{#if $isLoading}
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <LoadingSpinner size="large" />
  </div>
{:else if $isAuthenticated}
  <div class="min-h-screen bg-gray-50">
    {#if loading}
      <div class="min-h-screen flex items-center justify-center">
        <LoadingSpinner size="large" />
      </div>
    {:else if error}
      <div class="min-h-screen flex items-center justify-center">
        <div class="text-center">
          <div class="text-red-600 text-lg mb-4">{error}</div>
          <button on:click={() => push('/gyms')} class="btn-primary px-6 py-3 rounded-lg">
            Back to Gyms
          </button>
        </div>
      </div>
    {:else if gym}
      <!-- Hero Section -->
      <div class="bg-gradient-to-r from-blue-600 to-purple-600 text-white py-20">
        <div class="max-w-7xl mx-auto px-6">
          <div class="flex items-center gap-3 mb-4">
            <button on:click={() => push('/gyms')} class="text-white hover:text-blue-100 transition-colors">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
            </button>
            <h1 class="text-4xl md:text-5xl font-bold">{gym.name}</h1>
          </div>
          <div class="flex items-center gap-4 text-blue-100">
            <div class="flex items-center gap-2">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              {gym.address}
            </div>
            {#if gym.rating}
              <div class="flex items-center gap-2">
                <svg class="w-5 h-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                </svg>
                {gym.rating.toFixed(1)}
              </div>
            {/if}
            {#if gym.memberCount}
              <div class="flex items-center gap-2">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                </svg>
                {gym.memberCount} members
              </div>
            {/if}
          </div>
        </div>
      </div>

      <div class="max-w-7xl mx-auto px-6 py-12">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <!-- Main Content -->
          <div class="lg:col-span-2 space-y-8">
            <!-- Description -->
            {#if gym.description}
              <div class="card-panel p-6">
                <h2 class="text-2xl font-bold text-gray-900 mb-4">About This Gym</h2>
                <p class="text-gray-700 leading-relaxed">{gym.description}</p>
              </div>
            {/if}

            <!-- Facilities -->
            {#if gym.facilities && gym.facilities.length > 0}
              <div class="card-panel p-6">
                <h2 class="text-2xl font-bold text-gray-900 mb-4">Facilities & Amenities</h2>
                <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
                  {#each gym.facilities as facility}
                    <div class="flex items-center gap-2 text-gray-700">
                      <svg class="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                      {facility}
                    </div>
                  {/each}
                </div>
              </div>
            {/if}

            <!-- Groups -->
            {#if groups.length > 0}
              <div class="card-panel p-6">
                <h2 class="text-2xl font-bold text-gray-900 mb-4">Gym Groups</h2>
                <div class="space-y-3">
                  {#each groups as group}
                    <div class="p-4 border border-gray-200 rounded-lg hover:border-blue-300 transition-colors">
                      <div class="flex items-start justify-between">
                        <div>
                          <h3 class="font-semibold text-lg text-gray-900">{group.name}</h3>
                          {#if group.description}
                            <p class="text-sm text-gray-600 mt-1">{group.description}</p>
                          {/if}
                          <div class="flex items-center gap-4 mt-2 text-sm text-gray-500">
                            <span>{group.memberCount} members</span>
                            <span>Mod: {group.moderatorName}</span>
                          </div>
                        </div>
                        <span class="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium
                          {group.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}">
                          {group.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                    </div>
                  {/each}
                </div>
              </div>
            {/if}

            <!-- Hours -->
            <div class="card-panel p-6">
              <h2 class="text-2xl font-bold text-gray-900 mb-4">Hours of Operation</h2>
              <div class="flex items-center gap-4 text-gray-700">
                <div class="flex items-center gap-2">
                  <svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span class="font-medium">Opening:</span>
                  {formatHours(gym.openingTime)}
                </div>
                <div class="flex items-center gap-2">
                  <svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <span class="font-medium">Closing:</span>
                  {formatHours(gym.closingTime)}
                </div>
              </div>
            </div>
          </div>

          <!-- Sidebar -->
          <div class="space-y-6">
            <!-- Contact Info -->
            <div class="card-panel p-6 sticky top-6">
              <h2 class="text-xl font-bold text-gray-900 mb-4">Contact Information</h2>
              
              <div class="space-y-4">
                {#if gym.phoneNumber}
                  <div>
                    <div class="text-sm text-gray-600 mb-1">Phone</div>
                    <a href="tel:{gym.phoneNumber}" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
                      </svg>
                      {gym.phoneNumber}
                    </a>
                  </div>
                {/if}
                
                {#if gym.email}
                  <div>
                    <div class="text-sm text-gray-600 mb-1">Email</div>
                    <a href="mailto:{gym.email}" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                      </svg>
                      {gym.email}
                    </a>
                  </div>
                {/if}
                
                {#if gym.website}
                  <div>
                    <div class="text-sm text-gray-600 mb-1">Website</div>
                    <a href={gym.website} target="_blank" rel="noopener noreferrer" class="text-blue-600 hover:text-blue-700 font-medium flex items-center gap-2">
                      <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
                      </svg>
                      Visit Website
                    </a>
                  </div>
                {/if}
              </div>

              <button
                on:click={() => showPurchaseModal = true}
                class="btn-primary w-full mt-6 py-4 rounded-lg text-lg font-semibold shadow-lg hover:shadow-xl transition-shadow"
              >
                Purchase Membership
              </button>
            </div>
          </div>
        </div>
      </div>
    {/if}
  </div>

  <!-- Purchase Membership Modal -->
  <Modal bind:isOpen={showPurchaseModal} title="Purchase Membership" size="medium">
    <form on:submit|preventDefault={handlePurchase} class="space-y-4">
      <div>
        <label for="type" class="block text-sm font-medium text-gray-700 mb-1">Membership Type</label>
        <select
          id="type"
          bind:value={membershipForm.type}
          disabled={isPurchasing}
          class="no-border-input w-full"
          required
        >
          <option value="Monthly">Monthly</option>
          <option value="Quarterly">Quarterly (3 months)</option>
          <option value="Annual">Annual (12 months)</option>
        </select>
      </div>

      <div>
        <label for="duration" class="block text-sm font-medium text-gray-700 mb-1">
          Duration ({membershipForm.type === 'Monthly' ? 'months' : membershipForm.type === 'Quarterly' ? 'quarters' : 'years'})
        </label>
        <input
          id="duration"
          type="number"
          min="1"
          max="12"
          bind:value={membershipForm.duration}
          disabled={isPurchasing}
          class="no-border-input w-full"
          required
        />
      </div>

      <div>
        <label for="startDate" class="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
        <input
          id="startDate"
          type="date"
          bind:value={membershipForm.startDate}
          disabled={isPurchasing}
          min={new Date().toISOString().split('T')[0]}
          class="no-border-input w-full"
          required
        />
      </div>

      <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div class="flex justify-between items-center">
          <span class="text-gray-700 font-medium">Total Price:</span>
          <span class="text-2xl font-bold text-blue-600">${membershipPrice.toFixed(2)}</span>
        </div>
      </div>
    </form>

    <svelte:fragment slot="footer">
      <button
        type="button"
        on:click={() => showPurchaseModal = false}
        disabled={isPurchasing}
        class="bg-gray-200 text-gray-800 hover:bg-gray-300 transition-colors px-6 py-2 rounded-lg disabled:opacity-50"
      >
        Cancel
      </button>
      <button
        type="button"
        on:click={handlePurchase}
        disabled={isPurchasing}
        class="btn-primary px-6 py-2 rounded-lg disabled:opacity-50 flex items-center gap-2"
      >
        {#if isPurchasing}
          <LoadingSpinner size="small" color="white" />
        {/if}
        Purchase
      </button>
    </svelte:fragment>
  </Modal>
{/if}


