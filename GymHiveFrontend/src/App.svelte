<script lang="ts">
  import Router from 'svelte-spa-router';
  import { wrap } from 'svelte-spa-router/wrap';
  import './app.css';
  import Navbar from './lib/components/Navbar.svelte';
  import Toast from './lib/components/ui/Toast.svelte';
  import Home from './lib/pages/Home.svelte';
  import Feed from './lib/pages/Feed.svelte';
  import Gyms from './lib/pages/Gyms.svelte';
  import GymDetails from './lib/pages/GymDetails.svelte';
  import LoginRegister from './lib/pages/LoginRegister.svelte';
  import ProfileEnhanced from './lib/pages/ProfileEnhanced.svelte';
  import { ensureAuthenticated, requireRole, user, isAuthenticated } from './lib/auth';
  import { get } from 'svelte/store';
  import { replace } from 'svelte-spa-router';
  
  // Service Pages
  import MyMemberships from './lib/pages/MyMemberships.svelte';
  import Workouts from './lib/pages/Workouts.svelte';
  import Notifications from './lib/pages/Notifications.svelte';
  
  // Admin Pages
  import GymsManagement from './lib/pages/admin/GymsManagement.svelte';
  import UsersManagement from './lib/pages/admin/UsersManagement.svelte';
  import GymGroupsManagement from './lib/pages/admin/GymGroupsManagement.svelte';
  
  // Moderator Pages
  import GroupMembers from './lib/pages/moderator/GroupMembers.svelte';
  import MembershipsManagement from './lib/pages/moderator/MembershipsManagement.svelte';

  // Route guard functions
  async function authGuard(detail: any) {
    const isAuthed = await ensureAuthenticated();
    if (!isAuthed) {
      window.location.hash = '#/login';
      return false;
    }
    return true;
  }

  async function adminGuard(detail: any) {
    const isAuthed = await ensureAuthenticated();
    if (!isAuthed) {
      window.location.hash = '#/login';
      return false;
    }
    
    if (!requireRole('Admin')) {
      window.location.hash = '#/';
      return false;
    }
    return true;
  }

  async function moderatorGuard(detail: any) {
    const isAuthed = await ensureAuthenticated();
    if (!isAuthed) {
      window.location.hash = '#/login';
      return false;
    }
    
    if (!requireRole(['Moderator', 'Admin'])) {
      window.location.hash = '#/';
      return false;
    }
    return true;
  }

  const routes = {
    '/': Home,
    '/feed': wrap({
      component: Feed,
      conditions: [authGuard]
    }),
    '/login': LoginRegister,
    '/register': LoginRegister,
    '/gyms': wrap({
      component: Gyms,
      conditions: [authGuard]
    }),
    '/gyms/:id': wrap({
      component: GymDetails,
      conditions: [authGuard]
    }),
    '/profile': wrap({
      component: ProfileEnhanced,
      conditions: [authGuard]
    }),
    '/memberships': wrap({
      component: MyMemberships,
      conditions: [authGuard]
    }),
    '/workouts': wrap({
      component: Workouts,
      conditions: [authGuard]
    }),
    '/notifications': wrap({
      component: Notifications,
      conditions: [authGuard]
    }),
    '/admin/gyms': wrap({
      component: GymsManagement,
      conditions: [adminGuard]
    }),
    '/admin/users': wrap({
      component: UsersManagement,
      conditions: [adminGuard]
    }),
    '/admin/groups': wrap({
      component: GymGroupsManagement,
      conditions: [adminGuard]
    }),
    '/moderator/members': wrap({
      component: GroupMembers,
      conditions: [moderatorGuard]
    }),
    '/moderator/memberships': wrap({
      component: MembershipsManagement,
      conditions: [moderatorGuard]
    }),
  };
</script>

<div class="app-shell flex flex-col min-h-screen w-full">
  <Navbar />
  <main class="flex-1 w-full">
    <Router {routes} />
  </main>
  <Toast />
</div>
// test comment
