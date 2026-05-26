<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { Bell, MessageSquareText } from 'lucide-vue-next'
import ActivityPanel from '@/components/dashboard/ActivityPanel.vue'
import ConversationPanel from '@/components/dashboard/ConversationPanel.vue'
import UserMenu from '@/components/dashboard/UserMenu.vue'
import WorkspaceSidebar from '@/components/dashboard/WorkspaceSidebar.vue'
import Button from '@/components/ui/Button.vue'
import { useAuthStore } from '@/stores/auth'
import { useWorkspaceStore } from '@/stores/workspace'

const auth = useAuthStore()
const workspace = useWorkspaceStore()
const router = useRouter()
const sidebarOpen = ref(false)

onMounted(() => {
  if (auth.accessToken) {
    workspace.loadDashboard(auth.accessToken)
  }
})

watch(
  () => auth.accessToken,
  (accessToken) => {
    if (accessToken) {
      workspace.loadDashboard(accessToken)
    } else {
      workspace.clear()
    }
  },
)

async function selectChannel(id: string) {
  if (!auth.accessToken) {
    return
  }

  await workspace.selectConversation(auth.accessToken, id)
  sidebarOpen.value = false
}

async function sendMessage(content: string) {
  if (!auth.accessToken) {
    return
  }

  await workspace.sendMessage(auth.accessToken, content)
}

async function logout() {
  await auth.logout()
  workspace.clear()
  await router.replace({ name: 'login' })
}
</script>

<template>
  <main class="flex h-screen overflow-hidden bg-slate-100 text-slate-950">
    <div
      v-if="sidebarOpen"
      class="fixed inset-0 z-30 bg-slate-950/30 lg:hidden"
      aria-hidden="true"
      @click="sidebarOpen = false"
    />

    <WorkspaceSidebar
      :workspace-name="workspace.activeWorkspace?.name"
      :channels="workspace.channels"
      :active-channel-id="workspace.activeConversationId ?? ''"
      :open="sidebarOpen"
      @select-channel="selectChannel"
      @close="sidebarOpen = false"
    />

    <div class="flex min-w-0 flex-1 flex-col">
      <header
        class="flex h-16 shrink-0 items-center justify-between border-b border-slate-200 bg-white px-4"
      >
        <div class="flex min-w-0 items-center gap-3">
          <div class="grid h-9 w-9 place-items-center rounded-md bg-emerald-700 text-white">
            <MessageSquareText class="h-5 w-5" />
          </div>
          <div class="min-w-0">
            <p class="truncate text-sm font-bold text-slate-950">MiniSlack Dashboard</p>
            <p class="truncate text-xs text-slate-500">Team chat workspace</p>
          </div>
        </div>

        <div class="flex items-center gap-1 sm:gap-2">
          <Button variant="ghost" size="icon" aria-label="Notifications">
            <Bell class="h-5 w-5" />
          </Button>
          <UserMenu :user="auth.user" :loading="auth.loading" @logout="logout" />
        </div>
      </header>

      <div class="flex min-h-0 flex-1">
        <ConversationPanel
          v-if="workspace.activeChannel"
          :channel="workspace.activeChannel"
          :messages="workspace.activeMessages"
          :sending="workspace.sending"
          @open-sidebar="sidebarOpen = true"
          @send-message="sendMessage"
        />
        <section v-else class="flex min-w-0 flex-1 items-center justify-center bg-white p-6">
          <p class="text-sm text-slate-500">
            {{ workspace.loading ? 'Loading workspace...' : 'No channels found.' }}
          </p>
        </section>
        <ActivityPanel />
      </div>
    </div>
  </main>
</template>
