// Version: 1.0.1 - Country-scale load testing ready
// ci: trivial code comment to trigger pipeline
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
