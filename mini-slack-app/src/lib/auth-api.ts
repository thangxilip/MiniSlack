import { apiBaseUrl, apiClient } from '@/lib/api-client'

export interface AuthResponse {
  accessToken: string
  expiresInSeconds: number
}

export interface UserProfile {
  id: string
  email: string
  displayName: string
  avatarUrl?: string | null
  status?: string | null
}

export function loginWithGoogle() {
  window.location.href = `${apiBaseUrl}/auth/login/google`
}

export async function refreshAccessToken(): Promise<AuthResponse | null> {
  const { data } = await apiClient.POST('/auth/refresh')
  if (!data) {
    return null
  }

  return data as AuthResponse
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

  return data as UserProfile
}
