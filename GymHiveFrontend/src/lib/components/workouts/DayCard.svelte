<script lang="ts">
  import type { GymVisit } from '../../pages/Workouts.svelte';
  
  export let date: Date;
  export let dayName: string;
  export let visit: GymVisit | undefined;
  export let isToday: boolean;
  export let isFuture: boolean;
</script>

<div 
  class="bg-white rounded-lg shadow-sm p-4 min-h-[120px] transition-all
         {isFuture ? 'opacity-50' : ''}
         {isToday ? 'ring-2 ring-blue-500' : ''}
         {visit ? 'bg-green-50 border-2 border-green-500' : ''}"
>
  <div class="text-center">
    <div class="text-xs font-semibold text-gray-500 uppercase mb-1">
      {dayName}
    </div>
    <div class="text-2xl font-bold {isToday ? 'text-blue-600' : 'text-gray-900'} mb-2">
      {date.getDate()}
    </div>
    
    {#if visit}
      <div class="mt-2">
        <div class="inline-flex items-center px-2 py-1 bg-green-600 text-white text-xs font-medium rounded-full">
          <svg class="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd"/>
          </svg>
          Visited
        </div>
        <p class="text-xs text-gray-600 mt-1">{visit.gymName}</p>
      </div>
    {/if}
  </div>
</div>
