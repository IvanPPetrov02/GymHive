<script lang="ts">
  export let facilities: string[] = [];
</script>

{#if facilities && facilities.length > 0}
  <div class="card-panel p-6">
    <h2 class="text-2xl font-bold text-gray-900 mb-4">Facilities & Amenities</h2>
    <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
      {#each facilities as facility}
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
