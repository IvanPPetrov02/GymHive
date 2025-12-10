<script lang="ts">
  import { onMount } from 'svelte';
  import { getApiBase } from '../api';
  import { getAccessToken } from '../auth';
  import LoadingSpinner from '../components/ui/LoadingSpinner.svelte';

  interface Notification {
    id: number;
    userId: string;
    title: string;
    message: string;
    type: string;
    isRead: boolean;
    createdAt: string;
    relatedEntityType?: string;
    relatedEntityId?: number;
  }

  let notifications: Notification[] = [];
  let loading = true;
  let error = '';
  let filter: 'all' | 'unread' = 'all';

  async function fetchNotifications() {
    try {
      loading = true;
      error = '';
      const token = await getAccessToken();
      const apiBase = getApiBase();

      console.log('[Notifications] Token:', token ? `${token.substring(0, 20)}...` : 'NULL');
      console.log('[Notifications] API Base:', apiBase);

      if (!token) {
        throw new Error('No authentication token found. Please log in again.');
      }

      const endpoint = filter === 'unread' 
        ? `${apiBase}/api/notifications/unread`
        : `${apiBase}/api/notifications`;

      console.log('[Notifications] Fetching:', endpoint);

      const response = await fetch(endpoint, {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      console.log('[Notifications] Response status:', response.status);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('[Notifications] Error response:', errorText);
        throw new Error(`Failed to fetch notifications: ${response.status} ${response.statusText}`);
      }

      notifications = await response.json();
      console.log('[Notifications] Received notifications:', notifications.length);
    } catch (err: any) {
      error = err.message || 'Failed to load notifications';
      console.error('Error fetching notifications:', err);
    } finally {
      loading = false;
    }
  }

  async function markAsRead(notificationId: number) {
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/notifications/${notificationId}/read`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to mark notification as read');
      }

      // Update local state
      notifications = notifications.map(n => 
        n.id === notificationId ? { ...n, isRead: true } : n
      );
    } catch (err: any) {
      console.error('Error marking notification as read:', err);
    }
  }

  async function markAllAsRead() {
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/notifications/mark-all-read`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to mark all notifications as read');
      }

      await fetchNotifications();
    } catch (err: any) {
      error = err.message || 'Failed to mark all as read';
    }
  }

  async function deleteNotification(notificationId: number) {
    try {
      const token = await getAccessToken();
      const apiBase = getApiBase();

      const response = await fetch(`${apiBase}/api/notifications/${notificationId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to delete notification');
      }

      notifications = notifications.filter(n => n.id !== notificationId);
    } catch (err: any) {
      console.error('Error deleting notification:', err);
    }
  }

  function getNotificationIcon(type: string) {
    switch (type) {
      case 'membership':
        return '🎫';
      case 'workout':
        return '💪';
      case 'system':
        return '⚙️';
      case 'social':
        return '👥';
      default:
        return '📢';
    }
  }

  function getNotificationColor(type: string) {
    switch (type) {
      case 'membership':
        return 'bg-purple-100 text-purple-800';
      case 'workout':
        return 'bg-green-100 text-green-800';
      case 'system':
        return 'bg-gray-100 text-gray-800';
      case 'social':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }

  function formatDate(dateString: string) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
    });
  }

  $: filter, fetchNotifications();

  onMount(() => {
    fetchNotifications();
  });
</script>

<div class="min-h-screen bg-gray-50 py-8">
  <div class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Notifications</h1>
      <p class="mt-2 text-gray-600">Stay updated with your gym activities</p>
    </div>

    <div class="bg-white rounded-lg shadow-sm mb-6 p-4 flex items-center justify-between">
      <div class="flex gap-2">
        <button 
          on:click={() => filter = 'all'} 
          class="px-4 py-2 rounded-lg font-medium transition {filter === 'all' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'}"
        >
          All
        </button>
        <button 
          on:click={() => filter = 'unread'} 
          class="px-4 py-2 rounded-lg font-medium transition {filter === 'unread' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'}"
        >
          Unread
        </button>
      </div>
      {#if notifications.some(n => !n.isRead)}
        <button on:click={markAllAsRead} class="text-sm text-blue-600 hover:text-blue-800 font-medium">
          Mark all as read
        </button>
      {/if}
    </div>

    {#if loading}
      <div class="flex justify-center items-center py-12">
        <LoadingSpinner />
      </div>
    {:else if error}
      <div class="bg-red-50 border border-red-200 rounded-lg p-4">
        <p class="text-red-800">{error}</p>
        <button on:click={fetchNotifications} class="mt-2 text-red-600 hover:text-red-800 font-medium">
          Try Again
        </button>
      </div>
    {:else if notifications.length === 0}
      <div class="bg-white rounded-lg shadow-sm p-12 text-center">
        <svg class="mx-auto h-16 w-16 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
        <h3 class="mt-4 text-lg font-medium text-gray-900">
          {filter === 'unread' ? 'No unread notifications' : 'No notifications yet'}
        </h3>
        <p class="mt-2 text-gray-500">
          {filter === 'unread' 
            ? 'You\'re all caught up!' 
            : 'Notifications about your workouts, memberships, and more will appear here'}
        </p>
      </div>
    {:else}
      <div class="space-y-3">
        {#each notifications as notification}
          <div class="bg-white rounded-lg shadow-sm p-4 hover:shadow-md transition {!notification.isRead ? 'border-l-4 border-blue-500' : ''}">
            <div class="flex items-start gap-4">
              <div class="flex-shrink-0">
                <span class="text-2xl">{getNotificationIcon(notification.type)}</span>
              </div>
              <div class="flex-1 min-w-0">
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1">
                    <h3 class="text-sm font-semibold text-gray-900">{notification.title}</h3>
                    <p class="mt-1 text-sm text-gray-600">{notification.message}</p>
                  </div>
                  <button on:click={() => deleteNotification(notification.id)} class="text-gray-400 hover:text-red-600 transition">
                    <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
                <div class="mt-2 flex items-center gap-3">
                  <span class="inline-flex px-2 py-1 text-xs font-medium rounded-full {getNotificationColor(notification.type)}">
                    {notification.type}
                  </span>
                  <span class="text-xs text-gray-500">{formatDate(notification.createdAt)}</span>
                  {#if !notification.isRead}
                    <button on:click={() => markAsRead(notification.id)} class="text-xs text-blue-600 hover:text-blue-800 font-medium">
                      Mark as read
                    </button>
                  {/if}
                </div>
              </div>
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>
