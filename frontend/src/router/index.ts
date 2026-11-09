import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/LoginPage.vue'),
      meta: { title: '本地演示登录', public: true },
    },
    {
      path: '/',
      name: 'my-work',
      component: () => import('@/pages/MyWorkPage.vue'),
      meta: { title: '我的工作', requiredPermission: 'dashboard:read' },
    },
    {
      path: '/confirmations',
      name: 'confirmations',
      component: () => import('@/pages/ConfirmationCenterPage.vue'),
      meta: { title: '统一确认', requiredPermission: 'dashboard:read' },
    },
    {
      path: '/scenarios',
      name: 'scenarios',
      component: () => import('@/pages/ScenariosPage.vue'),
      meta: { title: '岗位业务场景', requiredPermission: 'scenario:read' },
    },
    {
      path: '/department',
      name: 'department',
      component: () => import('@/pages/DepartmentPage.vue'),
      meta: { title: '部门工作区', requiredPermission: 'department:read' },
    },
    {
      path: '/documents',
      name: 'documents',
      component: () => import('@/pages/DocumentsPage.vue'),
      meta: { title: '部门文件', requiredPermission: 'file:read' },
    },
    {
      path: '/minutes',
      name: 'minutes',
      component: () => import('@/pages/MinutesPage.vue'),
      meta: { title: 'AI 听记', requiredPermission: 'minutes:read' },
    },
    {
      path: '/projects',
      name: 'projects',
      component: () => import('@/pages/ProjectsPage.vue'),
      meta: { title: '项目工作台', requiredPermission: 'project:read' },
    },
    {
      path: '/projects/:id',
      name: 'project-detail',
      component: () => import('@/pages/ProjectDetailPage.vue'),
      meta: { title: '项目详情', requiredPermission: 'project:read' },
    },
    {
      path: '/inbox',
      name: 'inbox',
      component: () => import('@/pages/InboxPage.vue'),
      meta: { title: '事件收件箱', requiredPermission: 'event-inbox:read' },
    },
    {
      path: '/management',
      name: 'management',
      component: () => import('@/pages/DashboardPage.vue'),
      meta: {
        title: '管理概览',
        requiredPermission: 'dashboard:read',
        requiresPlatformAdmin: true,
      },
    },
    {
      path: '/workflows',
      name: 'workflows',
      component: () => import('@/pages/WorkflowPage.vue'),
      meta: { title: '流程事项', requiredPermission: 'workflow-task:read' },
    },
    {
      path: '/objects',
      name: 'objects',
      component: () => import('@/pages/BusinessObjectPage.vue'),
      meta: { title: '业务视图', requiredPermission: 'business-object:read' },
    },
    {
      path: '/modeling',
      name: 'modeling',
      component: () => import('@/pages/BusinessModelingPage.vue'),
      meta: {
        title: '业务建模工作室',
        requiredPermission: 'object-model:read',
        requiresPlatformAdmin: true,
      },
    },
    {
      path: '/audit',
      name: 'audit',
      component: () => import('@/pages/AuditPage.vue'),
      meta: { title: '审计证据', requiredPermission: 'audit:read' },
    },
    {
      path: '/agents',
      name: 'agents',
      component: () => import('@/pages/AgentPage.vue'),
      meta: { title: '数字员工身份', requiredPermission: 'agent:read' },
    },
    {
      path: '/workflow-policies',
      name: 'workflow-policies',
      component: () => import('@/pages/WorkflowPoliciesPage.vue'),
      meta: {
        title: '流程节点策略',
        requiredPermission: 'workflow-policy:read',
        requiresPlatformAdmin: true,
      },
    },
    {
      path: '/configurations',
      name: 'configurations',
      component: () => import('@/pages/ConfigurationCenterPage.vue'),
      meta: { title: '流程配置', requiredPermission: 'configuration:read' },
    },
    {
      path: '/iam',
      name: 'iam',
      component: () => import('@/pages/IamCenterPage.vue'),
      meta: { title: 'IAM 与导航', requiredPermission: 'identity-admin:read' },
    },
    {
      path: '/mcp-center',
      name: 'mcp-center',
      component: () => import('@/pages/McpCenterPage.vue'),
      meta: { title: 'MCP 接入治理', requiredPermission: 'mcp:read' },
    },
    {
      path: '/mcp-confirmations',
      name: 'mcp-confirmations',
      component: () => import('@/pages/McpConfirmationPage.vue'),
      meta: { title: 'MCP 真人确认' },
    },
    {
      path: '/access-denied',
      name: 'access-denied',
      component: () => import('@/pages/AccessDeniedPage.vue'),
      meta: { title: '无权限' },
    },
    {
      path: '/:pathMatch(.*)*',
      name: 'not-found',
      component: () => import('@/pages/NotFoundPage.vue'),
      meta: { title: '页面不存在' },
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  return (async () => {
    if (!auth.initialized) {
      await auth.initialize()
    }

    if (to.meta.public) {
      if (to.name === 'login' && auth.isAuthenticated && !auth.requiresReauthentication) {
        return { name: 'my-work' }
      }
      document.title = `${String(to.meta.title ?? 'AIDT')} · AIDT`
      return true
    }

    if (
      (!auth.isAuthenticated || auth.requiresReauthentication) &&
      auth.enforcement === 'enforced'
    ) {
      return {
        name: 'login',
        query: {
          redirect: to.fullPath,
          reason: auth.requiresReauthentication
            ? auth.errorMessage || '权限已更新，请重新登录。'
            : undefined,
        },
      }
    }

    if (!auth.can(to.meta.requiredPermission as string | undefined)) {
      return {
        name: 'access-denied',
        query: { permission: to.meta.requiredPermission as string },
      }
    }

    if (to.meta.requiresPlatformAdmin && !auth.isPlatformAdmin) {
      return { name: 'access-denied', query: { permission: 'platform-admin' } }
    }

    document.title = `${String(to.meta.title ?? 'AIDT')} · AIDT`
    return true
  })()
})

export default router
