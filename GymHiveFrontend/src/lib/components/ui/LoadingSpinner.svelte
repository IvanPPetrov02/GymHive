<script lang="ts">
  export let size: 'small' | 'medium' | 'large' = 'medium';
  export let color: string = 'blue';
  
  const sizes = {
    small: 'h-4 w-4 border-2',
    medium: 'h-8 w-8 border-3',
    large: 'h-12 w-12 border-4'
  };
</script>

<div class="flex items-center justify-center">
  <div 
    class="animate-spin rounded-full border-t-transparent {sizes[size]}"
    style="border-color: {color}; border-top-color: transparent;"
  ></div>
</div>
