<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { navigationApi } from '@/api/modules/navigationApi'
import { useAuthStore } from '@/stores/auth'
import { useNavigationStore } from '@/stores/navigation'
import type { NavigationPreferenceItem } from '@/api/types/contracts'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const navigation = useNavigationStore()
const preferenceVisible = ref(false)
const preferenceSaving = ref(false)
const preferenceRows = ref<Array<NavigationPreferenceItem & { name: string }>>([])
const navPanel = ref<HTMLElement | null>(null)
const drawerTrigger = ref<HTMLButtonElement | null>(null)
const sidebarOpen = ref(false)
const isNarrow = ref(false)
const openDepartmentKey = ref<string | null>(null)
let narrowQuery: MediaQueryList | null = null

const navGroups = computed(() =>
  navigation.groups.map((group) => ({
    ...group,
    items: group.items.map((item) => ({
      path: item.route,
      title: item.name,
      short: item.icon ?? item.name.slice(0, 1),
      key: `${group.workspaceKey}:${item.itemKey}`,
      workspaceKey: group.workspaceKey,
      itemKey: item.itemKey,
      isFavorite: item.isFavorite,
      isHidden: item.isHidden,
      sortOrder: item.sortOrder,
    })),
  })),
)
const companyGroup = computed(() =>
  navGroups.value.find((group) => group.layer === 'Company'),
)
const departmentGroups = computed(() =>
  navGroups.value.filter((group) => group.layer === 'Department'),
)
const platformGroup = computed(() =>
  navGroups.value.find((group) => group.layer === 'Platform'),
)
const favoriteItems = computed(() => {
  const seen = new Set<string>()
  return navGroups.value
    .flatMap((group) => group.items.filter((item) => item.isFavorite))
    .filter((item) => {
      if (seen.has(item.key)) return false
      seen.add(item.key)
      return true
    })
})
const activeNavKey = computed(() => {
  for (const group of navGroups.value) {
    for (const item of group.items) {
      if (navActive(item.path)) return item.key
    }
  }
  for (const item of favoriteItems.value) {
    if (navActive(item.path)) return `favorite:${item.key}`
  }
  return ''
})

const pageTitle = computed(() => String(route.meta.title ?? 'AIDT'))

function navActive(path: string) {
  return path === '/' ? route.path === '/' : route.path.startsWith(path)
}

function syncOpenDepartment() {
  const currentDepartment = departmentGroups.value.find(
    (group) => group.departmentId === navigation.currentDepartmentId,
  )
  const activeDepartment = departmentGroups.value.find((group) =>
    group.items.some((item) => navActive(item.path)),
  )
  openDepartmentKey.value =
    currentDepartment?.workspaceKey ??
    activeDepartment?.workspaceKey ??
    departmentGroups.value[0]?.workspaceKey ??
    null
}

function handleDepartmentToggle(group: { workspaceKey: string }, event: Event) {
  const details = event.currentTarget as HTMLDetailsElement
  if (details.open) {
    openDepartmentKey.value = group.workspaceKey
  } else if (openDepartmentKey.value === group.workspaceKey) {
    openDepartmentKey.value = null
  }
}

async function scrollActiveNavItem() {
  await nextTick()
  const active = Array.from(
    navPanel.value?.querySelectorAll<HTMLElement>('[data-nav-key]') ?? [],
  ).find((element) => element.dataset.navKey === activeNavKey.value)
  active?.scrollIntoView({ block: 'nearest' })
}

function updateNarrowMode(event: MediaQueryListEvent | MediaQueryList) {
  isNarrow.value = event.matches
  if (!event.matches) sidebarOpen.value = false
}

async function openSidebar() {
  sidebarOpen.value = true
  await nextTick()
  const active =
    navPanel.value?.querySelector<HTMLElement>('.nav-link.active') ??
    navPanel.value?.querySelector<HTMLElement>('.nav-link')
  active?.focus()
}

async function closeSidebar(returnFocus = true) {
  if (!sidebarOpen.value) return
  if (returnFocus) drawerTrigger.value?.focus()
  sidebarOpen.value = false
  await nextTick()
}

function handleGlobalKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && isNarrow.value && sidebarOpen.value) {
    event.preventDefault()
    closeSidebar()
  }
}

async function logout() {
  navigation.clear()
  await auth.logout()
  await router.replace('/login')
}

async function loadNavigation() {
  if (auth.isAuthenticated) {
    await navigation.load()
  }
}

async function openPreferences() {
  const saved = await navigationApi.getPreferences()
  const savedMap = new Map(saved.map((item) => [`${item.workspaceKey}:${item.itemKey}`, item]))
  const rows: Array<NavigationPreferenceItem & { name: string }> = navGroups.value.flatMap((group) =>
    group.items.map((item) => ({
      workspaceKey: item.workspaceKey,
      itemKey: item.itemKey,
      name: item.title,
      isFavorite: item.isFavorite,
      isHidden: item.isHidden,
      sortOrder: item.sortOrder,
    })),
  )
  for (const item of saved.filter((row) => row.isHidden)) {
    if (!rows.some((row) => row.workspaceKey === item.workspaceKey && row.itemKey === item.itemKey)) {
      rows.push({
        workspaceKey: item.workspaceKey,
        itemKey: item.itemKey,
        name: `已隐藏：${item.itemKey}`,
        isFavorite: item.isFavorite,
        isHidden: item.isHidden,
        sortOrder: item.sortOrder,
      })
    }
  }
  preferenceRows.value = rows.map((row) => ({
    ...row,
    ...savedMap.get(`${row.workspaceKey}:${row.itemKey}`),
    name: row.name,
  }))
  preferenceVisible.value = true
}

async function savePreferences() {
  preferenceSaving.value = true
  try {
    await navigationApi.setPreferences(
      preferenceRows.value.map(({ name: _name, ...item }) => item),
    )
    ElMessage.success('个人导航偏好已保存')
    preferenceVisible.value = false
    await navigation.load(true)
  } finally {
    preferenceSaving.value = false
  }
}

onMounted(() => {
  narrowQuery = window.matchMedia('(max-width: 820px)')
  updateNarrowMode(narrowQuery)
  narrowQuery.addEventListener('change', updateNarrowMode)
  window.addEventListener('keydown', handleGlobalKeydown)
  void loadNavigation()
})

onBeforeUnmount(() => {
  narrowQuery?.removeEventListener('change', updateNarrowMode)
  window.removeEventListener('keydown', handleGlobalKeydown)
})

watch(
  () => route.fullPath,
  async () => {
    syncOpenDepartment()
    sidebarOpen.value = false
    await scrollActiveNavItem()
  },
  { immediate: true },
)
watch(
  [
    () => navigation.currentDepartmentId,
    () => departmentGroups.value.map((group) => group.workspaceKey).join('|'),
  ],
  async () => {
    syncOpenDepartment()
    await scrollActiveNavItem()
  },
  { immediate: true },
)
watch(() => auth.isAuthenticated, loadNavigation)
</script>

<template>
  <RouterView v-if="route.name === 'login'" />
  <template v-else>
  <a class="skip-link" href="#main-content">跳到主要内容</a>
  <div class="app-layout">
    <button
      v-if="sidebarOpen"
      class="drawer-backdrop"
      type="button"
      aria-label="关闭主导航"
      data-testid="sidebar-backdrop"
      @click="closeSidebar()"
    ></button>
    <aside
      id="main-sidebar"
      class="sidebar"
      :class="{ 'drawer-open': sidebarOpen }"
      aria-label="主导航"
      :aria-hidden="isNarrow && !sidebarOpen ? 'true' : undefined"
      :aria-modal="isNarrow && sidebarOpen ? 'true' : undefined"
      :inert="isNarrow && !sidebarOpen ? true : undefined"
      data-testid="app-sidebar"
    >
      <RouterLink class="brand" to="/" aria-label="AIDT 首页">
        <span class="brand-mark" aria-hidden="true">A</span>
        <span>
          <strong>AIDT</strong>
          <small>企业业务操作系统</small>
        </span>
      </RouterLink>

      <nav ref="navPanel" data-testid="sidebar-navigation">
        <template v-if="favoriteItems.length">
          <div class="nav-section-label">我的收藏</div>
          <RouterLink
            v-for="item in favoriteItems"
            :key="`favorite:${item.key}`"
            :to="item.path"
            class="nav-link"
            :class="{ active: navActive(item.path) }"
            :data-nav-key="`favorite:${item.key}`"
            :aria-current="navActive(item.path) ? 'page' : undefined"
          >
            <span class="nav-icon" aria-hidden="true">{{ item.short }}</span>
            <span>{{ item.title }}</span>
          </RouterLink>
        </template>

        <template v-if="companyGroup">
          <div class="nav-section-label">公司公共入口</div>
          <RouterLink
            v-for="item in companyGroup.items"
            :key="item.key"
            :to="item.path"
            class="nav-link company-link"
            :class="{ active: navActive(item.path) }"
            :data-nav-key="item.key"
            :aria-current="navActive(item.path) ? 'page' : undefined"
          >
            <span class="nav-icon" aria-hidden="true">{{ item.short }}</span>
            <span>{{ item.title }}</span>
          </RouterLink>
        </template>

        <div class="nav-section-label">部门工作空间</div>
        <details
          v-for="group in departmentGroups"
          :key="group.workspaceKey"
          class="department-group"
          :open="openDepartmentKey === group.workspaceKey"
          @toggle="handleDepartmentToggle(group, $event)"
        >
          <summary>
            <span>
              {{ group.departmentName || group.workspaceName }}
              <el-tag v-if="group.isDemoData" size="small" type="warning" effect="plain">
                Demo
              </el-tag>
            </span>
            <small>{{ group.items.length }} 个入口</small>
          </summary>
          <RouterLink
            v-for="item in group.items"
            :key="item.key"
            :to="item.path"
            class="nav-link department-link"
            :class="{ active: navActive(item.path) }"
            :data-nav-key="item.key"
            :aria-current="navActive(item.path) ? 'page' : undefined"
          >
            <span class="nav-icon" aria-hidden="true">{{ item.short }}</span>
            <span>{{ item.title }}</span>
          </RouterLink>
        </details>

        <template v-if="platformGroup">
          <div class="nav-section-label">平台治理</div>
          <RouterLink
            v-for="item in platformGroup.items"
            :key="item.key"
            :to="item.path"
            class="nav-link admin-link"
            :class="{ active: navActive(item.path) }"
            :data-nav-key="item.key"
            :aria-current="navActive(item.path) ? 'page' : undefined"
          >
            <span class="nav-icon" aria-hidden="true">{{ item.short }}</span>
            <span>{{ item.title }}</span>
          </RouterLink>
        </template>
        <span v-if="navigation.loading" class="nav-loading">导航加载中…</span>
        <span v-else-if="navigation.errorMessage" class="nav-loading">
          {{ navigation.errorMessage }}
        </span>
      </nav>

      <div class="sidebar-footer">
        <div class="sidebar-state">
          <span class="state-dot" aria-hidden="true"></span>
          <div>
            <strong>部门优先工作空间</strong>
            <small>{{ departmentGroups.length }} 个可访问部门 · 平台入口按权限显示</small>
          </div>
        </div>
        <el-button
          class="preference-button"
          data-testid="navigation-preferences"
          text
          @click="openPreferences"
        >个人导航偏好</el-button>
      </div>
    </aside>

    <div class="main-column">
      <header class="topbar">
        <div class="topbar-heading">
          <button
            ref="drawerTrigger"
            class="drawer-trigger"
            type="button"
            aria-label="打开主导航"
            aria-controls="main-sidebar"
            :aria-expanded="sidebarOpen"
            data-testid="sidebar-drawer-trigger"
            @click="openSidebar"
          >
            <span aria-hidden="true">☰</span>
          </button>
          <div>
            <span class="topbar-label">当前页面</span>
            <strong>{{ pageTitle }}</strong>
          </div>
        </div>
        <div class="topbar-meta">
          <span class="user-name">{{ auth.session.displayName }}</span>
          <el-tag v-if="auth.isPreviewMode" type="warning" effect="plain">
            权限契约预览
          </el-tag>
          <el-button v-if="auth.isAuthenticated" data-testid="logout-button" text @click="logout">退出</el-button>
          <span class="trace-label">AIDT · 环境由部署配置控制</span>
        </div>
      </header>

      <main id="main-content" class="main-content" tabindex="-1">
        <RouterView />
      </main>
    </div>

    <el-dialog v-model="preferenceVisible" title="个人导航偏好" width="min(680px, 94vw)">
      <p class="preference-hint">排序、收藏和隐藏仅影响当前用户，不改变部门导航发布版本。</p>
      <el-table :data="preferenceRows" max-height="460">
        <el-table-column prop="name" label="入口" min-width="180" />
        <el-table-column label="排序" width="130">
          <template #default="{ row }">
            <el-input-number v-model="row.sortOrder" :min="0" :max="9999" size="small" />
          </template>
        </el-table-column>
        <el-table-column label="收藏" width="90">
          <template #default="{ row }"><el-switch v-model="row.isFavorite" /></template>
        </el-table-column>
        <el-table-column label="隐藏" width="90">
          <template #default="{ row }"><el-switch v-model="row.isHidden" /></template>
        </el-table-column>
      </el-table>
      <template #footer>
        <el-button @click="preferenceVisible = false">取消</el-button>
        <el-button type="primary" :loading="preferenceSaving" @click="savePreferences">保存偏好</el-button>
      </template>
    </el-dialog>
  </div>
  </template>
</template>

<style scoped>
.skip-link {
  position: fixed;
  z-index: 100;
  top: 8px;
  left: 8px;
  transform: translateY(-150%);
  border-radius: var(--aidt-radius-sm);
  background: var(--aidt-ink);
  color: white;
  padding: 10px 14px;
}

.skip-link:focus {
  transform: translateY(0);
}

.app-layout {
  display: grid;
  width: 100%;
  height: 100vh;
  height: 100dvh;
  min-height: 0;
  overflow: hidden;
  grid-template-columns: var(--aidt-sidebar-width) minmax(0, 1fr);
}

.sidebar {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  gap: 14px;
  border-right: 1px solid #dce6e0;
  background:
    linear-gradient(180deg, rgb(231 244 237 / 72%), transparent 150px),
    #f8fbf9;
  color: var(--aidt-ink);
  min-width: 0;
  max-width: 100vw;
  overflow: hidden;
  padding: 16px 12px 12px;
  z-index: 2;
}

.brand {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 0 0 auto;
  color: inherit;
  border: 1px solid #dce8e1;
  border-radius: 14px;
  background: rgb(255 255 255 / 86%);
  padding: 11px 12px;
  text-decoration: none;
}

.brand-mark {
  display: grid;
  width: 36px;
  height: 36px;
  place-items: center;
  border-radius: 12px 12px 12px 4px;
  background: var(--aidt-brand);
  color: #fff;
  font-size: 1.15rem;
  font-weight: 800;
  box-shadow: 0 6px 14px rgb(22 98 75 / 18%);
}

.brand strong,
.brand small {
  display: block;
}

.brand strong {
  letter-spacing: 0.08em;
}

.brand small {
  margin-top: 2px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

nav {
  display: flex;
  flex: 1 1 auto;
  min-height: 0;
  flex-direction: column;
  min-width: 0;
  max-width: 100%;
  gap: 2px;
  overflow-x: hidden;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 0 3px 12px 0;
  scrollbar-gutter: stable;
  scrollbar-width: thin;
}

.nav-link {
  display: flex;
  min-height: 38px;
  align-items: center;
  gap: 9px;
  border-radius: 8px;
  color: #3d4c45;
  padding: 7px 9px;
  font-size: 13px;
  line-height: 1.3;
  text-decoration: none;
  transition: background 160ms ease, color 160ms ease;
}

.nav-section-label {
  margin: 9px 0 4px;
  border-left: 3px solid #7cb99a;
  border-radius: 0 6px 6px 0;
  background: #eef5f1;
  color: #63756c;
  padding: 5px 9px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  white-space: nowrap;
}

.nav-loading {
  padding: 12px;
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.department-group {
  flex: 0 0 auto;
  overflow: visible;
}

.department-group summary {
  display: flex;
  min-height: 36px;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  border-radius: 8px;
  cursor: pointer;
  color: #405149;
  padding: 7px 9px;
  font-size: 13px;
  font-weight: 600;
  list-style: none;
}

.department-group summary::-webkit-details-marker {
  display: none;
}

.department-group summary span {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 7px;
}

.department-group summary small {
  flex: 0 0 auto;
  color: #89968f;
  font-size: 0.68rem;
  font-weight: 500;
}

.department-group[open] summary {
  background: #f0f6f2;
  color: var(--aidt-brand-strong);
}

.department-link {
  position: relative;
  min-height: 36px;
  margin-left: 13px;
  border-radius: 0 8px 8px 0;
  padding-left: 17px;
}

.department-link::before {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  width: 1px;
  background: #cfdcd5;
  content: '';
}

.company-link {
  background: rgb(255 255 255 / 64%);
}

.admin-link {
  border: 1px solid #e3ebe6;
}

.nav-link:hover,
.nav-link.active {
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand-strong);
}

.nav-link.active {
  box-shadow: inset 3px 0 var(--aidt-brand);
  font-weight: 700;
}

.nav-icon {
  display: grid;
  width: 24px;
  height: 24px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 7px;
  background: #e9f1ec;
  color: #4d6c5e;
  font-size: 0.72rem;
  font-weight: 700;
}

.sidebar-footer {
  display: grid;
  flex: 0 0 auto;
  gap: 7px;
  border-top: 1px solid #dce6e0;
  padding-top: 10px;
}

.sidebar-state {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 1px solid #dce7e0;
  border-radius: 10px;
  background: #fff;
  padding: 9px 10px;
}

.sidebar-state strong,
.sidebar-state small {
  display: block;
}

.sidebar-state small {
  margin-top: 2px;
  color: var(--aidt-ink-muted);
  font-size: 0.66rem;
}

.preference-button {
  width: 100%;
  color: var(--aidt-brand-strong);
}

.preference-hint {
  color: var(--aidt-ink-muted);
  line-height: 1.7;
}

.state-dot {
  width: 9px;
  height: 9px;
  flex: 0 0 auto;
  border-radius: 999px;
  background: #4f9d78;
  box-shadow: 0 0 0 5px rgb(79 157 120 / 12%);
}

.main-column {
  display: grid;
  height: 100%;
  min-width: 0;
  min-height: 0;
  grid-template-rows: auto minmax(0, 1fr);
  overflow: hidden;
}

.topbar {
  display: flex;
  min-height: 64px;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  border-bottom: 1px solid var(--aidt-border);
  background: rgb(255 255 255 / 84%);
  padding: 10px clamp(20px, 3vw, 44px);
  backdrop-filter: blur(12px);
}

.topbar-heading {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 10px;
}

.topbar-label,
.topbar strong {
  display: block;
}

.topbar-label {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.topbar-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-name {
  color: var(--aidt-ink);
  font-size: 0.8rem;
  font-weight: 700;
}

.trace-label {
  color: var(--aidt-ink-muted);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.68rem;
}

.drawer-trigger {
  display: none;
  width: 38px;
  height: 38px;
  flex: 0 0 auto;
  border: 1px solid var(--aidt-border);
  border-radius: 10px;
  background: #fff;
  color: var(--aidt-brand-strong);
  font-size: 19px;
  cursor: pointer;
}

.drawer-backdrop {
  display: none;
}

.main-content {
  width: 100%;
  min-width: 0;
  min-height: 0;
  overflow-x: clip;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: clamp(20px, 3vw, 40px);
  scrollbar-gutter: stable;
}

@media (max-width: 820px) {
  .app-layout {
    grid-template-columns: 1fr;
  }

  .sidebar {
    position: fixed;
    inset: 0 auto 0 0;
    width: min(280px, 86vw);
    height: 100vh;
    height: 100dvh;
    z-index: 60;
    transform: translateX(-102%);
    transition: transform 180ms ease;
    box-shadow: 18px 0 40px rgb(20 45 34 / 18%);
  }

  .sidebar.drawer-open {
    transform: translateX(0);
  }

  .drawer-backdrop {
    position: fixed;
    z-index: 50;
    inset: 0;
    display: block;
    width: 100%;
    height: 100%;
    border: 0;
    background: rgb(10 31 23 / 42%);
    cursor: pointer;
  }

  .sidebar-state {
    display: flex;
  }

  .preference-button {
    align-self: stretch;
  }

  .topbar {
    min-height: 58px;
    align-items: center;
    padding: 9px 14px;
  }

  .drawer-trigger {
    display: grid;
    place-items: center;
  }

  .topbar-meta {
    min-width: 0;
    align-items: center;
    gap: 6px;
  }

  .trace-label,
  .topbar-meta :deep(.el-tag) {
    display: none;
  }

  .main-content {
    padding: 18px 14px 30px;
  }
}

@media (max-width: 480px) {
  .brand small {
    display: none;
  }

  .nav-link {
    padding: 7px 8px;
  }

  .user-name {
    max-width: 90px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
</style>
