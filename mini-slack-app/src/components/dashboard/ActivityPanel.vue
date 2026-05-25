<script setup lang="ts">
import { Bell, CheckCircle2, Clock3 } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
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
          <CheckCircle2 class="h-4 w-4 text-emerald-700" />
          Dashboard milestone
        </div>
        <p class="mt-2 text-sm leading-6 text-slate-600">
          Google login is ready. The next step is connecting live workspace data.
        </p>
      </div>

      <div class="rounded-lg border border-slate-200 bg-white p-4">
        <div class="flex items-center gap-2 text-sm font-bold text-slate-950">
          <Clock3 class="h-4 w-4 text-amber-600" />
          Pending API work
        </div>
        <p class="mt-2 text-sm leading-6 text-slate-600">
          Channels, messages, and direct messages are currently backed by local mock data.
        </p>
      </div>
    </div>
  </aside>
</template>
