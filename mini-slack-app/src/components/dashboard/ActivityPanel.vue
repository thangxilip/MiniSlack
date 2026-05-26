<script setup lang="ts">
import { Bell, Hash, MessageCircle, MessagesSquare } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'

defineProps<{
  workspaceCount: number
  channelCount: number
  directMessageCount: number
  messageCount: number
}>()
</script>

<template>
  <aside class="hidden w-80 shrink-0 border-l border-slate-200 bg-slate-50 xl:flex xl:flex-col">
    <div class="flex h-16 items-center justify-between border-b border-slate-200 px-4">
      <div>
        <p class="text-xs font-bold uppercase text-slate-500">Today</p>
        <h2 class="text-base font-bold text-slate-950">Activity</h2>
      </div>
      <Button variant="ghost" size="icon" aria-label="Notifications">
        <Bell class="h-5 w-5" />
      </Button>
    </div>

    <div class="space-y-3 p-4">
      <div class="rounded-lg border border-slate-200 bg-white p-4">
        <div class="flex items-center gap-2 text-sm font-bold text-slate-950">
          <Hash class="h-4 w-4 text-emerald-700" />
          Channels
        </div>
        <p class="mt-2 text-3xl font-bold text-slate-950">{{ channelCount }}</p>
      </div>

      <div class="rounded-lg border border-slate-200 bg-white p-4">
        <div class="flex items-center gap-2 text-sm font-bold text-slate-950">
          <MessageCircle class="h-4 w-4 text-cyan-700" />
          Direct messages
        </div>
        <p class="mt-2 text-3xl font-bold text-slate-950">{{ directMessageCount }}</p>
      </div>

      <div class="rounded-lg border border-slate-200 bg-white p-4">
        <div class="flex items-center gap-2 text-sm font-bold text-slate-950">
          <MessagesSquare class="h-4 w-4 text-indigo-700" />
          Loaded messages
        </div>
        <p class="mt-2 text-3xl font-bold text-slate-950">{{ messageCount }}</p>
        <p class="mt-1 text-xs text-slate-500">
          Across {{ workspaceCount }} workspace{{ workspaceCount === 1 ? '' : 's' }}
        </p>
      </div>
    </div>
  </aside>
</template>
