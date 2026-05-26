import { apiClient } from '@/lib/api-client'
import type { components } from '@/lib/generated/api-schema'

export type WorkspaceSummary = components['schemas']['WorkspaceSummary']
export type ConversationSummary = components['schemas']['ConversationSummary']
export type MessageSummary = components['schemas']['MessageSummary']

const messageLimit = 50

export async function getWorkspaces(accessToken: string): Promise<WorkspaceSummary[]> {
  const { data, error } = await apiClient.GET('/workspaces', {
    headers: authHeaders(accessToken),
  })

  if (error || !data) {
    throw new Error('Unable to load workspaces.')
  }

  return data
}

export async function getWorkspaceConversations(
  accessToken: string,
  workspaceId: string,
): Promise<ConversationSummary[]> {
  const { data, error } = await apiClient.GET('/workspaces/{workspaceId}/conversations', {
    params: {
      path: {
        workspaceId,
      },
    },
    headers: authHeaders(accessToken),
  })

  if (error || !data) {
    throw new Error('Unable to load conversations.')
  }

  return data
}

export async function getConversationMessages(
  accessToken: string,
  conversationId: string,
): Promise<MessageSummary[]> {
  const { data, error } = await apiClient.GET('/conversations/{conversationId}/messages', {
    params: {
      path: {
        conversationId,
      },
      query: {
        limit: messageLimit,
      },
    },
    headers: authHeaders(accessToken),
  })

  if (error || !data) {
    throw new Error('Unable to load messages.')
  }

  return data
}

export async function createConversationMessage(
  accessToken: string,
  conversationId: string,
  content: string,
): Promise<MessageSummary> {
  const { data, error } = await apiClient.POST('/conversations/{conversationId}/messages', {
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

  if (error || !data) {
    throw new Error('Unable to send message.')
  }

  return data
}

function authHeaders(accessToken: string) {
  return {
    Authorization: `Bearer ${accessToken}`,
  }
}
