import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  createConversationMessage,
  getConversationMessages,
  getWorkspaceConversations,
  getWorkspaces,
  type ConversationSummary,
  type MessageSummary,
  type WorkspaceSummary,
} from '@/lib/workspace-api'

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
}

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<WorkspaceSummary[]>([])
  const conversations = ref<ConversationSummary[]>([])
  const messages = ref<MessageSummary[]>([])
  const activeWorkspaceId = ref<string | null>(null)
  const activeConversationId = ref<string | null>(null)
  const loading = ref(false)
  const loadingMessages = ref(false)
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
    activeConversationId.value = resolveConversationId(activeConversationId.value)

    if (activeConversationId.value) {
      await loadMessages(accessToken, activeConversationId.value)
    } else {
      messages.value = []
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

  async function sendMessage(accessToken: string, content: string) {
    const conversationId = activeConversationId.value
    if (!conversationId || !content.trim()) {
      return
    }

    sending.value = true

    try {
      const message = await createConversationMessage(accessToken, conversationId, content.trim())
      messages.value = [...messages.value, message]
      error.value = null
    } catch {
      error.value = 'Unable to send message.'
    } finally {
      sending.value = false
    }
  }

  function clear() {
    workspaces.value = []
    conversations.value = []
    messages.value = []
    activeWorkspaceId.value = null
    activeConversationId.value = null
    error.value = null
    loading.value = false
    loadingMessages.value = false
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
    activeWorkspaceId,
    activeConversationId,
    activeWorkspace,
    activeChannel,
    activeConversation,
    activeMessages,
    channels,
    directMessages,
    loading,
    loadingMessages,
    sending,
    error,
    clear,
    loadDashboard,
    selectWorkspace,
    selectConversation,
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
