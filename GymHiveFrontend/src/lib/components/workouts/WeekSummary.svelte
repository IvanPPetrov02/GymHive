<script lang="ts">
  export let visitsThisWeek: number;
  export let daysVisited: number;
</script>

<div class="mt-6 bg-white rounded-lg shadow-sm p-6">
  <h3 class="text-lg font-semibold text-gray-900 mb-4">Week Summary</h3>
  <div class="flex items-center space-x-8">
    <div>
      <p class="text-sm text-gray-600">Visits This Week</p>
      <p class="text-3xl font-bold text-blue-600">{visitsThisWeek}</p>
    </div>
    <div>
      <p class="text-sm text-gray-600">Days Visited</p>
      <p class="text-3xl font-bold text-green-600">{daysVisited} / 7</p>
    </div>
  </div>
</div>
