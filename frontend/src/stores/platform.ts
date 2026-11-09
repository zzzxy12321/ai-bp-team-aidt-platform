import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import { platformApi } from '@/api/modules/platformApi'
import type { PlatformCapabilities } from '@/api/types/contracts'

export type LoadState = 'idle' | 'loading' | 'success' | 'error'

export const usePlatformStore = defineStore('platform', {
  state: () => ({
    loadState: 'idle' as LoadState,
    capabilities: null as PlatformCapabilities | null,
    errorCode: '',
    errorMessage: '',
    traceId: '',
  }),
  actions: {
    async loadCapabilities() {
      this.loadState = 'loading'
      this.errorCode = ''
      this.errorMessage = ''
      this.traceId = ''
      try {
        this.capabilities = await platformApi.getCapabilities()
        this.loadState = 'success'
      } catch (error) {
        this.loadState = 'error'
        this.capabilities = null
        if (error instanceof ApiError) {
          this.errorCode = error.code
          this.errorMessage = error.message
          this.traceId = error.traceId
        } else {
          this.errorCode = 'UNEXPECTED_ERROR'
          this.errorMessage = '加载平台状态时发生未预期错误。'
        }
      }
    },
  },
})
