<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ApiError } from '@/api/http'
import { platformApi } from '@/api/modules/platformApi'
import { fileApi } from '@/api/modules/fileApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  BusinessObjectDto,
  DepartmentDto,
  FileAttachmentDto,
  FileVersionDto,
} from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const departments = ref<DepartmentDto[]>([])
const objects = ref<BusinessObjectDto[]>([])
const files = ref<FileAttachmentDto[]>([])
const selectedDepartmentId = ref('')
const includeTestData = ref(false)
const loading = ref(true)
const errorMessage = ref('')
const maxUploadBytes = ref(0)

const uploadDialog = ref(false)
const selectedFile = ref<File | null>(null)
const sensitivity = ref<'Internal' | 'Sensitive' | 'Core'>('Internal')
const selectedObjectId = ref('')
const uploading = ref(false)

const previewDialog = ref(false)
const previewFile = ref<FileAttachmentDto | null>(null)
const previewUrl = ref('')
const previewText = ref('')
const previewLoading = ref(false)

const versionsDialog = ref(false)
const versions = ref<FileVersionDto[]>([])
const versionFile = ref<FileAttachmentDto | null>(null)
const versionInput = ref<HTMLInputElement | null>(null)
const pendingVersionId = ref('')

const selectedObjects = computed(() =>
  objects.value.filter((item) => item.departmentId === selectedDepartmentId.value),
)

function messageFor(error: unknown, fallback: string): string {
  return error instanceof ApiError ? `${error.message}（${error.code}）` : fallback
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    departments.value = await workspaceApi.departments()
    const capabilities = await platformApi.getCapabilities()
    maxUploadBytes.value = capabilities.maxUploadBytes
    if (!selectedDepartmentId.value) {
      selectedDepartmentId.value =
        departments.value.find((item) => item.isPrimary)?.id ??
        departments.value[0]?.id ??
        ''
    }
    const allObjects = await workspaceApi.businessObjects(includeTestData.value)
    objects.value = allObjects.items
    if (selectedDepartmentId.value) {
      const result = await fileApi.list({
        departmentId: selectedDepartmentId.value,
        includeTestData: includeTestData.value,
      })
      files.value = result.items
    } else {
      files.value = []
    }
  } catch (error) {
    errorMessage.value = messageFor(error, '无法加载文件中心。')
  } finally {
    loading.value = false
  }
}

function openUpload() {
  selectedFile.value = null
  selectedObjectId.value = ''
  sensitivity.value = 'Internal'
  uploadDialog.value = true
}

function chooseUploadFile(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

function dropUploadFile(event: DragEvent) {
  selectedFile.value = event.dataTransfer?.files[0] ?? null
}

async function submitUpload() {
  if (!selectedFile.value || !selectedDepartmentId.value) {
    ElMessage.warning('请选择部门和文件。')
    return
  }
  uploading.value = true
  try {
    await fileApi.upload({
      file: selectedFile.value,
      departmentId: selectedDepartmentId.value,
      businessObjectId: selectedObjectId.value || undefined,
      sensitivity: sensitivity.value,
    })
    uploadDialog.value = false
    ElMessage.success('文件已上传并记录审计。')
    await load()
  } catch (error) {
    ElMessage.error(messageFor(error, '上传失败。'))
  } finally {
    uploading.value = false
  }
}

function clearPreview() {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
  previewUrl.value = ''
  previewText.value = ''
}

async function preview(file: FileAttachmentDto) {
  clearPreview()
  previewFile.value = file
  previewDialog.value = true
  previewLoading.value = true
  try {
    const blob = await fileApi.preview(file.id, includeTestData.value)
    if (file.previewKind === 'text') {
      previewText.value = await blob.text()
    } else {
      previewUrl.value = URL.createObjectURL(blob)
    }
  } catch (error) {
    previewDialog.value = false
    ElMessage.error(messageFor(error, '预览失败。'))
  } finally {
    previewLoading.value = false
  }
}

async function download(file: FileAttachmentDto) {
  try {
    const blob = await fileApi.download(file.id, includeTestData.value)
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = file.originalFileName
    anchor.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(messageFor(error, '下载失败。'))
  }
}

async function showVersions(file: FileAttachmentDto) {
  versionFile.value = file
  versionsDialog.value = true
  try {
    versions.value = await fileApi.versions(file.id, includeTestData.value)
  } catch (error) {
    versionsDialog.value = false
    ElMessage.error(messageFor(error, '读取版本失败。'))
  }
}

function beginVersion(id: string) {
  pendingVersionId.value = id
  versionInput.value?.click()
}

async function uploadVersion(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file || !pendingVersionId.value || !selectedDepartmentId.value) return
  try {
    await fileApi.addVersion(pendingVersionId.value, {
      file,
      departmentId: selectedDepartmentId.value,
      sensitivity: sensitivity.value,
    })
    ElMessage.success('新版本已保留，旧版本仍可追溯。')
    versionsDialog.value = false
    await load()
  } catch (error) {
    ElMessage.error(messageFor(error, '新增版本失败。'))
  }
}

async function requestDeletion(file: FileAttachmentDto) {
  let reason: string
  try {
    const result = await ElMessageBox.prompt('请填写删除原因。', '申请删除文件', {
      inputPattern: /\S+/,
      inputErrorMessage: '删除原因不能为空',
      confirmButtonText: '提交申请',
    })
    reason = result.value
  } catch {
    return
  }

  try {
    await fileApi.requestDeletion(file.id, reason)
    ElMessage.success('删除申请已记录，文件内容不会被静默删除。')
    await load()
  } catch (error) {
    ElMessage.error(messageFor(error, '删除申请失败。'))
  }
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function formatMaxUploadSize(): string {
  return maxUploadBytes.value
    ? `${(maxUploadBytes.value / 1024 / 1024).toFixed(0)} MiB`
    : '以服务端配置为准'
}

onMounted(load)
onBeforeUnmount(clearPreview)
</script>

<template>
  <div class="page-shell" data-testid="documents-page">
    <MainColumnPageHeader
      intro-key="documents"
      title="部门文件"
      scope-label="当前部门 · 文件版本与业务证据"
      kicker="部门文件中心"
    >
      <template #actions>
        <el-button type="primary" @click="openUpload">上传文件</el-button>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <section class="toolbar section-card">
      <div class="toolbar-group">
        <span>部门</span>
        <el-select v-model="selectedDepartmentId" @change="load">
          <el-option
            v-for="department in departments"
            :key="department.id"
            :label="department.name"
            :value="department.id"
          />
        </el-select>
      </div>
      <el-tag effect="plain">单文件上限 {{ formatMaxUploadSize() }}</el-tag>
      <el-tag effect="plain">默认仅显示当前环境</el-tag>
      <label v-if="auth.isPlatformAdmin" class="admin-filter">
        <el-switch v-model="includeTestData" @change="load" />
        查看跨环境测试数据
      </label>
    </section>

    <section v-loading="loading" class="file-grid" aria-label="文件列表">
      <article v-for="file in files" :key="file.id" class="file-card">
        <div class="file-card-head">
          <span class="file-icon">{{ file.previewKind === 'image' ? '图' : file.previewKind === 'pdf' ? 'PDF' : '档' }}</span>
          <EnvironmentTag :code="file.environmentCode" :is-test-data="file.isTestData" />
        </div>
        <h2>{{ file.originalFileName }}</h2>
        <p>
          版本 {{ file.currentVersionNumber }} · {{ formatSize(file.sizeBytes) }} ·
          {{ file.sensitivity === 'Internal' ? '内部' : file.sensitivity === 'Sensitive' ? '敏感' : '核心' }}
        </p>
        <div class="file-meta">
          <el-tag :type="file.status === 'Active' ? 'success' : 'warning'" effect="plain" size="small">
            {{ file.status === 'Active' ? '有效' : '已申请删除' }}
          </el-tag>
          <el-tag type="info" effect="plain" size="small">{{ file.virusScanStatus }}</el-tag>
          <span v-if="file.links.length">{{ file.links.length }} 个业务关联</span>
        </div>
        <div class="file-actions">
          <el-button
            v-if="file.previewKind !== 'unsupported'"
            text
            type="primary"
            @click="preview(file)"
          >
            预览
          </el-button>
          <el-button text @click="download(file)">下载</el-button>
          <el-button text @click="showVersions(file)">版本</el-button>
          <el-button
            v-if="file.status === 'Active'"
            text
            type="danger"
            @click="requestDeletion(file)"
          >
            申请删除
          </el-button>
        </div>
      </article>
      <el-empty v-if="!loading && !files.length" description="当前部门还没有文件" />
    </section>

    <el-dialog v-model="uploadDialog" title="上传部门文件" width="min(520px, 92vw)">
      <div class="dialog-form">
        <label>文件</label>
        <div
          class="drop-zone"
          data-testid="file-drop-zone"
          @dragover.prevent
          @drop.prevent="dropUploadFile"
        >
          <input
            type="file"
            accept=".pdf,.docx,.xlsx,.pptx,.png,.jpg,.jpeg,.txt,.csv"
            @change="chooseUploadFile"
          />
          <p>拖拽文件到此处，或点击选择文件。</p>
          <small v-if="selectedFile">已选择：{{ selectedFile.name }}</small>
        </div>
        <label>关联业务事项（可选）</label>
        <el-select v-model="selectedObjectId" clearable placeholder="不关联具体事项">
          <el-option
            v-for="item in selectedObjects"
            :key="item.id"
            :label="item.objectCode"
            :value="item.id"
          />
        </el-select>
        <label>保密等级</label>
        <el-select v-model="sensitivity" data-testid="sensitivity-select">
          <el-option label="内部" value="Internal" />
          <el-option label="敏感" value="Sensitive" />
          <el-option label="核心" value="Core" />
        </el-select>
        <p class="hint">
          敏感和核心文件会进入待人工复核状态；未放行前普通员工不能下载或预览。
        </p>
      </div>
      <template #footer>
        <el-button @click="uploadDialog = false">取消</el-button>
        <el-button type="primary" :loading="uploading" @click="submitUpload">上传</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="previewDialog"
      :title="previewFile?.originalFileName || '文件预览'"
      width="min(900px, 94vw)"
      @closed="clearPreview"
    >
      <div v-loading="previewLoading" class="preview-area">
        <iframe
          v-if="previewFile?.previewKind === 'pdf' && previewUrl"
          :src="previewUrl"
          title="PDF 文件预览"
        />
        <img
          v-else-if="previewFile?.previewKind === 'image' && previewUrl"
          :src="previewUrl"
          :alt="previewFile.originalFileName"
        />
        <pre v-else-if="previewFile?.previewKind === 'text'">{{ previewText }}</pre>
      </div>
    </el-dialog>

    <el-dialog
      v-model="versionsDialog"
      :title="`版本记录 · ${versionFile?.originalFileName || ''}`"
      width="min(720px, 94vw)"
    >
      <el-table :data="versions">
        <el-table-column prop="versionNumber" label="版本" width="72" />
        <el-table-column prop="originalFileName" label="文件名" min-width="180" />
        <el-table-column prop="sensitivity" label="等级" width="100" />
        <el-table-column prop="virusScanStatus" label="扫描" width="150" />
        <el-table-column prop="createdAt" label="上传时间" min-width="170" />
      </el-table>
      <template #footer>
        <el-button @click="versionsDialog = false">关闭</el-button>
        <el-button
          v-if="versionFile?.status === 'Active'"
          type="primary"
          @click="beginVersion(versionFile.id)"
        >
          上传新版本
        </el-button>
      </template>
    </el-dialog>
    <input
      ref="versionInput"
      class="hidden-input"
      type="file"
      accept=".pdf,.docx,.xlsx,.pptx,.png,.jpg,.jpeg,.txt,.csv"
      @change="uploadVersion"
    />
  </div>
</template>

<style scoped>
.page-head,
.toolbar,
.toolbar-group,
.admin-filter {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-head,
.toolbar {
  justify-content: space-between;
}

.toolbar {
  flex-wrap: wrap;
}

.toolbar-group span,
.admin-filter {
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.toolbar :deep(.el-select) {
  width: 220px;
}

.file-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  min-height: 120px;
}

.file-card {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 17px;
  background: #fff;
  padding: 18px;
  box-shadow: var(--aidt-shadow);
}

.file-card-head,
.file-meta,
.file-actions {
  display: flex;
  align-items: center;
  gap: 9px;
}

.file-card-head,
.file-actions {
  justify-content: space-between;
}

.file-icon {
  display: grid;
  width: 38px;
  height: 38px;
  place-items: center;
  border-radius: 11px;
  background: #e7f3ed;
  color: var(--aidt-brand-strong);
  font-size: 0.72rem;
  font-weight: 800;
}

.file-card h2 {
  overflow: hidden;
  margin: 14px 0 6px;
  font-size: 1rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.file-card p,
.file-meta {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.file-meta {
  flex-wrap: wrap;
  min-height: 26px;
}

.file-actions {
  justify-content: flex-start;
  flex-wrap: wrap;
  margin-top: 12px;
  border-top: 1px solid var(--aidt-border);
  padding-top: 9px;
}

.dialog-form {
  display: grid;
  gap: 10px;
}

.dialog-form label {
  font-size: 0.82rem;
  font-weight: 700;
}

.dialog-form :deep(.el-select) {
  width: 100%;
}

.hint {
  margin: 2px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
  line-height: 1.6;
}

.drop-zone {
  display: grid;
  gap: 8px;
  border: 1px dashed #93b4a6;
  border-radius: 13px;
  background: #f5faf7;
  padding: 16px;
}

.drop-zone p {
  margin: 0;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.drop-zone small {
  color: var(--aidt-brand-strong);
}

.preview-area {
  min-height: 320px;
}

.preview-area iframe,
.preview-area img {
  width: 100%;
  min-height: 65vh;
  border: 0;
  object-fit: contain;
}

.preview-area pre {
  overflow: auto;
  max-height: 65vh;
  margin: 0;
  border-radius: 12px;
  background: #0f2921;
  color: #eff8f3;
  padding: 18px;
  white-space: pre-wrap;
}

.hidden-input {
  display: none;
}

@media (max-width: 1000px) {
  .file-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 680px) {
  .page-head {
    align-items: stretch;
    flex-direction: column;
  }

  .file-grid {
    grid-template-columns: 1fr;
  }

  .toolbar :deep(.el-select) {
    width: min(220px, 65vw);
  }
}
</style>
