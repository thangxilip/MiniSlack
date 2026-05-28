<script setup lang="ts">
import { computed, ref } from 'vue'
import { Bell, Hash, MessageCircle, MessagesSquare, X } from 'lucide-vue-next'
import Button from '@/components/ui/Button.vue'
import type { WorkspaceMember } from '@/stores/workspace'
import type { CreatedWorkspaceInviteSummary, WorkspaceInviteSummary, WorkspaceSummary } from '@/lib/workspace-api'
import { cn } from '@/lib/utils'

const props = defineProps<{
  workspaceCount: number
  channelCount: number
  directMessageCount: number
  messageCount: number
  members: WorkspaceMember[]
  invites: WorkspaceInviteSummary[]
  loadingMembers: boolean
  loadingInvites: boolean
  currentUserId: string
  currentUserRole?: WorkspaceSummary['role']
  creatingInvite: boolean
  managingMember: boolean
  startingDirectMessage: boolean
  open: boolean
}>()

const emit = defineEmits<{
  startDirectMessage: [userId: string]
  createInvite: [
    payload: { email: string; role: WorkspaceSummary['role'] },
    done: (invite: CreatedWorkspaceInviteSummary | null) => void,
  ]
  revokeInvite: [inviteId: string]
  updateMemberRole: [userId: string, role: WorkspaceSummary['role']]
  removeMember: [userId: string]
  close: []
}>()

const inviteEmail = ref('')
const inviteRole = ref<WorkspaceSummary['role']>('Member')
const latestInviteUrl = ref('')

const canManageMembers = computed(() => props.currentUserRole === 'Owner' || props.currentUserRole === 'Admin')
const canChangeRoles = computed(() => props.currentUserRole === 'Owner')
const canCreateInvite = computed(
  () => canManageMembers.value && Boolean(inviteEmail.value.trim()) && !props.creatingInvite,
)

function createInvite() {
  if (!canCreateInvite.value) {
    return
  }

  emit(
    'createInvite',
    {
      email: inviteEmail.value,
      role: inviteRole.value,
    },
    (invite) => {
      if (!invite) {
        return
      }

      latestInviteUrl.value = invite.acceptUrl
      inviteEmail.value = ''
      inviteRole.value = 'Member'
    },
  )
}

async function copyInviteUrl(url: string) {
  await navigator.clipboard?.writeText(url)
}
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
              <div v-if="canChangeRoles && member.id !== currentUserId" class="mt-2">
                <select
                  class="h-8 w-full rounded-md border border-slate-200 bg-white px-2 text-xs font-semibold text-slate-700 outline-none focus:ring-2 focus:ring-emerald-600"
                  :value="member.role"
                  :disabled="managingMember"
                  aria-label="Update member role"
                  @change="
                    $emit(
                      'updateMemberRole',
                      member.id,
                      ($event.target as HTMLSelectElement).value as WorkspaceSummary['role'],
                    )
                  "
                >
                  <option value="Member">Member</option>
                  <option value="Admin">Admin</option>
                  <option value="Owner">Owner</option>
                </select>
              </div>
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
            <Button
              v-if="canManageMembers && member.id !== currentUserId"
              variant="ghost"
              size="icon"
              class="h-8 w-8 text-red-700"
              :disabled="managingMember"
              aria-label="Remove member"
              @click="$emit('removeMember', member.id)"
            >
              <X class="h-4 w-4" />
            </Button>
          </article>
        </div>
      </section>

      <section v-if="canManageMembers" class="mt-5">
        <div class="mb-2 flex items-center justify-between">
          <h3 class="text-sm font-bold text-slate-950">Invites</h3>
          <span class="text-xs font-semibold text-slate-500">{{ invites.length }}</span>
        </div>

        <form class="rounded-lg border border-slate-200 bg-white p-3" @submit.prevent="createInvite">
          <input
            v-model="inviteEmail"
            type="email"
            class="h-9 w-full rounded-md border border-slate-200 px-2 text-sm text-slate-950 outline-none placeholder:text-slate-400 focus:ring-2 focus:ring-emerald-600"
            placeholder="name@company.com"
          />
          <div class="mt-2 flex gap-2">
            <select
              v-model="inviteRole"
              class="h-9 min-w-0 flex-1 rounded-md border border-slate-200 bg-white px-2 text-sm text-slate-900 outline-none focus:ring-2 focus:ring-emerald-600"
            >
              <option value="Member">Member</option>
              <option v-if="currentUserRole === 'Owner'" value="Admin">Admin</option>
            </select>
            <Button type="submit" size="sm" :disabled="!canCreateInvite">
              {{ creatingInvite ? 'Inviting' : 'Invite' }}
            </Button>
          </div>
          <button
            v-if="latestInviteUrl"
            type="button"
            class="mt-3 w-full truncate rounded-md bg-emerald-50 px-2 py-2 text-left text-xs font-semibold text-emerald-900"
            @click="copyInviteUrl(latestInviteUrl)"
          >
            {{ latestInviteUrl }}
          </button>
        </form>

        <p v-if="loadingInvites" class="mt-2 rounded-md bg-white px-3 py-4 text-sm text-slate-500">
          Loading invites...
        </p>
        <div v-else class="mt-2 space-y-2">
          <article
            v-for="invite in invites"
            :key="invite.id"
            class="rounded-lg border border-slate-200 bg-white p-3"
          >
            <div class="flex items-start justify-between gap-2">
              <div class="min-w-0">
                <p class="truncate text-sm font-bold text-slate-950">{{ invite.email }}</p>
                <p class="text-xs text-slate-500">
                  {{ invite.role }} - expires {{ new Date(invite.expiresAtUtc).toLocaleDateString() }}
                </p>
              </div>
              <Button
                v-if="!invite.acceptedAtUtc && !invite.revokedAtUtc"
                variant="ghost"
                size="sm"
                @click="$emit('revokeInvite', invite.id)"
              >
                Revoke
              </Button>
            </div>
            <p v-if="invite.acceptedAtUtc" class="mt-2 text-xs font-semibold text-emerald-700">
              Accepted
            </p>
            <p v-else-if="invite.revokedAtUtc" class="mt-2 text-xs font-semibold text-red-700">
              Revoked
            </p>
          </article>
          <p v-if="!invites.length" class="rounded-md bg-white px-3 py-4 text-sm text-slate-500">
            No pending invites.
          </p>
        </div>
      </section>

      <p class="mt-5 text-xs text-slate-500">
        Across {{ workspaceCount }} workspace{{ workspaceCount === 1 ? '' : 's' }}
      </p>
    </div>
  </aside>
</template>
