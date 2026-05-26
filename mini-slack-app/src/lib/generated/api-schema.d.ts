/**
 * This file is generated from the MiniSlack Swagger/OpenAPI document.
 * Regenerate it with: pnpm generate:api
 */
export interface paths {
  '/auth/refresh': {
    post: {
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['AuthResponse']
          }
        }
        401: never
      }
    }
  }
  '/auth/logout': {
    post: {
      responses: {
        204: never
      }
    }
  }
  '/auth/me': {
    get: {
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['UserProfile']
          }
        }
        401: never
        404: never
      }
    }
  }
  '/workspaces': {
    get: {
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['WorkspaceSummary'][]
          }
        }
        401: never
      }
    }
    post: {
      requestBody: {
        content: {
          'application/json': components['schemas']['CreateWorkspaceRequest']
        }
      }
      responses: {
        201: {
          content: {
            'application/json': components['schemas']['WorkspaceSummary']
          }
        }
        400: {
          content: {
            'application/problem+json': components['schemas']['ProblemDetails']
          }
        }
        401: never
      }
    }
  }
  '/workspaces/{workspaceId}/conversations': {
    get: {
      parameters: {
        path: {
          workspaceId: string
        }
      }
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['ConversationSummary'][]
          }
        }
        401: never
        404: never
      }
    }
    post: {
      parameters: {
        path: {
          workspaceId: string
        }
      }
      requestBody: {
        content: {
          'application/json': components['schemas']['CreateConversationRequest']
        }
      }
      responses: {
        201: {
          content: {
            'application/json': components['schemas']['ConversationSummary']
          }
        }
        400: {
          content: {
            'application/problem+json': components['schemas']['ProblemDetails']
          }
        }
        401: never
        404: never
      }
    }
  }
  '/conversations/{conversationId}/messages': {
    get: {
      parameters: {
        path: {
          conversationId: string
        }
        query?: {
          before?: string
          limit?: number
        }
      }
      responses: {
        200: {
          content: {
            'application/json': components['schemas']['MessageSummary'][]
          }
        }
        401: never
        404: never
      }
    }
    post: {
      parameters: {
        path: {
          conversationId: string
        }
      }
      requestBody: {
        content: {
          'application/json': components['schemas']['CreateMessageRequest']
        }
      }
      responses: {
        201: {
          content: {
            'application/json': components['schemas']['MessageSummary']
          }
        }
        400: {
          content: {
            'application/problem+json': components['schemas']['ProblemDetails']
          }
        }
        401: never
        404: never
      }
    }
  }
}

export interface components {
  schemas: {
    AuthResponse: {
      accessToken: string
      expiresInSeconds: number
    }
    UserProfile: {
      id: string
      email: string
      displayName: string
      avatarUrl?: string | null
      status?: string | null
    }
    WorkspaceSummary: {
      id: string
      name: string
      slug: string
      role: 'Member' | 'Admin' | 'Owner'
    }
    ConversationSummary: {
      id: string
      workspaceId: string
      type: 'Direct' | 'Channel'
      name: string
      description?: string | null
      isPrivate: boolean
      memberCount: number
      createdAtUtc: string
    }
    MessageSummary: {
      id: string
      conversationId: string
      senderId: string
      senderDisplayName: string
      senderAvatarUrl?: string | null
      content: string
      type: 'Text' | 'System' | 'File'
      createdAtUtc: string
      editedAtUtc?: string | null
    }
    CreateWorkspaceRequest: {
      name: string
    }
    CreateConversationRequest: {
      name: string
      description?: string | null
      type: 'Direct' | 'Channel'
      isPrivate: boolean
    }
    CreateMessageRequest: {
      content: string
      parentMessageId?: string | null
    }
    ProblemDetails: {
      title?: string | null
      detail?: string | null
      status?: number | null
    }
  }
}
