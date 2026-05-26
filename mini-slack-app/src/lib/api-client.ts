import createClient from 'openapi-fetch'
import type { paths } from '@/lib/generated/api-schema'

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7006'

export const apiClient = createClient<paths>({
  baseUrl: apiBaseUrl,
  credentials: 'include',
})
