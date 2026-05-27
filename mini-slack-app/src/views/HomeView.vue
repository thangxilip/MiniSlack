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
const detailsOpen = ref(false)

onMounted(async () => {
  if (auth.accessToken) {
    await workspace.loadDashboard(auth.accessToken)
    await workspace.connectRealtime(() => auth.accessToken)
  }
})

watch(
  () => auth.accessToken,
  async (accessToken) => {
    if (accessToken) {
      await workspace.loadDashboard(accessToken)
      await workspace.connectRealtime(() => auth.accessToken)
    } else {
      await workspace.disconnectRealtime()
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

async function selectWorkspace(id: string) {
  if (!auth.accessToken) {
    return
  }

  await workspace.selectWorkspace(auth.accessToken, id)
  sidebarOpen.value = false
}

async function sendMessage(content: string) {
  if (!auth.accessToken || !auth.user) {
    return false
  }

  return await workspace.sendMessage(auth.accessToken, content, auth.user)
}

async function startTyping(conversationId: string) {
  await workspace.startTyping(conversationId)
}

async function stopTyping(conversationId: string) {
  await workspace.stopTyping(conversationId)
}

async function createChannel(
  payload: {
    name: string
    description?: string
    isPrivate: boolean
  },
  done: (created: boolean) => void,
) {
  if (!auth.accessToken) {
    done(false)
    return
  }

  const created = await workspace.createChannel(auth.accessToken, payload)
  if (created) {
    sidebarOpen.value = false
  }

  done(created)
}

async function startDirectMessage(userId: string) {
  if (!auth.accessToken) {
    return
  }

  const started = await workspace.startDirectMessage(auth.accessToken, userId)
  if (started) {
    detailsOpen.value = false
  }
}

async function logout() {
  await workspace.disconnectRealtime()
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
    <div
      v-if="detailsOpen"
      class="fixed inset-0 z-30 bg-slate-950/30 xl:hidden"
      aria-hidden="true"
      @click="detailsOpen = false"
    />

    <WorkspaceSidebar
      :workspaces="workspace.workspaces"
      :workspace-name="workspace.activeWorkspace?.name"
      :channels="workspace.channels"
      :direct-messages="workspace.directMessages"
      :active-workspace-id="workspace.activeWorkspaceId ?? ''"
      :active-channel-id="workspace.activeConversationId ?? ''"
      :creating-channel="workspace.creatingChannel"
      :open="sidebarOpen"
      @select-workspace="selectWorkspace"
      @select-channel="selectChannel"
      @create-channel="createChannel"
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
          :loading="workspace.loadingMessages"
          :sending="workspace.sending"
          :error="workspace.error"
          :typing-users="workspace.activeTypingUsers"
          :on-send-message="sendMessage"
          :on-start-typing="startTyping"
          :on-stop-typing="stopTyping"
          @open-sidebar="sidebarOpen = true"
          @toggle-details="detailsOpen = !detailsOpen"
        />
        <section v-else class="flex min-w-0 flex-1 items-center justify-center bg-white p-6">
          <div class="max-w-sm text-center">
            <p class="text-sm font-semibold text-slate-950">
              {{ workspace.loading ? 'Loading workspace...' : 'No conversations found.' }}
            </p>
            <p v-if="workspace.error" class="mt-2 text-sm text-red-700">{{ workspace.error }}</p>
          </div>
        </section>
        <ActivityPanel
          :workspace-count="workspace.workspaces.length"
          :channel-count="workspace.channels.length"
          :direct-message-count="workspace.directMessages.length"
          :message-count="workspace.messages.length"
          :members="workspace.workspaceMembers"
          :loading-members="workspace.loadingMembers"
          :current-user-id="auth.user?.id ?? ''"
          :starting-direct-message="workspace.startingDirectMessage"
          :open="detailsOpen"
          @start-direct-message="startDirectMessage"
          @close="detailsOpen = false"
        />
      </div>
    </div>
  </main>
</template>
