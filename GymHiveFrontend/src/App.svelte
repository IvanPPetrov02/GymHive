<script lang="ts">
  import Router from 'svelte-spa-router';
  import './app.css';
  import Navbar from './lib/components/Navbar.svelte';
  import Home from './lib/pages/Home.svelte';
  import Gyms from './lib/pages/Gyms.svelte';
  import LoginRegister from './lib/pages/LoginRegister.svelte';
  import ProfileSimple from './lib/pages/ProfileSimple.svelte';

  // '/register' kept as alias to Login for backward compatibility
  const routes = {
    '/': Home,
    '/login': LoginRegister,
    '/register': LoginRegister,
    '/gyms': Gyms,
    '/profile': ProfileSimple,
  };
</script>

<div class="app-shell flex flex-col min-h-screen w-full">
  <Navbar />
  <main class="flex-1 w-full">
    <Router {routes} />
  </main>
</div>
// test comment
