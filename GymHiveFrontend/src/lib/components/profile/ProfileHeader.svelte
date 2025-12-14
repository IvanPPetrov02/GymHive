<script lang="ts">
  import type { User } from '../../auth';
  
  export let user: User | null;
  
  function formatDate(dateString: string | undefined) {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  }
</script>

<div class="bg-gradient-to-r from-blue-600 to-purple-600 rounded-2xl p-8 mb-8 text-white shadow-xl">
  <div class="flex items-center gap-6">
    <div class="h-24 w-24 rounded-full bg-white/20 backdrop-blur-sm flex items-center justify-center text-4xl font-bold">
      {(user?.name?.[0] || '') + (user?.surname?.[0] || '')}
    </div>
    <div class="flex-1">
      <h1 class="text-3xl font-bold mb-1">{user?.name} {user?.surname}</h1>
      <p class="text-blue-100 text-sm">{user?.email}</p>
      <div class="flex items-center gap-4 mt-3 text-sm">
        <span class="flex items-center gap-1">
          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z" clip-rule="evenodd"/>
          </svg>
          Joined {formatDate(user?.createdAt)}
        </span>
        <span class="flex items-center gap-1 px-3 py-1 rounded-full bg-white/20 backdrop-blur-sm">
          <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
          </svg>
          {user?.role || 'User'}
        </span>
      </div>
    </div>
    <slot name="actions" />
  </div>
</div>
