<script lang="ts">
  import Router from 'svelte-spa-router';
  import './app.css';
  import Navbar from './lib/components/Navbar.svelte';
  import Home from './lib/pages/Home.svelte';
  import Login from './lib/pages/Login.svelte';
  import Gyms from './lib/pages/Gyms.svelte';

  // '/register' kept as alias to Login for backward compatibility
  const routes = {
    '/': Home,
    '/login': Login,
    '/register': Login,
    '/gyms': Gyms,
  };
</script>

<Navbar />
<main class="full-screen">
  <Router {routes} />
</main>
