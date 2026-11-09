import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import { navigationApi } from '@/api/modules/navigationApi'
import type { NavigationGroupDto } from '@/api/types/contracts'

export const useNavigationStore = defineStore('navigation', {
  state: () => ({
    groups: [] as NavigationGroupDto[],
    currentDepartmentId: null as string | null,
    loading: false,
    loaded: false,
    errorMessage: '',
  }),
  actions: {
    async load(force = false) {
      if (this.loading || (this.loaded && !force)) return
      this.loading = true
      this.errorMessage = ''
      try {
        const current = await navigationApi.getCurrent()
        this.groups = current.groups
        this.currentDepartmentId = current.currentDepartmentId
        this.loaded = true
      } catch (error) {
        this.errorMessage = error instanceof ApiError ? error.message : '读取导航失败。'
      } finally {
        this.loading = false
      }
    },
    clear() {
      this.groups = []
      this.currentDepartmentId = null
      this.loaded = false
      this.errorMessage = ''
    },
  },
})
