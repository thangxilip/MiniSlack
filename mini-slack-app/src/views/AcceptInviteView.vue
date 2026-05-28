<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Button from '@/components/ui/Button.vue'
import { useAuthStore } from '@/stores/auth'
import { useWorkspaceStore } from '@/stores/workspace'

const pendingInviteTokenKey = 'minislack.pendingInviteToken'
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const workspace = useWorkspaceStore()
const loading = ref(true)
const accepted = ref(false)
const error = ref('')

const token = computed(() => {
  const queryToken = typeof route.query.token === 'string' ? route.query.token : ''
  return queryToken || sessionStorage.getItem(pendingInviteTokenKey) || ''
})

onMounted(async () => {
  if (!token.value) {
    error.value = 'Invite token is missing.'
    loading.value = false
    return
  }

  const signedIn = await auth.initialize()
  if (!signedIn) {
    sessionStorage.setItem(pendingInviteTokenKey, token.value)
    await router.replace({ name: 'login' })
    return
  }

  if (!auth.accessToken) {
    error.value = 'Your session is not ready. Please try again.'
    loading.value = false
    return
  }

  const result = await workspace.acceptInvite(auth.accessToken, token.value)
  if (!result) {
    error.value = workspace.error ?? 'Unable to accept invite.'
    loading.value = false
    return
  }

  sessionStorage.removeItem(pendingInviteTokenKey)
  accepted.value = true
  loading.value = false
})
</script>

<template>
  <main class="grid min-h-screen place-items-center bg-slate-100 p-6 text-slate-950">
    <section class="w-full max-w-md rounded-lg border border-slate-200 bg-white p-6 text-center shadow-sm">
      <h1 class="text-xl font-bold">
        {{ loading ? 'Checking invite' : accepted ? 'Invite accepted' : 'Invite unavailable' }}
      </h1>
      <p class="mt-2 text-sm leading-6 text-slate-600">
        {{
          loading
            ? 'Opening your workspace invite.'
            : accepted
              ? 'You can now open the workspace from your dashboard.'
              : error
        }}
      </p>
      <Button v-if="accepted" class="mt-5" @click="router.replace({ name: 'home' })">
        Open dashboard
      </Button>
      <Button v-else-if="!loading" class="mt-5" variant="ghost" @click="router.replace({ name: 'home' })">
        Back to dashboard
      </Button>
    </section>
  </main>
</template>
