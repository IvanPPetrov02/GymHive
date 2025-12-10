<script lang="ts">
  import type { Membership } from '../../services/memberships';
  
  export let membership: Membership;
</script>

<div class="bg-white rounded-xl shadow-md overflow-hidden hover:shadow-lg transition">
  <div class="bg-gradient-to-r from-blue-500 to-purple-600 p-4">
    <h3 class="text-white font-bold text-lg">{membership.gym?.name || 'Gym Membership'}</h3>
    <p class="text-blue-100 text-sm">{membership.type}</p>
  </div>
  <div class="p-4">
    <div class="flex justify-between items-center mb-2">
      <span class="text-gray-600 text-sm">Valid Until:</span>
      <span class="font-semibold text-gray-900">{new Date(membership.endDate).toLocaleDateString()}</span>
    </div>
    <div class="flex justify-between items-center">
      <span class="text-gray-600 text-sm">Status:</span>
      <span class="px-2 py-1 bg-green-100 text-green-700 rounded-full text-xs font-medium">Active</span>
    </div>
  </div>
</div>
