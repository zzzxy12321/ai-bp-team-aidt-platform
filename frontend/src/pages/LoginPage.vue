<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const selectedUser = ref('demo.admin')

const users = [
  { value: 'demo.admin', label: '平台管理员' },
  { value: 'demo.researcher', label: '研发工程师' },
  { value: 'demo.qa', label: '质量工程师' },
  { value: 'demo.supply', label: '供应链专员' },
  { value: 'demo.finance', label: '财务审批人' },
  { value: 'demo.production', label: '生产班组' },
  { value: 'demo.pm', label: '项目经理' },
  { value: 'demo.delivery', label: '交付专员（Demo）' },
  { value: 'demo.security', label: '安全管理员' },
]

async function login() {
  try {
    await auth.login(selectedUser.value)
    ElMessage.success('本地演示身份已登录')
    const redirect = String(route.query.redirect ?? '/')
    if (redirect.startsWith('/oauth/')) {
      window.location.assign(redirect)
      return
    }
    await router.replace(redirect)
  } catch {
    // The store exposes the stable API error message.
  }
}
</script>

<template>
  <main class="login-shell">
    <section class="login-card">
      <p class="page-kicker">AIDT · LOCAL DEMO</p>
      <h1>选择本地演示身份</h1>
      <p>
        这是仅允许本机回环访问的测试身份，不是生产 SSO。不同身份拥有不同 API 权限、
        组织与部门数据范围和任务分配。SQL 阻断期间身份接口会明确失败关闭，
        不会伪造登录成功或持久化数据。
      </p>
      <el-form label-position="top" @submit.prevent="login">
        <el-form-item label="演示用户">
          <el-select v-model="selectedUser" data-testid="login-user" style="width: 100%">
            <el-option
              v-for="user in users"
              :key="user.value"
              :label="user.label"
              :value="user.value"
            />
          </el-select>
        </el-form-item>
        <el-alert
          v-if="auth.errorMessage"
          type="error"
          :closable="false"
          show-icon
          :title="auth.errorMessage"
        />
        <el-button
          class="login-button"
          data-testid="login-submit"
          type="primary"
          native-type="submit"
          :loading="auth.loading"
        >
          进入 AIDT Demo
        </el-button>
      </el-form>
      <small>本地开发身份不允许用于生产环境，系统不会伪造企业 SSO 已接通。</small>
    </section>
  </main>
</template>

<style scoped>
.login-shell {
  display: grid;
  min-height: 100vh;
  place-items: center;
  padding: 24px;
  background:
    radial-gradient(circle at 25% 15%, rgb(190 225 208 / 50%), transparent 26rem),
    linear-gradient(155deg, #eef5f0, #dbe9e0);
}

.login-card {
  width: min(480px, 100%);
  border: 1px solid var(--aidt-border);
  border-radius: 26px;
  background: rgb(255 255 255 / 94%);
  box-shadow: 0 24px 70px rgb(17 58 42 / 12%);
  padding: clamp(26px, 6vw, 44px);
}

h1 {
  margin: 0;
  font-size: clamp(1.8rem, 5vw, 2.6rem);
}

p {
  color: var(--aidt-ink-muted);
  line-height: 1.8;
}

.login-button {
  width: 100%;
  margin-top: 16px;
}

small {
  display: block;
  margin-top: 18px;
  color: var(--aidt-ink-muted);
  line-height: 1.65;
}
</style>
