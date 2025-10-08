import { mount } from 'svelte'
import './app.css'
import App from './App.svelte'
import { initAuth } from './lib/auth' // ...existing code...

// Initialize Auth0 (non-blocking)
initAuth()

const app = mount(App, {
  target: document.getElementById('app')!,
})

export default app
