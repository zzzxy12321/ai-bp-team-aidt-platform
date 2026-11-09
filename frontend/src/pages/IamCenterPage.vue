<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { iamApi } from '@/api/modules/iamApi'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import type {
  AccessChangeRequestDto,
  IdentityAdministrationSnapshotDto,
  IamUserListItemDto,
} from '@/api/types/contracts'
import IamOrganizationPanel from '@/features/iam/components/IamOrganizationPanel.vue'
import IamPeoplePanel from '@/features/iam/components/IamPeoplePanel.vue'
import IamRolesPanel from '@/features/iam/components/IamRolesPanel.vue'
import IamNavigationPanel from '@/features/iam/components/IamNavigationPanel.vue'
import IamWorkbenchPanel from '@/features/iam/components/IamWorkbenchPanel.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const snapshot = ref<IdentityAdministrationSnapshotDto | null>(null)
const allUsers = ref<IamUserListItemDto[]>([])
const activeTab = ref('organization')
const loading = ref(false)
const errorMessage = ref('')
const reviewVisible = ref(false)
const reviewComment = ref('')
const reviewTarget = ref<AccessChangeRequestDto | null>(null)
const reviewApprove = ref(true)
const canReview = computed(() => auth.can('identity-admin:manage'))

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [nextSnapshot, users] = await Promise.all([
      iamApi.getSnapshot(),
      iamApi.listAllUsers(),
    ])
    snapshot.value = nextSnapshot
    allUsers.value = users
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '读取 IAM 配置失败。'
  } finally {
    loading.value = false
  }
}

function openReview(request: AccessChangeRequestDto, approve: boolean) {
  reviewTarget.value = request
  reviewApprove.value = approve
  reviewComment.value = ''
  reviewVisible.value = true
}

async function submitReview() {
  const request = reviewTarget.value
  const comment = reviewComment.value.trim()
  if (!request || !comment) {
    ElMessage.warning('通过或驳回都必须填写审核意见。')
    return
  }
  if (reviewApprove.value) {
    await iamApi.approveAccessRequest(request.id, comment)
    ElMessage.success('高风险授权已由第二名管理员审核并应用')
  } else {
    await iamApi.rejectAccessRequest(request.id, comment)
    ElMessage.success('高风险授权已驳回并写入审计')
  }
  reviewVisible.value = false
  await load()
}

onMounted(load)
</script>

<template>
  <div class="iam-page" data-testid="iam-center-page">
    <MainColumnPageHeader
      intro-key="iam"
      title="IAM 与导航"
      scope-label="组织、角色、数据范围与导航"
      kicker="PLATFORM GOVERNANCE"
    >
      <template #actions>
        <el-button :loading="loading" @click="load">刷新配置</el-button>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />
    <el-skeleton v-if="loading && !snapshot" :rows="8" animated />

    <template v-if="snapshot">
      <el-tabs v-model="activeTab">
        <el-tab-pane label="组织架构" name="organization">
          <IamOrganizationPanel :tree="snapshot.organizationTree" :users="allUsers" @changed="load" />
        </el-tab-pane>
        <el-tab-pane label="人员与数据范围" name="people">
          <IamPeoplePanel :tree="snapshot.organizationTree" :users="allUsers" @changed="load" />
        </el-tab-pane>
        <el-tab-pane label="角色与权限矩阵" name="roles">
          <IamRolesPanel
            :roles="snapshot.roles"
            :users="allUsers"
            :permissions="snapshot.permissions"
            :tree="snapshot.organizationTree"
            @changed="load"
          />
        </el-tab-pane>
        <el-tab-pane label="导航与工作区" name="navigation">
          <IamNavigationPanel :tree="snapshot.organizationTree" @changed="load" />
        </el-tab-pane>
        <el-tab-pane label="工作台模板" name="workbench">
          <IamWorkbenchPanel
            :tree="snapshot.organizationTree"
            :roles="snapshot.roles"
            @changed="load"
          />
        </el-tab-pane>
        <el-tab-pane :label="`高风险审核 (${snapshot.pendingAccessRequests.length})`" name="access">
          <section class="iam-card" data-testid="iam-access-panel">
            <div class="card-heading">
              <div>
                <h2>高风险授权双人复核</h2>
                <p>申请人不能审核自己的系统角色或管理型权限授权。</p>
              </div>
            </div>
            <el-empty v-if="!snapshot.pendingAccessRequests.length" description="没有待审核授权" />
            <div v-else class="access-request-list">
              <article
                v-for="request in snapshot.pendingAccessRequests"
                :key="request.id"
                class="access-request"
                :data-request-id="request.id"
              >
                <div>
                  <strong>{{ request.requestType }}</strong>
                  <p>{{ request.riskReason }}</p>
                  <small>申请人：{{ request.requestedByDisplayName }} · {{ request.requestedAt }}</small>
                </div>
                <div v-if="canReview" class="access-actions">
                  <el-button type="primary" plain @click="openReview(request, true)">复核通过</el-button>
                  <el-button type="danger" plain @click="openReview(request, false)">复核驳回</el-button>
                </div>
              </article>
            </div>
          </section>
        </el-tab-pane>
      </el-tabs>
    </template>

    <el-dialog
      v-model="reviewVisible"
      :title="reviewApprove ? '复核通过高风险授权' : '复核驳回高风险授权'"
      width="min(520px, 92vw)"
    >
      <el-alert
        type="warning"
        :closable="false"
        title="审核意见为必填项；申请人不能审核自己的申请。"
      />
      <el-input
        v-model="reviewComment"
        type="textarea"
        :rows="4"
        maxlength="1000"
        show-word-limit
        placeholder="说明通过或驳回的业务理由"
      />
      <template #footer>
        <el-button @click="reviewVisible = false">取消</el-button>
        <el-button
          :type="reviewApprove ? 'primary' : 'danger'"
          @click="submitReview"
        >
          {{ reviewApprove ? '确认通过' : '确认驳回' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style>
.iam-page {
  display: grid;
  gap: 24px;
}

.iam-page-header,
.card-heading,
.heading-actions,
.form-actions,
.scope-builder,
.member-summary,
.access-request {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.iam-page-header h1,
.iam-card h2 {
  margin: 0;
}

.iam-page-header p,
.iam-card p {
  color: var(--aidt-ink-muted);
  line-height: 1.7;
}

.iam-grid {
  display: grid;
  grid-template-columns: minmax(280px, 0.8fr) minmax(0, 1.4fr);
  gap: 20px;
}

.iam-card {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 20px;
  background: rgb(255 255 255 / 92%);
  padding: 22px;
  box-shadow: 0 14px 38px rgb(18 60 45 / 6%);
}

.role-list {
  align-self: start;
}

.role-item {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 8px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: white;
  padding: 12px 14px;
  text-align: left;
  cursor: pointer;
}

.role-item.active {
  border-color: #2f6b53;
  background: #edf7f1;
}

.role-item span:first-child,
.role-item small {
  display: block;
}

.role-item small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
}

.iam-details {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  margin: 18px 0;
}

.iam-details div {
  border-radius: 12px;
  background: #f4f7f5;
  padding: 12px;
}

.iam-details dt {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.iam-details dd {
  margin: 4px 0 0;
  font-weight: 700;
}

.iam-form,
.permission-sections {
  display: grid;
  gap: 14px;
}

.form-actions,
.heading-actions {
  flex-wrap: wrap;
}

.people-heading {
  align-items: flex-end;
}

.heading-actions .el-input,
.heading-actions .el-select,
.scope-builder .el-select {
  width: min(240px, 100%);
}

.permission-group {
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  padding: 14px;
}

.permission-group h3 {
  margin: 0 0 10px;
  font-size: 0.9rem;
}

.permission-group .el-checkbox-group,
.member-picker,
.scope-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
}

.member-summary {
  justify-content: flex-start;
  margin-top: 16px;
  color: var(--aidt-ink-muted);
}

.access-request-list {
  display: grid;
  gap: 12px;
}

.access-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.access-request {
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  padding: 14px;
}

.access-request p {
  margin: 6px 0;
}

@media (max-width: 980px) {
  .iam-grid,
  .iam-details {
    grid-template-columns: 1fr;
  }

  .iam-page-header,
  .card-heading,
  .people-heading,
  .access-request {
    align-items: flex-start;
    flex-direction: column;
  }
}

@media (max-width: 560px) {
  .iam-card {
    padding: 16px;
  }

  .heading-actions,
  .heading-actions .el-input,
  .heading-actions .el-select,
  .heading-actions .el-button,
  .scope-builder,
  .scope-builder .el-select,
  .scope-builder .el-button {
    width: 100%;
  }
}
</style>
