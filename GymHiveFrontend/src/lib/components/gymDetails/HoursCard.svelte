<script lang="ts">
  export let openingTime: string | undefined;
  export let closingTime: string | undefined;
  
  function formatHours(time?: string): string {
    if (!time) return 'N/A';
    return time;
  }
</script>

<div class="card-panel p-6">
  <h2 class="text-2xl font-bold text-gray-900 mb-4">Hours of Operation</h2>
  <div class="flex items-center gap-4 text-gray-700">
    <div class="flex items-center gap-2">
      <svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <span class="font-medium">Opening:</span>
      {formatHours(openingTime)}
    </div>
    <div class="flex items-center gap-2">
      <svg class="w-5 h-5 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <span class="font-medium">Closing:</span>
      {formatHours(closingTime)}
    </div>
  </div>
</div>
