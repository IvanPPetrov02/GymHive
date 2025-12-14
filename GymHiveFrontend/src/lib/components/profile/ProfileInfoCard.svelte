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

<div class="space-y-6">
  <h2 class="text-2xl font-bold text-gray-800 mb-6">Personal Information</h2>
  
  <div class="grid md:grid-cols-2 gap-6">
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">First Name</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <p class="text-gray-800 font-medium">{user?.name || 'N/A'}</p>
      </div>
    </div>
    
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">Last Name</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <p class="text-gray-800 font-medium">{user?.surname || 'N/A'}</p>
      </div>
    </div>
    
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">Email Address</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <p class="text-gray-800 font-medium">{user?.email || 'N/A'}</p>
      </div>
    </div>
    
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">Account Status</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <span class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium {user?.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
          <span class="w-2 h-2 rounded-full {user?.isActive ? 'bg-green-600' : 'bg-red-600'}"></span>
          {user?.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>
    </div>
    
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">User ID</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <p class="text-gray-600 font-mono text-xs break-all">{user?.uuid || 'N/A'}</p>
      </div>
    </div>
    
    <div class="space-y-2">
      <label class="text-sm font-medium text-gray-500">Member Since</label>
      <div class="p-4 bg-gray-50 rounded-lg border border-gray-200">
        <p class="text-gray-800 font-medium">{formatDate(user?.createdAt)}</p>
      </div>
    </div>
  </div>
</div>
