<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  Hash,
  Info,
  LockKeyhole,
  Menu,
  MessageCircle,
  Paperclip,
  SendHorizontal,
  Smile,
} from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
import type { WorkspaceChannel, WorkspaceMessage } from '@/stores/workspace'

const props = defineProps<{
  channel: WorkspaceChannel
  messages: WorkspaceMessage[]
  loading: boolean
  sending: boolean
  error: string | null
}>()

const emit = defineEmits<{
  openSidebar: []
  sendMessage: [content: string]
}>()

const draft = ref('')

const channelLabel = computed(() => `${props.channel.isPrivate ? '' : '#'}${props.channel.name}`)

function sendMessage() {
  if (!draft.value.trim()) {
    return
  }

  emit('sendMessage', draft.value.trim())
  draft.value = ''
}
</script>

<template>
  <section class="flex min-w-0 flex-1 flex-col bg-white">
    <header class="flex h-16 shrink-0 items-center gap-3 border-b border-slate-200 px-4">
      <Button
        variant="ghost"
        size="icon"
        class="lg:hidden"
        aria-label="Open sidebar"
        @click="$emit('openSidebar')"
      >
        <Menu class="h-5 w-5" />
      </Button>
      <div class="flex min-w-0 flex-1 items-center gap-2">
        <MessageCircle v-if="channel.memberCount <= 2" class="h-5 w-5 text-slate-600" />
        <LockKeyhole v-else-if="channel.isPrivate" class="h-5 w-5 text-slate-600" />
        <Hash v-else class="h-5 w-5 text-slate-600" />
        <div class="min-w-0">
          <h1 class="truncate text-base font-bold text-slate-950">{{ channel.name }}</h1>
          <p class="truncate text-xs text-slate-500">
            {{ channel.memberCount }} member{{ channel.memberCount === 1 ? '' : 's' }}
            <span v-if="channel.description"> - {{ channel.description }}</span>
          </p>
        </div>
      </div>
      <Button variant="ghost" size="icon" aria-label="Channel details">
        <Info class="h-5 w-5" />
      </Button>
    </header>

    <div class="flex-1 overflow-y-auto px-4 py-5">
      <div class="mx-auto flex max-w-3xl flex-col gap-5">
        <div class="rounded-lg border border-slate-200 bg-slate-50 p-4">
          <p class="text-sm font-semibold text-slate-950">{{ channelLabel }}</p>
          <p class="mt-1 text-sm leading-6 text-slate-600">
            This is the start of the {{ channel.name }} channel.
          </p>
        </div>

        <p v-if="error" class="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {{ error }}
        </p>

        <p v-if="loading" class="py-8 text-center text-sm text-slate-500">Loading messages...</p>

        <p
          v-else-if="!messages.length"
          class="rounded-md border border-dashed border-slate-300 px-4 py-8 text-center text-sm text-slate-500"
        >
          No messages yet.
        </p>

        <article v-for="message in messages" :key="message.id" class="flex gap-3">
          <div
            v-if="!message.avatar"
            class="grid h-10 w-10 shrink-0 place-items-center rounded-md bg-emerald-700 text-xs font-bold text-white"
          >
            {{ message.initials }}
          </div>
          <img
            v-else
            :src="message.avatar"
            alt=""
            class="h-10 w-10 shrink-0 rounded-md object-cover"
          />
          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-baseline gap-2">
              <h3 class="text-sm font-bold text-slate-950">{{ message.author }}</h3>
              <time class="text-xs text-slate-500">{{ message.time }}</time>
              <span v-if="message.edited" class="text-xs text-slate-400">edited</span>
            </div>
            <p class="mt-1 text-sm leading-6 text-slate-700">{{ message.body }}</p>
          </div>
        </article>
      </div>
    </div>

    <div class="shrink-0 border-t border-slate-200 bg-white p-4">
      <form
        class="mx-auto max-w-3xl rounded-lg border border-slate-300 bg-white shadow-sm"
        @submit.prevent="sendMessage"
      >
        <textarea
          v-model="draft"
          rows="3"
          :disabled="sending"
          class="block max-h-36 min-h-20 w-full resize-none rounded-t-lg border-0 px-3 py-3 text-sm text-slate-950 outline-none placeholder:text-slate-400"
          :placeholder="`Message #${channel.name}`"
        />
        <div class="flex items-center justify-between border-t border-slate-200 px-2 py-2">
          <div class="flex items-center gap-1">
            <Button variant="ghost" size="icon" aria-label="Attach file">
              <Paperclip class="h-4 w-4" />
            </Button>
            <Button variant="ghost" size="icon" aria-label="Add emoji">
              <Smile class="h-4 w-4" />
            </Button>
          </div>
          <Button size="sm" :disabled="!draft.trim() || sending">
            <SendHorizontal class="h-4 w-4" />
            {{ sending ? 'Sending' : 'Send' }}
          </Button>
        </div>
      </form>
    </div>
  </section>
</template>
