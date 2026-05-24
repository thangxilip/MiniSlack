<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
</script>

<template>
  <main class="login-view">
    <section class="login-panel">
      <p class="eyebrow">MiniSlack</p>
      <h1>Sign in to your workspace</h1>
      <p class="summary">Continue with your Google account to open MiniSlack.</p>
      <button type="button" class="primary-action" :disabled="auth.loading" @click="auth.startLogin">
        Continue with Google
      </button>
    </section>
  </main>
</template>

<style scoped>
.login-view {
  display: grid;
  min-height: 100vh;
  place-items: center;
  background: #f7f8fb;
  color: #1f2430;
}

.login-panel {
  width: min(100% - 32px, 420px);
  padding: 32px;
  border: 1px solid #dfe3ec;
  border-radius: 8px;
  background: #ffffff;
  box-shadow: 0 18px 60px rgb(31 36 48 / 10%);
}

.eyebrow {
  margin: 0 0 12px;
  color: #506073;
  font-size: 0.82rem;
  font-weight: 700;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  font-size: 1.75rem;
  line-height: 1.15;
}

.summary {
  margin: 12px 0 24px;
  color: #5d6878;
  line-height: 1.5;
}

.primary-action {
  width: 100%;
  min-height: 44px;
  border: 0;
  border-radius: 6px;
  background: #1f7a5c;
  color: #ffffff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.primary-action:disabled {
  cursor: wait;
  opacity: 0.68;
}
</style>
