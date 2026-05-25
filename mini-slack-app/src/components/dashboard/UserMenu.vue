<script setup lang="ts">
import {
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuPortal,
  DropdownMenuRoot,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from 'reka-ui'
import { LogOut, Settings, UserRound } from 'lucide-vue-next'
import type { UserProfile } from '@/lib/auth-api'

defineProps<{
  user: UserProfile | null
  loading: boolean
}>()

defineEmits<{
  logout: []
}>()
</script>

<template>
  <DropdownMenuRoot>
    <DropdownMenuTrigger
      class="inline-flex h-10 max-w-56 items-center gap-2 rounded-md px-2 text-left transition-colors hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 focus-visible:ring-offset-2"
    >
      <img
        v-if="user?.avatarUrl"
        :src="user.avatarUrl"
        alt=""
        class="h-8 w-8 rounded-full object-cover"
      />
      <span
        v-else
        class="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-emerald-700 text-xs font-bold text-white"
      >
        {{ user?.displayName?.charAt(0) ?? 'M' }}
      </span>
      <span class="hidden min-w-0 sm:block">
        <span class="block truncate text-sm font-bold text-slate-950">{{
          user?.displayName ?? 'MiniSlack user'
        }}</span>
        <span class="block truncate text-xs text-slate-500">{{ user?.email ?? 'Signed in' }}</span>
      </span>
    </DropdownMenuTrigger>

    <DropdownMenuPortal>
      <DropdownMenuContent
        :side-offset="8"
        align="end"
        class="z-50 w-64 rounded-lg border border-slate-200 bg-white p-1 shadow-xl outline-none"
      >
        <DropdownMenuLabel class="px-3 py-2">
          <span class="block truncate text-sm font-bold text-slate-950">{{
            user?.displayName ?? 'MiniSlack user'
          }}</span>
          <span class="block truncate text-xs font-normal text-slate-500">{{
            user?.email ?? 'Signed in'
          }}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator class="my-1 h-px bg-slate-200" />
        <DropdownMenuItem
          class="flex h-9 cursor-default select-none items-center gap-2 rounded-md px-2 text-sm text-slate-700 outline-none data-[highlighted]:bg-slate-100"
        >
          <UserRound class="h-4 w-4" />
          Profile
        </DropdownMenuItem>
        <DropdownMenuItem
          class="flex h-9 cursor-default select-none items-center gap-2 rounded-md px-2 text-sm text-slate-700 outline-none data-[highlighted]:bg-slate-100"
        >
          <Settings class="h-4 w-4" />
          Preferences
        </DropdownMenuItem>
        <DropdownMenuSeparator class="my-1 h-px bg-slate-200" />
        <DropdownMenuItem
          class="flex h-9 cursor-default select-none items-center gap-2 rounded-md px-2 text-sm text-red-700 outline-none data-[highlighted]:bg-red-50"
          :disabled="loading"
          @select="$emit('logout')"
        >
          <LogOut class="h-4 w-4" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenuPortal>
  </DropdownMenuRoot>
</template>
