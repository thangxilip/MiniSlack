import * as signalR from '@microsoft/signalr'
import { apiBaseUrl } from '@/lib/api-client'
import type { ConversationSummary, MessageSummary, WorkspaceMemberSummary } from '@/lib/workspace-api'

export interface PresenceRealtimeDto {
  userId: string
  isOnline: boolean
}

export interface TypingRealtimeDto {
  conversationId: string
  userId: string
  displayName: string
}

export interface WorkspaceRealtimeHandlers {
  messageCreated: (message: MessageSummary) => void
  conversationCreated: (conversation: ConversationSummary) => void
  memberPresenceChanged: (presence: PresenceRealtimeDto) => void
  workspaceMemberAdded: (member: WorkspaceMemberSummary) => void
  workspaceMemberRemoved: (member: { workspaceId: string; userId: string }) => void
  workspaceMemberRoleChanged: (member: WorkspaceMemberSummary) => void
  userTyping: (typing: TypingRealtimeDto) => void
  userStoppedTyping: (typing: TypingRealtimeDto) => void
  reconnected: () => void | Promise<void>
}

export interface WorkspaceRealtimeClient {
  start: () => Promise<void>
  stop: () => Promise<void>
  joinWorkspace: (workspaceId: string) => Promise<void>
  leaveWorkspace: (workspaceId: string) => Promise<void>
  joinConversation: (conversationId: string) => Promise<void>
  leaveConversation: (conversationId: string) => Promise<void>
  startTyping: (conversationId: string) => Promise<void>
  stopTyping: (conversationId: string) => Promise<void>
}

export function createWorkspaceRealtimeClient(
  accessTokenProvider: () => string | null,
  handlers: WorkspaceRealtimeHandlers,
): WorkspaceRealtimeClient {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/hubs/workspace`, {
      accessTokenFactory: () => accessTokenProvider() ?? '',
      withCredentials: false,
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.on('MessageCreated', handlers.messageCreated)
  connection.on('ConversationCreated', handlers.conversationCreated)
  connection.on('MemberPresenceChanged', handlers.memberPresenceChanged)
  connection.on('WorkspaceMemberAdded', handlers.workspaceMemberAdded)
  connection.on('WorkspaceMemberRemoved', handlers.workspaceMemberRemoved)
  connection.on('WorkspaceMemberRoleChanged', handlers.workspaceMemberRoleChanged)
  connection.on('UserTyping', handlers.userTyping)
  connection.on('UserStoppedTyping', handlers.userStoppedTyping)
  connection.onreconnected(() => {
    void handlers.reconnected()
  })

  async function invoke(methodName: string, ...args: unknown[]) {
    if (connection.state !== signalR.HubConnectionState.Connected) {
      return
    }

    await connection.invoke(methodName, ...args)
  }

  return {
    async start() {
      if (connection.state !== signalR.HubConnectionState.Disconnected) {
        return
      }

      await connection.start()
    },
    async stop() {
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        return
      }

      await connection.stop()
    },
    joinWorkspace: (workspaceId) => invoke('JoinWorkspace', workspaceId),
    leaveWorkspace: (workspaceId) => invoke('LeaveWorkspace', workspaceId),
    joinConversation: (conversationId) => invoke('JoinConversation', conversationId),
    leaveConversation: (conversationId) => invoke('LeaveConversation', conversationId),
    startTyping: (conversationId) => invoke('StartTyping', conversationId),
    stopTyping: (conversationId) => invoke('StopTyping', conversationId),
  }
}
