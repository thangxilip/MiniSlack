import { apiBaseUrl, apiClient } from '@/lib/api-client'
import type { components } from '@/lib/generated/api-schema'

export interface AuthResponse {
  accessToken: string
  expiresInSeconds: number
}

export type UserProfile = components['schemas']['UserProfile']

export function loginWithGoogle() {
  window.location.href = `${apiBaseUrl}/auth/login/google`
}

export async function refreshAccessToken(): Promise<AuthResponse | null> {
  const { data } = await apiClient.POST('/auth/refresh')
  if (!data) {
    return null
  }

  return data
}

export async function revokeSession() {
  await apiClient.POST('/auth/logout')
}

export async function getCurrentUser(accessToken: string): Promise<UserProfile> {
  const { data } = await apiClient.GET('/auth/me', {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  if (!data) {
    throw new Error('Unable to load the current user.')
  }

  return data
}
