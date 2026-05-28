import { apiBaseUrl, apiClient } from '@/lib/api-client'
import type { components } from '@/lib/generated/api-schema'

type ApiWorkspaceSummary = components['schemas']['WorkspaceSummary']
type ApiConversationSummary = components['schemas']['ConversationSummary']
type ApiMessageSummary = components['schemas']['MessageSummary']

export interface WorkspaceSummary {
  id: string
  name: string
  slug: string
  role: 'Member' | 'Admin' | 'Owner'
}

export interface ConversationSummary {
  id: string
  workspaceId: string
  type: 'Direct' | 'Channel'
  name: string
  description: string | null
  isPrivate: boolean
  memberCount: number
  createdAtUtc: string
}

export interface MessageSummary {
  id: string
  conversationId: string
  senderId: string
  senderDisplayName: string
  senderAvatarUrl: string | null
  content: string
  type: 'Text' | 'System' | 'File'
  createdAtUtc: string
  editedAtUtc: string | null
}

export interface WorkspaceMemberSummary {
  userId: string
  displayName: string
  email: string
  avatarUrl: string | null
  status: string | null
  role: 'Member' | 'Admin' | 'Owner'
  joinedAtUtc: string
}

export interface WorkspaceInviteSummary {
  id: string
  workspaceId: string
  email: string
  role: WorkspaceSummary['role']
  invitedByUserId: string
  invitedByDisplayName: string
  expiresAtUtc: string
  acceptedAtUtc: string | null
  revokedAtUtc: string | null
  createdAtUtc: string
}

export interface CreatedWorkspaceInviteSummary extends WorkspaceInviteSummary {
  token: string
  acceptUrl: string
}

export interface AcceptWorkspaceInviteResult {
  workspace: WorkspaceSummary
  member: WorkspaceMemberSummary
}

export interface CreateChannelRequest {
  name: string
  description?: string | null
  isPrivate: boolean
}

const messageLimit = 50

export async function getWorkspaces(accessToken: string): Promise<WorkspaceSummary[]> {
  const { data, error } = await apiClient.GET('/workspaces', {
    headers: authHeaders(accessToken),
  })

  if (error || !data) {
    throw new Error('Unable to load workspaces.')
  }

  return data.map(normalizeWorkspace)
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

  return data.map(normalizeConversation)
}

export async function getWorkspaceMembers(
  accessToken: string,
  workspaceId: string,
): Promise<WorkspaceMemberSummary[]> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/members`, {
    credentials: 'include',
    headers: authHeaders(accessToken),
  })

  if (!response.ok) {
    throw new Error('Unable to load workspace members.')
  }

  const data = (await response.json()) as unknown[]
  return data.map(normalizeWorkspaceMember)
}

export async function getWorkspaceInvites(
  accessToken: string,
  workspaceId: string,
): Promise<WorkspaceInviteSummary[]> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/invites`, {
    credentials: 'include',
    headers: authHeaders(accessToken),
  })

  if (!response.ok) {
    throw new Error('Unable to load workspace invites.')
  }

  const data = (await response.json()) as unknown[]
  return data.map(normalizeWorkspaceInvite)
}

export async function createWorkspaceInvite(
  accessToken: string,
  workspaceId: string,
  request: {
    email: string
    role: WorkspaceSummary['role']
  },
): Promise<CreatedWorkspaceInviteSummary> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/invites`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      ...authHeaders(accessToken),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error('Unable to create workspace invite.')
  }

  return normalizeCreatedWorkspaceInvite(await response.json())
}

export async function revokeWorkspaceInvite(
  accessToken: string,
  workspaceId: string,
  inviteId: string,
): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/invites/${inviteId}`, {
    method: 'DELETE',
    credentials: 'include',
    headers: authHeaders(accessToken),
  })

  if (!response.ok) {
    throw new Error('Unable to revoke workspace invite.')
  }
}

export async function acceptWorkspaceInvite(
  accessToken: string,
  token: string,
): Promise<AcceptWorkspaceInviteResult> {
  const response = await fetch(`${apiBaseUrl}/workspace-invites/accept`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      ...authHeaders(accessToken),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ token }),
  })

  if (!response.ok) {
    throw new Error('Unable to accept workspace invite.')
  }

  const data = (await response.json()) as Record<string, unknown>
  return {
    workspace: normalizeWorkspace(data.workspace as ApiWorkspaceSummary),
    member: normalizeWorkspaceMember(data.member),
  }
}

export async function removeWorkspaceMember(
  accessToken: string,
  workspaceId: string,
  targetUserId: string,
): Promise<{ workspaceId: string; userId: string }> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/members/${targetUserId}`, {
    method: 'DELETE',
    credentials: 'include',
    headers: authHeaders(accessToken),
  })

  if (!response.ok) {
    throw new Error('Unable to remove workspace member.')
  }

  const data = (await response.json()) as Record<string, unknown>
  return {
    workspaceId: requireString(asNullableString(data.workspaceId)),
    userId: requireString(asNullableString(data.userId)),
  }
}

export async function updateWorkspaceMemberRole(
  accessToken: string,
  workspaceId: string,
  targetUserId: string,
  role: WorkspaceSummary['role'],
): Promise<WorkspaceMemberSummary> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/members/${targetUserId}/role`, {
    method: 'PATCH',
    credentials: 'include',
    headers: {
      ...authHeaders(accessToken),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ role }),
  })

  if (!response.ok) {
    throw new Error('Unable to update workspace member role.')
  }

  return normalizeWorkspaceMember(await response.json())
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

  return data.map(normalizeMessage)
}

export async function createWorkspaceConversation(
  accessToken: string,
  workspaceId: string,
  request: CreateChannelRequest,
): Promise<ConversationSummary> {
  const { data, error } = await apiClient.POST('/workspaces/{workspaceId}/conversations', {
    params: {
      path: {
        workspaceId,
      },
    },
    body: {
      name: request.name,
      description: request.description,
      type: 2,
      isPrivate: request.isPrivate,
    },
    headers: authHeaders(accessToken),
  })

  if (error || !data) {
    throw new Error('Unable to create channel.')
  }

  return normalizeConversation(data)
}

export async function startWorkspaceDirectMessage(
  accessToken: string,
  workspaceId: string,
  targetUserId: string,
): Promise<ConversationSummary> {
  const response = await fetch(`${apiBaseUrl}/workspaces/${workspaceId}/direct-messages`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      ...authHeaders(accessToken),
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      targetUserId,
    }),
  })

  if (!response.ok) {
    throw new Error('Unable to start direct message.')
  }

  return normalizeConversation(await response.json())
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

  return normalizeMessage(data)
}

function authHeaders(accessToken: string) {
  return {
    Authorization: `Bearer ${accessToken}`,
  }
}

function normalizeWorkspace(workspace: ApiWorkspaceSummary): WorkspaceSummary {
  return {
    id: requireString(workspace.id),
    name: workspace.name ?? 'Untitled workspace',
    slug: workspace.slug ?? '',
    role: normalizeWorkspaceRole(workspace.role),
  }
}

function normalizeConversation(conversation: ApiConversationSummary): ConversationSummary {
  return {
    id: requireString(conversation.id),
    workspaceId: requireString(conversation.workspaceId),
    type: normalizeConversationType(conversation.type),
    name: conversation.name ?? 'direct message',
    description: conversation.description ?? null,
    isPrivate: conversation.isPrivate ?? false,
    memberCount: conversation.memberCount ?? 0,
    createdAtUtc: conversation.createdAtUtc ?? new Date().toISOString(),
  }
}

function normalizeMessage(message: ApiMessageSummary): MessageSummary {
  return {
    id: requireString(message.id),
    conversationId: requireString(message.conversationId),
    senderId: requireString(message.senderId),
    senderDisplayName: message.senderDisplayName ?? 'Unknown user',
    senderAvatarUrl: message.senderAvatarUrl ?? null,
    content: message.content ?? '',
    type: normalizeMessageType(message.type),
    createdAtUtc: message.createdAtUtc ?? new Date().toISOString(),
    editedAtUtc: message.editedAtUtc ?? null,
  }
}

function normalizeWorkspaceMember(value: unknown): WorkspaceMemberSummary {
  const member = value as Record<string, unknown>

  return {
    userId: requireString(asNullableString(member.userId)),
    displayName: asNullableString(member.displayName) ?? 'Unknown user',
    email: asNullableString(member.email) ?? '',
    avatarUrl: asNullableString(member.avatarUrl),
    status: asNullableString(member.status),
    role: normalizeWorkspaceRole(member.role),
    joinedAtUtc: asNullableString(member.joinedAtUtc) ?? new Date().toISOString(),
  }
}

function normalizeWorkspaceInvite(value: unknown): WorkspaceInviteSummary {
  const invite = value as Record<string, unknown>

  return {
    id: requireString(asNullableString(invite.id)),
    workspaceId: requireString(asNullableString(invite.workspaceId)),
    email: asNullableString(invite.email) ?? '',
    role: normalizeWorkspaceRole(invite.role),
    invitedByUserId: requireString(asNullableString(invite.invitedByUserId)),
    invitedByDisplayName: asNullableString(invite.invitedByDisplayName) ?? 'Unknown user',
    expiresAtUtc: asNullableString(invite.expiresAtUtc) ?? new Date().toISOString(),
    acceptedAtUtc: asNullableString(invite.acceptedAtUtc),
    revokedAtUtc: asNullableString(invite.revokedAtUtc),
    createdAtUtc: asNullableString(invite.createdAtUtc) ?? new Date().toISOString(),
  }
}

function normalizeCreatedWorkspaceInvite(value: unknown): CreatedWorkspaceInviteSummary {
  const invite = value as Record<string, unknown>

  return {
    ...normalizeWorkspaceInvite(value),
    token: asNullableString(invite.token) ?? '',
    acceptUrl: asNullableString(invite.acceptUrl) ?? '',
  }
}

function normalizeWorkspaceRole(role: unknown): WorkspaceSummary['role'] {
  const value: unknown = role

  if (value === 3 || value === 'Owner') {
    return 'Owner'
  }

  if (value === 2 || value === 'Admin') {
    return 'Admin'
  }

  return 'Member'
}

function normalizeConversationType(
  type: ApiConversationSummary['type'],
): ConversationSummary['type'] {
  const value: unknown = type
  return value === 1 || value === 'Direct' ? 'Direct' : 'Channel'
}

function normalizeMessageType(type: ApiMessageSummary['type']): MessageSummary['type'] {
  const value: unknown = type

  if (value === 2 || value === 'System') {
    return 'System'
  }

  if (value === 3 || value === 'File') {
    return 'File'
  }

  return 'Text'
}

function requireString(value: string | null | undefined) {
  if (!value) {
    throw new Error('API response is missing a required identifier.')
  }

  return value
}

function asNullableString(value: unknown) {
  return typeof value === 'string' ? value : null
}
