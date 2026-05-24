import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import {
  getCurrentUser,
  loginWithGoogle,
  refreshAccessToken,
  revokeSession,
  type UserProfile,
} from '@/lib/auth-api'

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const accessTokenExpiresAt = ref<number | null>(null)
  const user = ref<UserProfile | null>(null)
  const initialized = ref(false)
  const loading = ref(false)

  const isAuthenticated = computed(() => Boolean(accessToken.value && user.value))

  async function initialize() {
    if (initialized.value) {
      return isAuthenticated.value
    }

    initialized.value = true
    return refreshSession()
  }

  async function refreshSession() {
    initialized.value = true
    loading.value = true

    try {
      const auth = await refreshAccessToken()
      if (!auth) {
        clearSession()
        return false
      }

      accessToken.value = auth.accessToken
      accessTokenExpiresAt.value = Date.now() + auth.expiresInSeconds * 1000
      user.value = await getCurrentUser(auth.accessToken)

      return true
    } catch {
      clearSession()
      return false
    } finally {
      loading.value = false
    }
  }

  function startLogin() {
    loginWithGoogle()
  }

  async function logout() {
    loading.value = true

    try {
      await revokeSession()
      clearSession()
    } finally {
      loading.value = false
    }
  }

  function clearSession() {
    accessToken.value = null
    accessTokenExpiresAt.value = null
    user.value = null
  }

  return {
    accessToken,
    accessTokenExpiresAt,
    user,
    initialized,
    loading,
    isAuthenticated,
    initialize,
    refreshSession,
    startLogin,
    logout,
  }
})
