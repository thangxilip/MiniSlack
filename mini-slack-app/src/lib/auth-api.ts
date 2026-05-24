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

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7006'

export function loginWithGoogle() {
  window.location.href = `${apiBaseUrl}/auth/login/google`
}

export async function refreshAccessToken(): Promise<AuthResponse | null> {
  const response = await fetch(`${apiBaseUrl}/auth/refresh`, {
    method: 'POST',
    credentials: 'include',
  })

  if (!response.ok) {
    return null
  }

  return response.json()
}

export async function revokeSession() {
  await fetch(`${apiBaseUrl}/auth/logout`, {
    method: 'POST',
    credentials: 'include',
  })
}

export async function getCurrentUser(accessToken: string): Promise<UserProfile> {
  const response = await fetch(`${apiBaseUrl}/auth/me`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Unable to load the current user.')
  }

  return response.json()
}
