<script lang="ts" context="module">
  import { writable } from 'svelte/store';
  
  export type ToastType = 'success' | 'error' | 'warning' | 'info';
  
  export interface Toast {
    id: string;
    type: ToastType;
    message: string;
    duration?: number;
  }
  
  export const toasts = writable<Toast[]>([]);
  
  export function showToast(type: ToastType, message: string, duration: number = 5000) {
    const id = Math.random().toString(36).substring(7);
    const toast: Toast = { id, type, message, duration };
    
    toasts.update(t => [...t, toast]);
    
    if (duration > 0) {
      setTimeout(() => {
        removeToast(id);
      }, duration);
    }
  }
  
  export function removeToast(id: string) {
    toasts.update(t => t.filter(toast => toast.id !== id));
  }
</script>

<script lang="ts">
  import { fade, fly } from 'svelte/transition';
  
  const icons = {
    success: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z',
    error: 'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
    warning: 'M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z',
    info: 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z'
  };
  
  const colors = {
    success: 'bg-green-50 text-green-800 border-green-200',
    error: 'bg-red-50 text-red-800 border-red-200',
    warning: 'bg-yellow-50 text-yellow-800 border-yellow-200',
    info: 'bg-blue-50 text-blue-800 border-blue-200'
  };
  
  const iconColors = {
    success: 'text-green-600',
    error: 'text-red-600',
    warning: 'text-yellow-600',
    info: 'text-blue-600'
  };
</script>

<div class="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-md">
  {#each $toasts as toast (toast.id)}
    <div 
      class="flex items-start gap-3 p-4 rounded-xl shadow-lg border {colors[toast.type]}"
      transition:fly="{{ y: -20, duration: 300 }}"
    >
      <svg 
        class="w-6 h-6 flex-shrink-0 {iconColors[toast.type]}" 
        fill="none" 
        stroke="currentColor" 
        viewBox="0 0 24 24"
      >
        <path 
          stroke-linecap="round" 
          stroke-linejoin="round" 
          stroke-width="2" 
          d={icons[toast.type]}
        />
      </svg>
      
      <div class="flex-1">
        <p class="text-sm font-medium">{toast.message}</p>
      </div>
      
      <button 
        on:click={() => removeToast(toast.id)}
        class="text-gray-400 hover:text-gray-600 transition-colors flex-shrink-0"
        aria-label="Close"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  {/each}
</div>
