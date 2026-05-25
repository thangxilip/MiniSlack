<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { Bell, MessageSquareText } from 'lucide-vue-next'
import ActivityPanel from '@/components/dashboard/ActivityPanel.vue'
import ConversationPanel from '@/components/dashboard/ConversationPanel.vue'
import UserMenu from '@/components/dashboard/UserMenu.vue'
import WorkspaceSidebar from '@/components/dashboard/WorkspaceSidebar.vue'
import Button from '@/components/ui/Button.vue'
import { channels, directMessages, messagesByChannel } from '@/lib/dashboard-data'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const activeChannelId = ref(channels[0]?.id ?? 'general')
const sidebarOpen = ref(false)

const activeChannel = computed(
  () => channels.find((channel) => channel.id === activeChannelId.value) ?? channels[0],
)
const activeMessages = computed(() => messagesByChannel[activeChannelId.value] ?? [])

function selectChannel(id: string) {
  activeChannelId.value = id
  sidebarOpen.value = false
}

async function logout() {
  await auth.logout()
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
      :channels="channels"
      :direct-messages="directMessages"
      :active-channel-id="activeChannelId"
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
          v-if="activeChannel"
          :channel="activeChannel"
          :messages="activeMessages"
          @open-sidebar="sidebarOpen = true"
        />
        <ActivityPanel />
      </div>
    </div>
  </main>
</template>
