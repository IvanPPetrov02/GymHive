<script lang="ts">
  // Home page logic placeholder (future API hooks)
</script>

<div class="w-full min-h-screen bg-gray-50 flex flex-col">
  <!-- Hero Section -->
  <section class="flex flex-col justify-center flex-1 relative bg-gradient-to-r from-blue-600 to-purple-600 text-white">
    <div class="max-w-7xl mx-auto px-6 py-24 w-full">
      <div class="text-center">
        <h1 class="text-5xl md:text-6xl font-bold mb-6 tracking-tight">Welcome to GymHive</h1>
        <p class="text-xl md:text-2xl mb-10 text-blue-100 max-w-3xl mx-auto leading-relaxed">
          Connect with fitness enthusiasts, share your journey, and discover the perfect gym community.
        </p>
        <div class="flex gap-5 justify-center flex-wrap">
          <a href="#/login" class="btn-primary bg-white text-blue-600 hover:bg-gray-100 hover:text-blue-700 px-10 py-4 rounded-xl shadow-lg font-semibold transition">
            Get Started
          </a>
          <a href="#/gyms" class="btn-outline-light px-10 py-4 rounded-xl font-semibold backdrop-blur-sm">
            Explore Gyms
          </a>
        </div>
      </div>
    </div>
    <div class="absolute inset-0 opacity-10 bg-[radial-gradient(circle_at_30%_30%,white,transparent_60%)] pointer-events-none"></div>
  </section>

  <!-- Features -->
  <section class="py-24 bg-gray-50">
    <div class="max-w-7xl mx-auto px-6">
      <h2 class="text-4xl font-bold text-center mb-16 text-gray-800">Why GymHive?</h2>
      <div class="grid md:grid-cols-3 gap-10">
        <div class="card-panel shadow-lg hover:shadow-2xl transition group">
          <div class="text-5xl mb-6 select-none">🏋️</div>
          <h3 class="text-2xl font-bold mb-4 text-gray-800 group-hover:text-blue-600 transition">Find Your Gym</h3>
            <p class="text-gray-600 leading-relaxed">Browse gyms with rich facility details, ratings, and community activity to match your goals.</p>
        </div>
        <div class="card-panel shadow-lg hover:shadow-2xl transition group">
          <div class="text-5xl mb-6 select-none">👥</div>
          <h3 class="text-2xl font-bold mb-4 text-gray-800 group-hover:text-blue-600 transition">Connect</h3>
            <p class="text-gray-600 leading-relaxed">Follow athletes, share progress, and motivate each other through posts and reactions.</p>
        </div>
        <div class="card-panel shadow-lg hover:shadow-2xl transition group">
          <div class="text-5xl mb-6 select-none">📊</div>
          <h3 class="text-2xl font-bold mb-4 text-gray-800 group-hover:text-blue-600 transition">Track Progress</h3>
            <p class="text-gray-600 leading-relaxed">Structured workout logs and analytics (coming soon) to optimize your training path.</p>
        </div>
      </div>
    </div>
  </section>

  <!-- CTA -->
  <section class="bg-gray-900 text-white py-24">
    <div class="max-w-5xl mx-auto px-6 text-center">
      <h2 class="text-4xl font-bold mb-6">Start Your Transformation Today</h2>
      <p class="text-xl text-gray-300 mb-10 leading-relaxed">Join the community built for lifters, athletes, and everyday champions. Your journey deserves the right hive.</p>
      <a href="#/login" class="btn-primary inline-block px-12 py-4 rounded-xl text-lg shadow-lg hover:shadow-xl">
        Create Free Account
      </a>
    </div>
  </section>
</div>
