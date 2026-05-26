import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createWorkspaceConversation,
  createConversationMessage,
  getConversationMessages,
  getWorkspaceConversations,
  getWorkspaceMembers,
  getWorkspaces,
  startWorkspaceDirectMessage,
  type ConversationSummary,
  type MessageSummary,
  type WorkspaceMemberSummary,
  type WorkspaceSummary,
} from '@/lib/workspace-api'
import type { UserProfile } from '@/lib/auth-api'

export interface WorkspaceChannel {
  id: string
  name: string
  unread: number
  isPrivate: boolean
  memberCount: number
  description?: string | null
}

export interface WorkspaceDirectMessage {
  id: string
  name: string
  unread: number
  memberCount: number
}

export interface WorkspaceMessage {
  id: string
  author: string
  avatar: string | null
  initials: string
  time: string
  body: string
  edited: boolean
  pending: boolean
}

export interface WorkspaceMember {
  id: string
  name: string
  email: string
  avatar: string | null
  initials: string
  status: string | null
  role: string
  joinedAt: string
}

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<WorkspaceSummary[]>([])
  const conversations = ref<ConversationSummary[]>([])
  const messages = ref<MessageSummary[]>([])
  const members = ref<WorkspaceMemberSummary[]>([])
  const activeWorkspaceId = ref<string | null>(null)
  const activeConversationId = ref<string | null>(null)
  const loading = ref(false)
  const loadingMessages = ref(false)
  const loadingMembers = ref(false)
  const creatingChannel = ref(false)
  const startingDirectMessage = ref(false)
  const sending = ref(false)
  const error = ref<string | null>(null)

  const activeWorkspace = computed(
    () => workspaces.value.find((workspace) => workspace.id === activeWorkspaceId.value) ?? null,
  )

  const channels = computed<WorkspaceChannel[]>(() =>
    conversations.value
      .filter((conversation) => conversation.type === 'Channel')
      .map((conversation) => ({
        id: conversation.id,
        name: conversation.name,
        unread: 0,
        isPrivate: conversation.isPrivate,
        memberCount: conversation.memberCount,
        description: conversation.description,
      })),
  )

  const directMessages = computed<WorkspaceDirectMessage[]>(() =>
    conversations.value
      .filter((conversation) => conversation.type === 'Direct')
      .map((conversation) => ({
        id: conversation.id,
        name: conversation.name,
        unread: 0,
        memberCount: conversation.memberCount,
      })),
  )

  const activeConversation = computed(
    () =>
      conversations.value.find((conversation) => conversation.id === activeConversationId.value) ??
      null,
  )

  const activeChannel = computed(() => {
    const conversation = activeConversation.value
    if (!conversation) {
      return null
    }

    return {
      id: conversation.id,
      name: conversation.name,
      unread: 0,
      isPrivate: conversation.isPrivate,
      memberCount: conversation.memberCount,
      description: conversation.description,
    }
  })

  const activeMessages = computed<WorkspaceMessage[]>(() =>
    messages.value.map((message) => ({
      id: message.id,
      author: message.senderDisplayName,
      avatar: message.senderAvatarUrl ?? null,
      initials: initials(message.senderDisplayName),
      time: formatMessageTime(message.createdAtUtc),
      body: message.content,
      edited: Boolean(message.editedAtUtc),
      pending: isOptimisticMessage(message.id),
    })),
  )

  const workspaceMembers = computed<WorkspaceMember[]>(() =>
    members.value.map((member) => ({
      id: member.userId,
      name: member.displayName,
      email: member.email,
      avatar: member.avatarUrl,
      initials: initials(member.displayName),
      status: member.status,
      role: member.role,
      joinedAt: formatJoinedDate(member.joinedAtUtc),
    })),
  )

  async function loadDashboard(accessToken: string) {
    loading.value = true
    error.value = null

    try {
      workspaces.value = await getWorkspaces(accessToken)
      activeWorkspaceId.value = resolveWorkspaceId(activeWorkspaceId.value)

      if (activeWorkspaceId.value) {
        await loadConversations(accessToken, activeWorkspaceId.value)
      } else {
        conversations.value = []
        messages.value = []
        activeConversationId.value = null
      }
    } catch {
      error.value = 'Unable to load workspace data.'
      conversations.value = []
      messages.value = []
    } finally {
      loading.value = false
    }
  }

  async function loadConversations(accessToken: string, workspaceId: string) {
    conversations.value = await getWorkspaceConversations(accessToken, workspaceId)
    await loadMembers(accessToken, workspaceId)
    activeConversationId.value = resolveConversationId(activeConversationId.value)

    if (activeConversationId.value) {
      await loadMessages(accessToken, activeConversationId.value)
    } else {
      messages.value = []
    }
  }

  async function loadMembers(accessToken: string, workspaceId: string) {
    loadingMembers.value = true

    try {
      members.value = await getWorkspaceMembers(accessToken, workspaceId)
    } catch {
      error.value = 'Unable to load workspace members.'
      members.value = []
    } finally {
      loadingMembers.value = false
    }
  }

  async function selectWorkspace(accessToken: string, workspaceId: string) {
    if (workspaceId === activeWorkspaceId.value) {
      return
    }

    activeWorkspaceId.value = workspaceId
    activeConversationId.value = null
    messages.value = []
    loading.value = true
    error.value = null

    try {
      await loadConversations(accessToken, workspaceId)
    } catch {
      error.value = 'Unable to load workspace data.'
    } finally {
      loading.value = false
    }
  }

  async function selectConversation(accessToken: string, conversationId: string) {
    if (conversationId === activeConversationId.value) {
      return
    }

    activeConversationId.value = conversationId
    await loadMessages(accessToken, conversationId)
  }

  async function createChannel(
    accessToken: string,
    request: {
      name: string
      description?: string
      isPrivate: boolean
    },
  ) {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId || !request.name.trim()) {
      return false
    }

    creatingChannel.value = true
    error.value = null

    try {
      const conversation = await createWorkspaceConversation(accessToken, workspaceId, {
        name: request.name.trim(),
        description: request.description?.trim() || null,
        isPrivate: request.isPrivate,
      })

      conversations.value = [...conversations.value, conversation]
      activeConversationId.value = conversation.id
      messages.value = []
      return true
    } catch {
      error.value = 'Unable to create channel.'
      return false
    } finally {
      creatingChannel.value = false
    }
  }

  async function startDirectMessage(accessToken: string, targetUserId: string) {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId) {
      return false
    }

    startingDirectMessage.value = true
    error.value = null

    try {
      const conversation = await startWorkspaceDirectMessage(accessToken, workspaceId, targetUserId)
      const exists = conversations.value.some((candidate) => candidate.id === conversation.id)
      conversations.value = exists
        ? conversations.value.map((candidate) =>
            candidate.id === conversation.id ? conversation : candidate,
          )
        : [...conversations.value, conversation]
      activeConversationId.value = conversation.id
      await loadMessages(accessToken, conversation.id)
      return true
    } catch {
      error.value = 'Unable to start direct message.'
      return false
    } finally {
      startingDirectMessage.value = false
    }
  }

  async function loadMessages(accessToken: string, conversationId: string) {
    loadingMessages.value = true
    error.value = null

    try {
      messages.value = await getConversationMessages(accessToken, conversationId)
    } catch {
      error.value = 'Unable to load messages.'
      messages.value = []
    } finally {
      loadingMessages.value = false
    }
  }

  async function sendMessage(accessToken: string, content: string, sender: UserProfile) {
    const conversationId = activeConversationId.value
    if (!conversationId || !content.trim()) {
      return false
    }

    const optimisticId = `pending-${crypto.randomUUID()}`
    const optimisticMessage: MessageSummary = {
      id: optimisticId,
      conversationId,
      senderId: sender.id,
      senderDisplayName: sender.displayName,
      senderAvatarUrl: sender.avatarUrl ?? null,
      content: content.trim(),
      type: 'Text',
      createdAtUtc: new Date().toISOString(),
      editedAtUtc: null,
    }

    messages.value = [...messages.value, optimisticMessage]
    sending.value = true

    try {
      const message = await createConversationMessage(accessToken, conversationId, content.trim())
      messages.value = messages.value.map((candidate) =>
        candidate.id === optimisticId ? message : candidate,
      )
      error.value = null
      return true
    } catch {
      messages.value = messages.value.filter((candidate) => candidate.id !== optimisticId)
      error.value = 'Unable to send message.'
      return false
    } finally {
      sending.value = false
    }
  }

  function clear() {
    workspaces.value = []
    conversations.value = []
    messages.value = []
    members.value = []
    activeWorkspaceId.value = null
    activeConversationId.value = null
    error.value = null
    loading.value = false
    loadingMessages.value = false
    loadingMembers.value = false
    creatingChannel.value = false
    startingDirectMessage.value = false
    sending.value = false
  }

  function resolveWorkspaceId(currentWorkspaceId: string | null) {
    if (
      currentWorkspaceId &&
      workspaces.value.some((workspace) => workspace.id === currentWorkspaceId)
    ) {
      return currentWorkspaceId
    }

    return workspaces.value[0]?.id ?? null
  }

  function resolveConversationId(currentConversationId: string | null) {
    if (
      currentConversationId &&
      conversations.value.some((conversation) => conversation.id === currentConversationId)
    ) {
      return currentConversationId
    }

    return channels.value[0]?.id ?? directMessages.value[0]?.id ?? null
  }

  return {
    workspaces,
    conversations,
    messages,
    members,
    activeWorkspaceId,
    activeConversationId,
    activeWorkspace,
    activeChannel,
    activeConversation,
    activeMessages,
    workspaceMembers,
    channels,
    directMessages,
    loading,
    loadingMessages,
    loadingMembers,
    creatingChannel,
    startingDirectMessage,
    sending,
    error,
    clear,
    loadDashboard,
    selectWorkspace,
    selectConversation,
    createChannel,
    startDirectMessage,
    sendMessage,
  }
})

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
}

function formatMessageTime(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function formatJoinedDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    month: 'short',
    day: 'numeric',
  }).format(new Date(value))
}

function isOptimisticMessage(id: string) {
  return id.startsWith('pending-')
}
