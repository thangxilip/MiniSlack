<script setup lang="ts">
import { Bell, Hash, MessageCircle, MessagesSquare, X } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
import type { WorkspaceMember } from '@/stores/workspace'
import { cn } from '@/lib/utils'

defineProps<{
  workspaceCount: number
  channelCount: number
  directMessageCount: number
  messageCount: number
  members: WorkspaceMember[]
  loadingMembers: boolean
  currentUserId: string
  startingDirectMessage: boolean
  open: boolean
}>()

defineEmits<{
  startDirectMessage: [userId: string]
  close: []
}>()
</script>

<template>
  <aside
    :class="
      cn(
        'fixed inset-y-0 right-0 z-40 flex w-80 flex-col border-l border-slate-200 bg-slate-50 transition-transform duration-200 xl:static xl:z-auto xl:translate-x-0',
        open ? 'translate-x-0' : 'translate-x-full',
      )
    "
  >
    <div class="flex h-16 items-center justify-between border-b border-slate-200 px-4">
      <div>
        <p class="text-xs font-bold uppercase text-slate-500">Today</p>
        <h2 class="text-base font-bold text-slate-950">Workspace</h2>
      </div>
      <Button
        variant="ghost"
        size="icon"
        class="xl:hidden"
        aria-label="Close details"
        @click="$emit('close')"
      >
        <X class="h-5 w-5" />
      </Button>
      <Button variant="ghost" size="icon" class="hidden xl:inline-flex" aria-label="Notifications">
        <Bell class="h-5 w-5" />
      </Button>
    </div>

    <div class="min-h-0 flex-1 overflow-y-auto p-4">
      <div class="grid grid-cols-3 gap-2">
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <Hash class="h-4 w-4 text-emerald-700" />
          <p class="mt-2 text-xl font-bold text-slate-950">{{ channelCount }}</p>
          <p class="text-xs text-slate-500">Channels</p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <MessageCircle class="h-4 w-4 text-cyan-700" />
          <p class="mt-2 text-xl font-bold text-slate-950">{{ directMessageCount }}</p>
          <p class="text-xs text-slate-500">DMs</p>
        </div>
        <div class="rounded-lg border border-slate-200 bg-white p-3">
          <MessagesSquare class="h-4 w-4 text-indigo-700" />
          <p class="mt-2 text-xl font-bold text-slate-950">{{ messageCount }}</p>
          <p class="text-xs text-slate-500">Messages</p>
        </div>
      </div>

      <section class="mt-5">
        <div class="mb-2 flex items-center justify-between">
          <h3 class="text-sm font-bold text-slate-950">Members</h3>
          <span class="text-xs font-semibold text-slate-500">{{ members.length }}</span>
        </div>

        <p v-if="loadingMembers" class="rounded-md bg-white px-3 py-4 text-sm text-slate-500">
          Loading members...
        </p>
        <div v-else class="space-y-2">
          <article
            v-for="member in members"
            :key="member.id"
            class="flex items-center gap-3 rounded-lg border border-slate-200 bg-white p-3"
          >
            <img
              v-if="member.avatar"
              :src="member.avatar"
              alt=""
              class="h-9 w-9 shrink-0 rounded-md object-cover"
            />
            <div
              v-else
              class="grid h-9 w-9 shrink-0 place-items-center rounded-md bg-emerald-700 text-xs font-bold text-white"
            >
              {{ member.initials }}
            </div>
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <p class="truncate text-sm font-bold text-slate-950">{{ member.name }}</p>
                <span
                  :class="
                    cn(
                      'h-2 w-2 shrink-0 rounded-full',
                      member.status === 'online' && 'bg-emerald-500',
                      member.status === 'away' && 'bg-amber-500',
                      member.status !== 'online' && member.status !== 'away' && 'bg-slate-300',
                    )
                  "
                />
              </div>
              <p class="truncate text-xs text-slate-500">{{ member.email }}</p>
              <p class="mt-1 text-xs font-medium text-slate-400">
                {{ member.role }} - joined {{ member.joinedAt }}
              </p>
            </div>
            <Button
              v-if="member.id !== currentUserId"
              variant="ghost"
              size="icon"
              class="h-8 w-8"
              :disabled="startingDirectMessage"
              aria-label="Start direct message"
              @click="$emit('startDirectMessage', member.id)"
            >
              <MessageCircle class="h-4 w-4" />
            </Button>
          </article>
        </div>
      </section>

      <p class="mt-5 text-xs text-slate-500">
        Across {{ workspaceCount }} workspace{{ workspaceCount === 1 ? '' : 's' }}
      </p>
    </div>
  </aside>
</template>
