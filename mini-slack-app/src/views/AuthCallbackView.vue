<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const error = ref(false)
const pendingInviteTokenKey = 'minislack.pendingInviteToken'

onMounted(async () => {
  const signedIn = await auth.refreshSession()

  if (signedIn) {
    const pendingInviteToken = sessionStorage.getItem(pendingInviteTokenKey)
    if (pendingInviteToken) {
      await router.replace({ name: 'accept-invite', query: { token: pendingInviteToken } })
      return
    }

    await router.replace({ name: 'home' })
    return
  }

  error.value = true
})
</script>

<template>
  <main class="callback-view">
    <section class="status-panel">
      <h1>{{ error ? 'Sign-in failed' : 'Signing you in' }}</h1>
      <p>
        {{
          error
            ? 'The session could not be completed. Please try signing in again.'
            : 'Finishing the secure handoff from Google.'
        }}
      </p>
      <RouterLink v-if="error" class="retry-link" :to="{ name: 'login' }">Back to sign in</RouterLink>
    </section>
  </main>
</template>

<style scoped>
.callback-view {
  display: grid;
  min-height: 100vh;
  place-items: center;
  background: #f7f8fb;
  color: #1f2430;
}

.status-panel {
  width: min(100% - 32px, 420px);
  padding: 28px;
  border: 1px solid #dfe3ec;
  border-radius: 8px;
  background: #ffffff;
  text-align: center;
}

h1 {
  margin: 0 0 10px;
  font-size: 1.45rem;
}

p {
  margin: 0;
  color: #5d6878;
  line-height: 1.5;
}

.retry-link {
  display: inline-flex;
  margin-top: 20px;
  color: #1f7a5c;
  font-weight: 700;
}
</style>
