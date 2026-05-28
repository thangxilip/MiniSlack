import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  acceptWorkspaceInvite,
  createWorkspaceInvite,
  createWorkspaceConversation,
  createConversationMessage,
  getConversationMessages,
  getWorkspaceConversations,
  getWorkspaceInvites,
  getWorkspaceMembers,
  getWorkspaces,
  removeWorkspaceMember,
  revokeWorkspaceInvite,
  startWorkspaceDirectMessage,
  updateWorkspaceMemberRole,
  type CreatedWorkspaceInviteSummary,
  type ConversationSummary,
  type MessageSummary,
  type WorkspaceInviteSummary,
  type WorkspaceMemberSummary,
  type WorkspaceSummary,
} from '@/lib/workspace-api'
import {
  createWorkspaceRealtimeClient,
  type TypingRealtimeDto,
  type WorkspaceRealtimeClient,
} from '@/lib/realtime-client'
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

export interface TypingUser {
  id: string
  name: string
}

export const useWorkspaceStore = defineStore('workspace', () => {
  const workspaces = ref<WorkspaceSummary[]>([])
  const conversations = ref<ConversationSummary[]>([])
  const messages = ref<MessageSummary[]>([])
  const members = ref<WorkspaceMemberSummary[]>([])
  const invites = ref<WorkspaceInviteSummary[]>([])
  const activeWorkspaceId = ref<string | null>(null)
  const activeConversationId = ref<string | null>(null)
  const loading = ref(false)
  const loadingMessages = ref(false)
  const loadingMembers = ref(false)
  const loadingInvites = ref(false)
  const creatingChannel = ref(false)
  const creatingInvite = ref(false)
  const managingMember = ref(false)
  const startingDirectMessage = ref(false)
  const sending = ref(false)
  const error = ref<string | null>(null)
  const typingUsersByConversation = ref<Record<string, Record<string, TypingUser>>>({})
  const typingTimeouts = new Map<string, ReturnType<typeof setTimeout>>()
  let realtimeClient: WorkspaceRealtimeClient | null = null

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

  const pendingInvites = computed(() =>
    invites.value.filter((invite) => !invite.acceptedAtUtc && !invite.revokedAtUtc),
  )

  const activeTypingUsers = computed<TypingUser[]>(() => {
    if (!activeConversationId.value) {
      return []
    }

    return Object.values(typingUsersByConversation.value[activeConversationId.value] ?? {})
  })

  async function loadDashboard(accessToken: string) {
    loading.value = true
    error.value = null

    try {
      workspaces.value = await getWorkspaces(accessToken)
      activeWorkspaceId.value = resolveWorkspaceId(activeWorkspaceId.value)

      if (activeWorkspaceId.value) {
        await loadConversations(accessToken, activeWorkspaceId.value)
        await joinRealtimeState()
      } else {
        conversations.value = []
        messages.value = []
        invites.value = []
        activeConversationId.value = null
      }
    } catch {
      error.value = 'Unable to load workspace data.'
      conversations.value = []
      messages.value = []
      invites.value = []
    } finally {
      loading.value = false
    }
  }

  async function loadConversations(accessToken: string, workspaceId: string) {
    conversations.value = await getWorkspaceConversations(accessToken, workspaceId)
    await loadMembers(accessToken, workspaceId)
    await loadInvites(accessToken, workspaceId)
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

  async function loadInvites(accessToken: string, workspaceId: string) {
    loadingInvites.value = true

    try {
      invites.value = await getWorkspaceInvites(accessToken, workspaceId)
    } catch {
      invites.value = []
    } finally {
      loadingInvites.value = false
    }
  }

  async function selectWorkspace(accessToken: string, workspaceId: string) {
    if (workspaceId === activeWorkspaceId.value) {
      return
    }

    const previousWorkspaceId = activeWorkspaceId.value
    const previousConversationId = activeConversationId.value
    activeWorkspaceId.value = workspaceId
    activeConversationId.value = null
    messages.value = []
    loading.value = true
    error.value = null

    try {
      if (previousConversationId) {
        await realtimeClient?.leaveConversation(previousConversationId)
      }
      if (previousWorkspaceId) {
        await realtimeClient?.leaveWorkspace(previousWorkspaceId)
      }
      await loadConversations(accessToken, workspaceId)
      await joinRealtimeState()
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

    const previousConversationId = activeConversationId.value
    activeConversationId.value = conversationId
    if (previousConversationId) {
      await realtimeClient?.leaveConversation(previousConversationId)
    }
    await realtimeClient?.joinConversation(conversationId)
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

      upsertRealtimeConversation(conversation)
      activeConversationId.value = conversation.id
      messages.value = []
      await realtimeClient?.joinConversation(conversation.id)
      return true
    } catch {
      error.value = 'Unable to create channel.'
      return false
    } finally {
      creatingChannel.value = false
    }
  }

  async function createInvite(
    accessToken: string,
    request: {
      email: string
      role: WorkspaceSummary['role']
    },
  ): Promise<CreatedWorkspaceInviteSummary | null> {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId || !request.email.trim()) {
      return null
    }

    creatingInvite.value = true
    error.value = null

    try {
      const invite = await createWorkspaceInvite(accessToken, workspaceId, {
        email: request.email.trim(),
        role: request.role,
      })
      invites.value = [invite, ...invites.value]
      return invite
    } catch {
      error.value = 'Unable to create invite.'
      return null
    } finally {
      creatingInvite.value = false
    }
  }

  async function revokeInvite(accessToken: string, inviteId: string) {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId) {
      return false
    }

    try {
      await revokeWorkspaceInvite(accessToken, workspaceId, inviteId)
      const revokedAtUtc = new Date().toISOString()
      invites.value = invites.value.map((invite) =>
        invite.id === inviteId ? { ...invite, revokedAtUtc } : invite,
      )
      return true
    } catch {
      error.value = 'Unable to revoke invite.'
      return false
    }
  }

  async function acceptInvite(accessToken: string, token: string) {
    error.value = null

    try {
      const result = await acceptWorkspaceInvite(accessToken, token)
      const exists = workspaces.value.some((workspace) => workspace.id === result.workspace.id)
      workspaces.value = exists
        ? workspaces.value.map((workspace) =>
            workspace.id === result.workspace.id ? result.workspace : workspace,
          )
        : [...workspaces.value, result.workspace]
      return result
    } catch {
      error.value = 'Unable to accept invite.'
      return null
    }
  }

  async function removeMember(accessToken: string, targetUserId: string) {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId) {
      return false
    }

    managingMember.value = true
    error.value = null

    try {
      const removed = await removeWorkspaceMember(accessToken, workspaceId, targetUserId)
      handleWorkspaceMemberRemoved(removed)
      return true
    } catch {
      error.value = 'Unable to remove member.'
      return false
    } finally {
      managingMember.value = false
    }
  }

  async function updateMemberRole(
    accessToken: string,
    targetUserId: string,
    role: WorkspaceSummary['role'],
  ) {
    const workspaceId = activeWorkspaceId.value
    if (!workspaceId) {
      return false
    }

    managingMember.value = true
    error.value = null

    try {
      const member = await updateWorkspaceMemberRole(accessToken, workspaceId, targetUserId, role)
      upsertWorkspaceMember(member)
      return true
    } catch {
      error.value = 'Unable to update member role.'
      return false
    } finally {
      managingMember.value = false
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
      await realtimeClient?.joinConversation(conversation.id)
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
      messages.value = [
        ...messages.value.filter(
          (candidate) => candidate.id !== optimisticId && candidate.id !== message.id,
        ),
        message,
      ]
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

  async function connectRealtime(accessTokenProvider: () => string | null) {
    if (realtimeClient) {
      return
    }

    realtimeClient = createWorkspaceRealtimeClient(accessTokenProvider, {
      messageCreated: upsertRealtimeMessage,
      conversationCreated: upsertRealtimeConversation,
      memberPresenceChanged: (presence) => {
        members.value = members.value.map((member) =>
          member.userId === presence.userId
            ? { ...member, status: presence.isOnline ? 'online' : 'offline' }
            : member,
        )
      },
      workspaceMemberAdded: upsertWorkspaceMember,
      workspaceMemberRemoved: handleWorkspaceMemberRemoved,
      workspaceMemberRoleChanged: upsertWorkspaceMember,
      userTyping: markUserTyping,
      userStoppedTyping: clearUserTyping,
      reconnected: joinRealtimeState,
    })

    try {
      await realtimeClient.start()
      await joinRealtimeState()
    } catch {
      error.value = 'Unable to connect to real-time updates.'
      await disconnectRealtime()
    }
  }

  async function disconnectRealtime() {
    clearTypingUsers()
    const client = realtimeClient
    realtimeClient = null
    await client?.stop()
  }

  async function startTyping(conversationId: string) {
    await realtimeClient?.startTyping(conversationId)
  }

  async function stopTyping(conversationId: string) {
    await realtimeClient?.stopTyping(conversationId)
  }

  async function joinRealtimeState() {
    if (!realtimeClient) {
      return
    }

    if (activeWorkspaceId.value) {
      await realtimeClient.joinWorkspace(activeWorkspaceId.value)
    }

    if (activeConversationId.value) {
      await realtimeClient.joinConversation(activeConversationId.value)
    }
  }

  function upsertRealtimeMessage(message: MessageSummary) {
    const exists = messages.value.some((candidate) => candidate.id === message.id)
    if (exists) {
      messages.value = messages.value.map((candidate) =>
        candidate.id === message.id ? message : candidate,
      )
      return
    }

    if (message.conversationId !== activeConversationId.value) {
      return
    }

    messages.value = [...messages.value, message]
  }

  function upsertRealtimeConversation(conversation: ConversationSummary) {
    if (conversation.workspaceId !== activeWorkspaceId.value) {
      return
    }

    const exists = conversations.value.some((candidate) => candidate.id === conversation.id)
    conversations.value = exists
      ? conversations.value.map((candidate) =>
          candidate.id === conversation.id ? conversation : candidate,
        )
      : [...conversations.value, conversation]
  }

  function upsertWorkspaceMember(member: WorkspaceMemberSummary) {
    const exists = members.value.some((candidate) => candidate.userId === member.userId)
    members.value = exists
      ? members.value.map((candidate) => (candidate.userId === member.userId ? member : candidate))
      : [...members.value, member]
  }

  function handleWorkspaceMemberRemoved(removed: { workspaceId: string; userId: string }) {
    if (removed.workspaceId !== activeWorkspaceId.value) {
      return
    }

    members.value = members.value.filter((member) => member.userId !== removed.userId)
    conversations.value = conversations.value.filter((conversation) => {
      if (conversation.type !== 'Direct') {
        return true
      }

      return conversation.memberCount > 1
    })
  }

  function markUserTyping(typing: TypingRealtimeDto) {
    const conversationUsers = {
      ...(typingUsersByConversation.value[typing.conversationId] ?? {}),
      [typing.userId]: {
        id: typing.userId,
        name: typing.displayName,
      },
    }

    typingUsersByConversation.value = {
      ...typingUsersByConversation.value,
      [typing.conversationId]: conversationUsers,
    }

    const timeoutKey = typingTimeoutKey(typing)
    const existingTimeout = typingTimeouts.get(timeoutKey)
    if (existingTimeout) {
      clearTimeout(existingTimeout)
    }

    typingTimeouts.set(
      timeoutKey,
      setTimeout(() => clearUserTyping(typing), 4000),
    )
  }

  function clearUserTyping(typing: TypingRealtimeDto) {
    const timeoutKey = typingTimeoutKey(typing)
    const existingTimeout = typingTimeouts.get(timeoutKey)
    if (existingTimeout) {
      clearTimeout(existingTimeout)
      typingTimeouts.delete(timeoutKey)
    }

    const conversationUsers = { ...(typingUsersByConversation.value[typing.conversationId] ?? {}) }
    delete conversationUsers[typing.userId]

    typingUsersByConversation.value = {
      ...typingUsersByConversation.value,
      [typing.conversationId]: conversationUsers,
    }
  }

  function clearTypingUsers() {
    for (const timeout of typingTimeouts.values()) {
      clearTimeout(timeout)
    }

    typingTimeouts.clear()
    typingUsersByConversation.value = {}
  }

  function clear() {
    workspaces.value = []
    conversations.value = []
    messages.value = []
    members.value = []
    invites.value = []
    activeWorkspaceId.value = null
    activeConversationId.value = null
    error.value = null
    loading.value = false
    loadingMessages.value = false
    loadingMembers.value = false
    loadingInvites.value = false
    creatingChannel.value = false
    creatingInvite.value = false
    managingMember.value = false
    startingDirectMessage.value = false
    sending.value = false
    clearTypingUsers()
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
    invites,
    activeWorkspaceId,
    activeConversationId,
    activeWorkspace,
    activeChannel,
    activeConversation,
    activeMessages,
    activeTypingUsers,
    pendingInvites,
    workspaceMembers,
    channels,
    directMessages,
    loading,
    loadingMessages,
    loadingMembers,
    loadingInvites,
    creatingChannel,
    creatingInvite,
    managingMember,
    startingDirectMessage,
    sending,
    error,
    clear,
    loadDashboard,
    connectRealtime,
    disconnectRealtime,
    selectWorkspace,
    selectConversation,
    createChannel,
    createInvite,
    revokeInvite,
    acceptInvite,
    removeMember,
    updateMemberRole,
    startDirectMessage,
    sendMessage,
    startTyping,
    stopTyping,
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

function typingTimeoutKey(typing: TypingRealtimeDto) {
  return `${typing.conversationId}:${typing.userId}`
}
