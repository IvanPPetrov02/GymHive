<script lang="ts">
  export let title: string;
  export let value: string | number;
  export let color: 'blue' | 'green' | 'purple' = 'blue';
  export let icon: string;
  
  const colors = {
    blue: {
      border: 'border-blue-500',
      bg: 'bg-blue-100',
      text: 'text-blue-600'
    },
    green: {
      border: 'border-green-500',
      bg: 'bg-green-100',
      text: 'text-green-600'
    },
    purple: {
      border: 'border-purple-500',
      bg: 'bg-purple-100',
      text: 'text-purple-600'
    }
  };
  
  const colorClasses = colors[color];
</script>

<div class="bg-white rounded-xl shadow-md p-6 border-l-4 {colorClasses.border}">
  <div class="flex items-center justify-between">
    <div>
      <p class="text-gray-500 text-sm font-medium">{title}</p>
      <p class="text-3xl font-bold text-gray-900 mt-1">{value}</p>
    </div>
    <div class="{colorClasses.bg} rounded-full p-3">
      {@html icon}
    </div>
  </div>
</div>
