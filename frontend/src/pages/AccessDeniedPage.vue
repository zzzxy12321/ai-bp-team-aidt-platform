<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

<template>
  <div class="page-shell">
    <section class="section-card">
      <p class="page-kicker">403 · Permission</p>
      <h1 class="page-title">当前身份没有访问权限</h1>
      <p class="page-lead">
        路由守卫已启用，但隐藏菜单不能替代 API 权限校验。正式身份源接入后，后端仍会独立校验数据范围和动作权限。
      </p>
      <el-alert
        v-if="route.query.permission"
        class="permission-alert"
        type="warning"
        :closable="false"
        :title="`缺少权限：${route.query.permission}`"
      />
    </section>
  </div>
</template>

<style scoped>
.permission-alert {
  margin-top: 20px;
}
</style>
