<script lang="ts">
  import type { GymGroup } from '../../services/gymGroups';
  
  export let group: GymGroup;
</script>

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
