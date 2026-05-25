<script setup lang="ts">
import { Hash, LockKeyhole, Plus, Search, UsersRound, X } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
import type { DashboardChannel, DashboardDm } from '@/lib/dashboard-data'
import { cn } from '@/lib/utils'

defineProps<{
  channels: DashboardChannel[]
  directMessages: DashboardDm[]
  activeChannelId: string
  open: boolean
}>()

defineEmits<{
  selectChannel: [id: string]
  close: []
}>()

function statusClass(status: DashboardDm['status']) {
  return {
    online: 'bg-emerald-500',
    away: 'bg-amber-500',
    offline: 'bg-slate-300',
  }[status]
}
</script>

<template>
  <aside
    :class="
      cn(
        'fixed inset-y-0 left-0 z-40 flex w-80 flex-col border-r border-slate-200 bg-white transition-transform duration-200 lg:static lg:z-auto lg:w-72 lg:translate-x-0',
        open ? 'translate-x-0' : '-translate-x-full',
      )
    "
  >
    <div class="flex h-16 items-center justify-between border-b border-slate-200 px-4">
      <div>
        <p class="text-xs font-bold uppercase text-slate-500">Workspace</p>
        <h2 class="text-base font-bold text-slate-950">MiniSlack</h2>
      </div>
      <Button
        variant="ghost"
        size="icon"
        class="lg:hidden"
        aria-label="Close sidebar"
        @click="$emit('close')"
      >
        <X class="h-4 w-4" />
      </Button>
    </div>

    <div class="p-3">
      <label
        class="flex h-10 items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 text-sm text-slate-500"
      >
        <Search class="h-4 w-4" />
        <input
          class="min-w-0 flex-1 bg-transparent text-slate-900 outline-none placeholder:text-slate-500"
          placeholder="Search workspace"
        />
      </label>
    </div>

    <nav class="flex-1 overflow-y-auto px-3 pb-4">
      <div class="mb-5">
        <div class="mb-2 flex items-center justify-between px-2">
          <p class="text-xs font-bold uppercase text-slate-500">Channels</p>
          <Button variant="ghost" size="icon" class="h-7 w-7" aria-label="Add channel">
            <Plus class="h-4 w-4" />
          </Button>
        </div>
        <div class="space-y-1">
          <button
            v-for="channel in channels"
            :key="channel.id"
            type="button"
            :class="
              cn(
                'flex h-9 w-full items-center gap-2 rounded-md px-2 text-left text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-950',
                activeChannelId === channel.id && 'bg-emerald-50 text-emerald-900',
              )
            "
            @click="$emit('selectChannel', channel.id)"
          >
            <LockKeyhole v-if="channel.isPrivate" class="h-4 w-4" />
            <Hash v-else class="h-4 w-4" />
            <span class="min-w-0 flex-1 truncate">{{ channel.name }}</span>
            <span
              v-if="channel.unread"
              class="grid h-5 min-w-5 place-items-center rounded-full bg-emerald-700 px-1.5 text-xs font-bold text-white"
            >
              {{ channel.unread }}
            </span>
          </button>
        </div>
      </div>

      <div>
        <div class="mb-2 flex items-center gap-2 px-2">
          <UsersRound class="h-4 w-4 text-slate-500" />
          <p class="text-xs font-bold uppercase text-slate-500">Direct messages</p>
        </div>
        <div class="space-y-1">
          <button
            v-for="dm in directMessages"
            :key="dm.id"
            type="button"
            class="flex h-9 w-full items-center gap-2 rounded-md px-2 text-left text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-950"
          >
            <span
              class="relative grid h-6 w-6 place-items-center rounded-full bg-slate-200 text-[10px] font-bold text-slate-700"
            >
              {{
                dm.name
                  .split(' ')
                  .map((part) => part[0])
                  .join('')
              }}
              <span
                :class="
                  cn(
                    'absolute -bottom-0.5 -right-0.5 h-2.5 w-2.5 rounded-full border-2 border-white',
                    statusClass(dm.status),
                  )
                "
              />
            </span>
            <span class="min-w-0 flex-1 truncate">{{ dm.name }}</span>
            <span v-if="dm.unread" class="h-2 w-2 rounded-full bg-emerald-700" />
          </button>
        </div>
      </div>
    </nav>
  </aside>
</template>
