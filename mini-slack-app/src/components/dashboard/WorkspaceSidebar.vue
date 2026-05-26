<script setup lang="ts">
import { computed, nextTick, ref, useTemplateRef } from 'vue'
import { Hash, LockKeyhole, MessageCircle, Plus, Search, UsersRound, X } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
import type { WorkspaceChannel, WorkspaceDirectMessage } from '@/stores/workspace'
import type { WorkspaceSummary } from '@/lib/workspace-api'
import { cn } from '@/lib/utils'

const props = defineProps<{
  workspaces: WorkspaceSummary[]
  workspaceName?: string
  channels: WorkspaceChannel[]
  directMessages: WorkspaceDirectMessage[]
  activeWorkspaceId: string
  activeChannelId: string
  creatingChannel: boolean
  open: boolean
}>()

const emit = defineEmits<{
  selectWorkspace: [id: string]
  selectChannel: [id: string]
  createChannel: [
    payload: { name: string; description?: string; isPrivate: boolean },
    done: (created: boolean) => void,
  ]
  close: []
}>()

const showCreateChannel = ref(false)
const channelName = ref('')
const channelDescription = ref('')
const isPrivate = ref(false)
const nameInput = useTemplateRef<HTMLInputElement>('nameInput')

const canCreate = computed(() => Boolean(channelName.value.trim()) && !props.creatingChannel)

async function openCreateChannel() {
  showCreateChannel.value = true
  await nextTick()
  nameInput.value?.focus()
}

function cancelCreateChannel() {
  showCreateChannel.value = false
  channelName.value = ''
  channelDescription.value = ''
  isPrivate.value = false
}

async function createChannel() {
  if (!canCreate.value) {
    return
  }

  emit(
    'createChannel',
    {
      name: channelName.value,
      description: channelDescription.value,
      isPrivate: isPrivate.value,
    },
    (created) => {
      if (created) {
        cancelCreateChannel()
      }
    },
  )
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
      <div class="min-w-0">
        <p class="text-xs font-bold uppercase text-slate-500">Workspace</p>
        <h2 class="truncate text-base font-bold text-slate-950">
          {{ workspaceName ?? 'MiniSlack' }}
        </h2>
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
      <div v-if="workspaces.length > 1" class="mb-3">
        <select
          class="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-medium text-slate-900 outline-none focus:ring-2 focus:ring-emerald-600"
          :value="activeWorkspaceId"
          aria-label="Select workspace"
          @change="$emit('selectWorkspace', ($event.target as HTMLSelectElement).value)"
        >
          <option v-for="workspace in workspaces" :key="workspace.id" :value="workspace.id">
            {{ workspace.name }}
          </option>
        </select>
      </div>
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
          <div class="flex items-center gap-1">
            <span class="text-xs font-semibold text-slate-400">{{ channels.length }}</span>
            <Button
              variant="ghost"
              size="icon"
              class="h-7 w-7"
              aria-label="Create channel"
              @click="openCreateChannel"
            >
              <Plus class="h-4 w-4" />
            </Button>
          </div>
        </div>
        <form
          v-if="showCreateChannel"
          class="mb-2 rounded-md border border-slate-200 bg-slate-50 p-2"
          @submit.prevent="createChannel"
        >
          <input
            ref="nameInput"
            v-model="channelName"
            class="h-9 w-full rounded-md border border-slate-200 bg-white px-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:ring-2 focus:ring-emerald-600"
            maxlength="120"
            placeholder="Channel name"
          />
          <input
            v-model="channelDescription"
            class="mt-2 h-9 w-full rounded-md border border-slate-200 bg-white px-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:ring-2 focus:ring-emerald-600"
            maxlength="512"
            placeholder="Description"
          />
          <label class="mt-2 flex items-center gap-2 px-1 text-sm font-medium text-slate-700">
            <input
              v-model="isPrivate"
              type="checkbox"
              class="h-4 w-4 rounded border-slate-300 accent-emerald-700"
            />
            Private channel
          </label>
          <div class="mt-3 flex items-center justify-end gap-2">
            <Button variant="ghost" size="sm" @click="cancelCreateChannel">Cancel</Button>
            <Button type="submit" size="sm" :disabled="!canCreate">
              {{ creatingChannel ? 'Creating' : 'Create' }}
            </Button>
          </div>
        </form>
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
          <p v-if="!channels.length" class="px-2 py-3 text-sm text-slate-500">No channels yet.</p>
        </div>
      </div>

      <div>
        <div class="mb-2 flex items-center gap-2 px-2">
          <UsersRound class="h-4 w-4 text-slate-500" />
          <p class="text-xs font-bold uppercase text-slate-500">Direct messages</p>
        </div>
        <div class="space-y-1">
          <button
            v-for="directMessage in directMessages"
            :key="directMessage.id"
            type="button"
            :class="
              cn(
                'flex h-9 w-full items-center gap-2 rounded-md px-2 text-left text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-950',
                activeChannelId === directMessage.id && 'bg-emerald-50 text-emerald-900',
              )
            "
            @click="$emit('selectChannel', directMessage.id)"
          >
            <MessageCircle class="h-4 w-4" />
            <span class="min-w-0 flex-1 truncate">{{ directMessage.name }}</span>
            <span
              v-if="directMessage.unread"
              class="grid h-5 min-w-5 place-items-center rounded-full bg-emerald-700 px-1.5 text-xs font-bold text-white"
            >
              {{ directMessage.unread }}
            </span>
          </button>
          <p v-if="!directMessages.length" class="px-2 py-3 text-sm text-slate-500">
            No direct messages yet.
          </p>
        </div>
      </div>
    </nav>
  </aside>
</template>
