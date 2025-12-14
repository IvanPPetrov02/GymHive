<script lang="ts">
  import type { User } from '../../auth';
  
  export let user: User | null;
</script>

<div class="max-w-2xl space-y-8">
  <div>
    <h2 class="text-2xl font-bold text-gray-800 mb-2">Account Settings</h2>
    <p class="text-gray-600">Manage your account preferences and settings</p>
  </div>
  
  <div class="space-y-4">
    <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
      <div class="flex items-start justify-between">
        <div class="flex-1">
          <h3 class="font-semibold text-gray-800 mb-1">Account Status</h3>
          <p class="text-sm text-gray-600">Your account is currently {user?.isActive ? 'active' : 'inactive'}</p>
        </div>
        <span class="inline-flex items-center gap-2 px-3 py-1 rounded-full text-sm font-medium {user?.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}">
          <span class="w-2 h-2 rounded-full {user?.isActive ? 'bg-green-600' : 'bg-red-600'}"></span>
          {user?.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>
    </div>
    
    <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
      <div class="flex items-start justify-between">
        <div class="flex-1">
          <h3 class="font-semibold text-gray-800 mb-1">Email Notifications</h3>
          <p class="text-sm text-gray-600">Receive updates about your account and activities</p>
        </div>
        <label class="relative inline-flex items-center cursor-pointer">
          <input type="checkbox" checked class="sr-only peer">
          <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
        </label>
      </div>
    </div>
    
    <div class="p-6 border border-gray-200 rounded-lg hover:border-gray-300 transition-colors">
      <div class="flex items-start justify-between">
        <div class="flex-1">
          <h3 class="font-semibold text-gray-800 mb-1">Two-Factor Authentication</h3>
          <p class="text-sm text-gray-600">Add an extra layer of security to your account</p>
        </div>
        <button class="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors">
          Enable
        </button>
      </div>
    </div>
    
    <div class="p-6 border border-red-200 rounded-lg bg-red-50">
      <h3 class="font-semibold text-red-800 mb-2">Danger Zone</h3>
      <p class="text-sm text-red-600 mb-4">Once you delete your account, there is no going back. Please be certain.</p>
      <button class="px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-medium hover:bg-red-700 transition-colors">
        Delete Account
      </button>
    </div>
  </div>
</div>
