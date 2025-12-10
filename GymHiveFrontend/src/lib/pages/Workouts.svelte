<script context="module" lang="ts">
  export interface GymVisit {
    id: number;
    userId: string;
    gymId: number;
    gymName: string;
    date: string;
    createdAt: string;
  }
</script>

<script lang="ts">
  import { onMount } from 'svelte';
  import { getApiBase } from '../api';
  import { getAccessToken } from '../auth';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';
  import Modal from '../components/ui/Modal.svelte';
  import WeekNavigator from '../components/workouts/WeekNavigator.svelte';
  import DayCard from '../components/workouts/DayCard.svelte';
  import WeekSummary from '../components/workouts/WeekSummary.svelte';

  interface Gym {
    id: number;
    name: string;
  }

  let visits: GymVisit[] = [];
  let gyms: Gym[] = [];
  let loading = true;
  let error = '';
  let showModal = false;
  let currentWeekStart = getWeekStart(new Date());
  let selectedDate = '';
  let selectedGymId = 0;

  const daysOfWeek = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  function getWeekStart(date: Date): Date {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    const day = d.getDay();
    const diff = d.getDate() - day;
    return new Date(d.setDate(diff));
  }

  function getWeekDates(weekStart: Date): Date[] {
    const dates: Date[] = [];
    for (let i = 0; i < 7; i++) {
      const date = new Date(weekStart);
      date.setDate(weekStart.getDate() + i);
      dates.push(date);
    }
    return dates;
  }

  function formatDateForApi(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  function formatDateDisplay(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  function isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
  }

  function isFuture(date: Date): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date > today;
  }

  function hasVisit(date: Date): GymVisit | undefined {
    const dateStr = formatDateForApi(date);
    return visits.find(v => v.date.split('T')[0] === dateStr);
  }

  function canGoBack(): boolean {
    // Disable only if going back would show a week more than 1 month ago
    const oneMonthAgo = new Date();
    oneMonthAgo.setMonth(oneMonthAgo.getMonth() - 1);
    oneMonthAgo.setHours(0, 0, 0, 0);
    const prevWeekStart = new Date(currentWeekStart);
    prevWeekStart.setDate(prevWeekStart.getDate() - 7);
    return prevWeekStart >= oneMonthAgo;
  }

  function canGoForward(): boolean {
    // Disable only if going forward would show a week more than 1 month in the future
    const oneMonthFromNow = new Date();
    oneMonthFromNow.setMonth(oneMonthFromNow.getMonth() + 1);
    oneMonthFromNow.setHours(0, 0, 0, 0);
    const nextWeekStart = new Date(currentWeekStart);
    nextWeekStart.setDate(nextWeekStart.getDate() + 7);
    return nextWeekStart <= oneMonthFromNow;
  }

  function goToPreviousWeek() {
    const newWeek = new Date(currentWeekStart);
    newWeek.setDate(newWeek.getDate() - 7);
    currentWeekStart = newWeek;
    fetchVisits();
  }

  function goToNextWeek() {
    const newWeek = new Date(currentWeekStart);
    newWeek.setDate(newWeek.getDate() + 7);
    currentWeekStart = newWeek;
    fetchVisits();
  }

  function goToCurrentWeek() {
    currentWeekStart = getWeekStart(new Date());
    fetchVisits();
  }

  async function fetchVisits() {
    try {
      loading = true;
      error = '';
      const token = await getAccessToken();
      const apiBase = getApiBase();

      console.log('[Workouts] Token:', token ? `${token.substring(0, 20)}...` : 'NULL');
      console.log('[Workouts] API Base:', apiBase);

      if (!token) {
        throw new Error('No authentication token found. Please log in again.');
      }

      const weekEnd = new Date(currentWeekStart);
      weekEnd.setDate(weekEnd.getDate() + 6);

      const startDate = formatDateForApi(currentWeekStart);
      const endDate = formatDateForApi(weekEnd);

      const url = `${apiBase}/api/workouts/my-workouts?startDate=${startDate}&endDate=${endDate}`;
      console.log('[Workouts] Fetching:', url);

      const response = await fetch(url, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      console.log('[Workouts] Response status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('[Workouts] Error response:', errorText);
        throw new Error(`Failed to fetch gym visits: ${response.status} ${response.statusText}`);
      }

      visits = await response.json();
      console.log('[Workouts] Received visits:', visits.length);
    } catch (err: any) {
      error = err.message || 'Failed to load gym visits';
      console.error('Error fetching visits:', err);
    } finally {
      loading = false;
    }
  }

  async function fetchGyms() {
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/gyms`, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        gyms = await response.json();
      }
    } catch (err) {
      console.error('Error fetching gyms:', err);
    }
  }

  function openModal(date: Date) {
    if (isFuture(date)) {
      return;
    }
    selectedDate = formatDateForApi(date);
    selectedGymId = gyms.length > 0 ? gyms[0].id : 0;
    showModal = true;
  }

  async function logVisit() {
    try {
      if (!selectedGymId || !selectedDate) {
        error = 'Please select a gym and date';
        return;
      }

      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/workouts`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          gymId: selectedGymId,
          date: selectedDate
        })
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to log gym visit');
      }

      showModal = false;
      selectedDate = '';
      selectedGymId = 0;
      await fetchVisits();
    } catch (err: any) {
      error = err.message || 'Failed to log gym visit';
    }
  }

  onMount(() => {
    fetchGyms();
    fetchVisits();
  });

  $: weekDates = getWeekDates(currentWeekStart);
  $: weekLabel = `${formatDateDisplay(weekDates[0])} - ${formatDateDisplay(weekDates[6])}, ${weekDates[0].getFullYear()}`;
</script>

<div class="min-h-screen bg-gray-50 py-8">
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Gym Visits</h1>
      <p class="mt-2 text-gray-600">Track your gym visits by week</p>
    </div>

    {#if error}
      <div class="mb-4 bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{error}</p>
        <button on:click={() => error = ''} class="mt-2 text-red-600 hover:text-red-800 font-medium">
          Dismiss
        </button>
      </div>
    {/if}

    <!-- Week Navigation -->
    <WeekNavigator 
      {weekLabel}
      canGoBack={canGoBack()}
      canGoForward={canGoForward()}
      on:previous={goToPreviousWeek}
      on:next={goToNextWeek}
      on:current={goToCurrentWeek}
    />

    {#if loading}
      <div class="flex justify-center items-center py-12">
        <LoadingSpinner />
      </div>
    {:else}
      <!-- Week Calendar -->
      <div class="grid grid-cols-7 gap-4">
        {#each weekDates as date, index}
          {@const visit = hasVisit(date)}
          {@const future = isFuture(date)}
          {@const today = isToday(date)}
          <DayCard 
            {date}
            dayName={daysOfWeek[index]}
            {visit}
            isToday={today}
            isFuture={future}
          />
        {/each}
      </div>

      <!-- Summary -->
      <WeekSummary visitsThisWeek={visits.length} daysVisited={visits.length} />
    {/if}
  </div>
</div>

<!-- Log Visit Modal -->
{#if showModal}
  <Modal on:close={() => showModal = false}>
    <div class="p-6">
      <h2 class="text-2xl font-bold text-gray-900 mb-4">Log Gym Visit</h2>
      
      <div class="space-y-4">
        <div>
          <label for="gym" class="block text-sm font-medium text-gray-700 mb-1">
            Select Gym
          </label>
          <select 
            id="gym"
            bind:value={selectedGymId}
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            <option value={0}>Select a gym...</option>
            {#each gyms as gym}
              <option value={gym.id}>{gym.name}</option>
            {/each}
          </select>
        </div>

        <div>
          <label for="date" class="block text-sm font-medium text-gray-700 mb-1">
            Date
          </label>
          <input 
            id="date"
            type="date"
            bind:value={selectedDate}
            max={new Date().toISOString().split('T')[0]}
            min={new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0]}
            class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
      </div>

      <div class="mt-6 flex justify-end space-x-3">
        <button 
          on:click={() => showModal = false}
          class="px-4 py-2 border border-gray-300 rounded-lg font-medium text-gray-700 hover:bg-gray-50 transition"
        >
          Cancel
        </button>
        <button 
          on:click={logVisit}
          disabled={!selectedGymId || !selectedDate}
          class="px-4 py-2 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition"
        >
          Log Visit
        </button>
      </div>
    </div>
  </Modal>
{/if}
