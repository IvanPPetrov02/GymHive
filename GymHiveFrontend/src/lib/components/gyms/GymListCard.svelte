<script lang="ts">
  import type { Gym } from '../../services/gyms';
  
  export let gym: Gym;
</script>

<div class="bg-white rounded-2xl shadow-lg hover:shadow-2xl transition overflow-hidden">
  <div class="h-48 bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white text-6xl">
    🏋️
  </div>
  <div class="p-6">
    <h3 class="text-2xl font-bold text-gray-800 mb-2">{gym.name}</h3>
    <p class="text-gray-600 mb-4 flex items-center gap-2">
      <span class="text-lg">📍</span>
      {gym.address}
    </p>
    
    {#if gym.description}
      <p class="text-gray-600 text-sm mb-4 line-clamp-2">{gym.description}</p>
    {/if}

    <div class="flex flex-wrap gap-2 mb-4">
      {#if gym.phoneNumber}
        <span class="px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-xs">📞 {gym.phoneNumber}</span>
      {/if}
      {#if gym.email}
        <span class="px-3 py-1 bg-purple-100 text-purple-700 rounded-full text-xs">✉️ {gym.email}</span>
      {/if}
    </div>

    {#if gym.openingTime && gym.closingTime}
      <p class="text-gray-600 text-sm mb-4">
        ⏰ {gym.openingTime} - {gym.closingTime}
      </p>
    {/if}

    <div class="flex gap-3">
      <slot name="actions" />
    </div>
  </div>
</div>
