<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function logout() {
  await auth.logout()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <main class="home-view">
    <header class="topbar">
      <strong>MiniSlack</strong>
      <button type="button" class="ghost-action" :disabled="auth.loading" @click="logout">Sign out</button>
    </header>

    <section class="workspace">
      <div class="profile">
        <img v-if="auth.user?.avatarUrl" :src="auth.user.avatarUrl" alt="" />
        <div v-else class="avatar-fallback">{{ auth.user?.displayName?.charAt(0) ?? 'M' }}</div>
        <div>
          <h1>{{ auth.user?.displayName }}</h1>
          <p>{{ auth.user?.email }}</p>
        </div>
      </div>
    </section>
  </main>
</template>

<style scoped>
.home-view {
  min-height: 100vh;
  background: #f7f8fb;
  color: #1f2430;
}

.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 64px;
  padding: 0 24px;
  border-bottom: 1px solid #dfe3ec;
  background: #ffffff;
}

.ghost-action {
  min-height: 36px;
  border: 1px solid #c9d1df;
  border-radius: 6px;
  background: #ffffff;
  color: #1f2430;
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}

.ghost-action:disabled {
  cursor: wait;
  opacity: 0.68;
}

.workspace {
  width: min(100% - 32px, 960px);
  margin: 40px auto;
}

.profile {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 24px;
  border: 1px solid #dfe3ec;
  border-radius: 8px;
  background: #ffffff;
}

img,
.avatar-fallback {
  width: 56px;
  height: 56px;
  border-radius: 50%;
}

img {
  object-fit: cover;
}

.avatar-fallback {
  display: grid;
  place-items: center;
  background: #1f7a5c;
  color: #ffffff;
  font-weight: 800;
}

h1 {
  margin: 0 0 4px;
  font-size: 1.35rem;
}

p {
  margin: 0;
  color: #5d6878;
}
</style>
