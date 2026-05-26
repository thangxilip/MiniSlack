import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { apiClient } from '@/lib/api-client'
import type { components } from '@/lib/generated/api-schema'

type WorkspaceSummary = components['schemas']['WorkspaceSummary']
type ConversationSummary = components['schemas']['ConversationSummary']
type MessageSummary = components['schemas']['MessageSummary']

export interface WorkspaceChannel {
  id: string
  name: string
  unread: number
  isPrivate: boolean
  memberCount: number
  description?: string | null
}

export interface WorkspaceMessage {
  id: string
  author: string
  avatar: string
  time: string
  body: string
}

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<WorkspaceSummary[]>([])
  const conversations = ref<ConversationSummary[]>([])
  const messages = ref<MessageSummary[]>([])
  const activeWorkspaceId = ref<string | null>(null)
  const activeConversationId = ref<string | null>(null)
  const loading = ref(false)
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

  const activeChannel = computed(
    () => channels.value.find((channel) => channel.id === activeConversationId.value) ?? null,
  )

  const activeMessages = computed<WorkspaceMessage[]>(() =>
    messages.value.map((message) => ({
      id: message.id,
      author: message.senderDisplayName,
      avatar: initials(message.senderDisplayName),
      time: formatMessageTime(message.createdAtUtc),
      body: message.content,
    })),
  )

  async function loadDashboard(accessToken: string) {
    loading.value = true
    error.value = null

    try {
      const { data: loadedWorkspaces } = await apiClient.GET('/workspaces', {
        headers: authHeaders(accessToken),
      })

      workspaces.value = loadedWorkspaces ?? []
      activeWorkspaceId.value = activeWorkspaceId.value ?? workspaces.value[0]?.id ?? null

      if (activeWorkspaceId.value) {
        await loadConversations(accessToken, activeWorkspaceId.value)
      }
    } catch {
      error.value = 'Unable to load workspace data.'
    } finally {
      loading.value = false
    }
  }

  async function loadConversations(accessToken: string, workspaceId: string) {
    const { data } = await apiClient.GET('/workspaces/{workspaceId}/conversations', {
      params: {
        path: {
          workspaceId,
        },
      },
      headers: authHeaders(accessToken),
    })

    conversations.value = data ?? []
    activeConversationId.value = activeConversationId.value ?? channels.value[0]?.id ?? null

    if (activeConversationId.value) {
      await loadMessages(accessToken, activeConversationId.value)
    }
  }

  async function selectConversation(accessToken: string, conversationId: string) {
    activeConversationId.value = conversationId
    await loadMessages(accessToken, conversationId)
  }

  async function loadMessages(accessToken: string, conversationId: string) {
    const { data } = await apiClient.GET('/conversations/{conversationId}/messages', {
      params: {
        path: {
          conversationId,
        },
        query: {
          limit: 50,
        },
      },
      headers: authHeaders(accessToken),
    })

    messages.value = data ?? []
  }

  async function sendMessage(accessToken: string, content: string) {
    const conversationId = activeConversationId.value
    if (!conversationId || !content.trim()) {
      return
    }

    sending.value = true

    try {
      const { data } = await apiClient.POST('/conversations/{conversationId}/messages', {
        params: {
          path: {
            conversationId,
          },
        },
        body: {
          content,
          parentMessageId: null,
        },
        headers: authHeaders(accessToken),
      })

      if (data) {
        messages.value = [...messages.value, data]
      }
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
  }

  return {
    workspaces,
    conversations,
    messages,
    activeWorkspaceId,
    activeConversationId,
    activeWorkspace,
    activeChannel,
    activeMessages,
    channels,
    loading,
    sending,
    error,
    clear,
    loadDashboard,
    selectConversation,
    sendMessage,
  }
})

function authHeaders(accessToken: string) {
  return {
    Authorization: `Bearer ${accessToken}`,
  }
}

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
