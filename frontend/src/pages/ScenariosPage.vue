<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { ApiError } from "@/api/http";
import { scenarioApi } from "@/api/modules/scenarioApi";
import { workspaceApi } from "@/api/modules/workspaceApi";
import type {
  AgentProfileDto,
  DepartmentDto,
  ObjectTypeDto,
  Scenario,
  ScenarioLaunch,
  ScenarioLaunchSummary,
} from "@/api/types/contracts";
import MainColumnPageHeader from "@/components/workspace/MainColumnPageHeader.vue";
import ScenarioManagementPanel from "@/features/scenarios/ScenarioManagementPanel.vue";
import ScenarioStartWizard from "@/features/scenarios/ScenarioStartWizard.vue";
import {
  businessLinkTarget,
  scenarioCategory,
  scenarioErrorGuidance,
  scenarioPositions,
  scenarioRequiresApproval,
  scenarioRiskText,
  scenarioUsesAgent,
} from "@/features/scenarios/scenarioPresentation";
import { useAuthStore } from "@/stores/auth";

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const loading = ref(true);
const errorMessage = ref("");
const scenarios = ref<Scenario[]>([]);
const launches = ref<ScenarioLaunchSummary[]>([]);
const departments = ref<DepartmentDto[]>([]);
const objectTypes = ref<ObjectTypeDto[]>([]);
const agents = ref<AgentProfileDto[]>([]);
const search = ref("");
const departmentFilter = ref("");
const roleFilter = ref("");
const categoryFilter = ref("");
const currentView = ref<"catalog" | "my" | "manage">(
  route.query.mode === "manage"
    ? "manage"
    : route.query.view === "my"
      ? "my"
      : "catalog",
);
const detail = ref<Scenario | null>(null);
const detailVisible = ref(false);
const startVisible = ref(false);
const selectedLaunch = ref<ScenarioLaunchSummary | null>(null);
const launchVisible = ref(false);

const canManage = computed(
  () => auth.can("scenario:create") || auth.can("scenario:update"),
);
const categories = computed(() =>
  [
    ...new Set(scenarios.value.map((scenario) => scenarioCategory(scenario))),
  ].sort(),
);
const roles = computed(() =>
  [
    ...new Set(
      scenarios.value.flatMap(
        (scenario) =>
          scenario.currentPublishedVersion?.payload.participantRoles ?? [],
      ),
    ),
  ].sort(),
);
const filteredScenarios = computed(() => {
  const term = search.value.trim().toLowerCase();
  return scenarios.value.filter((scenario) => {
    const payload = scenario.currentPublishedVersion?.payload;
    const haystack = [
      scenario.name,
      scenario.description,
      scenario.ownerDepartmentName,
      ...(payload?.participantRoles ?? []),
      ...(payload?.preparationItems.map((item) => item.name) ?? []),
    ]
      .join(" ")
      .toLowerCase();
    if (term && !haystack.includes(term)) return false;
    if (
      departmentFilter.value &&
      !payload?.applicableDepartmentCodes.includes(departmentFilter.value) &&
      scenario.ownerDepartmentId !==
        departments.value.find((item) => item.code === departmentFilter.value)
          ?.id
    ) {
      return false;
    }
    if (
      roleFilter.value &&
      !payload?.participantRoles.includes(roleFilter.value)
    )
      return false;
    if (
      categoryFilter.value &&
      scenarioCategory(scenario) !== categoryFilter.value
    )
      return false;
    return true;
  });
});
const recommended = computed(() =>
  [...filteredScenarios.value]
    .sort(
      (left, right) => recommendationScore(right) - recommendationScore(left),
    )
    .slice(0, 3),
);
const activeLaunches = computed(() =>
  launches.value.filter((item) => !item.isCompleted),
);
const initiatedScenarioIds = computed(
  () => new Set(launches.value.map((item) => item.scenarioId)),
);
const initiatedScenarios = computed(() =>
  scenarios.value.filter((scenario) =>
    initiatedScenarioIds.value.has(scenario.id),
  ),
);

function recommendationScore(scenario: Scenario): number {
  const payload = scenario.currentPublishedVersion?.payload;
  let score = 0;
  if (
    payload?.applicableDepartmentCodes.some((code) =>
      departments.value.some(
        (department) => department.code === code && department.isPrimary,
      ),
    )
  ) {
    score += 10;
  }
  if (payload?.startMode === "EventInbox") score += 2;
  if (payload?.permanentHumanBoundaries.length) score += 1;
  return score;
}

function showError(error: unknown) {
  if (error instanceof ApiError) {
    errorMessage.value = scenarioErrorGuidance(error.code, error.message);
  } else {
    errorMessage.value =
      "无法加载岗位业务场景。请稍后重试；如持续失败，请联系平台管理员。";
  }
}

async function load() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const [
      scenarioPage,
      launchPage,
      departmentRows,
      objectTypeRows,
      agentRows,
    ] = await Promise.all([
      scenarioApi.listVisible(),
      scenarioApi.myLaunches(),
      workspaceApi.departments(),
      workspaceApi.publishedObjectTypes(),
      workspaceApi.agents(),
    ]);
    scenarios.value = scenarioPage.items;
    launches.value = launchPage.items;
    departments.value = departmentRows;
    objectTypes.value = objectTypeRows;
    agents.value = agentRows;
    const requestedScenario = String(route.query.scenario ?? "");
    if (requestedScenario) {
      const matched = scenarios.value.find(
        (item) => item.id === requestedScenario,
      );
      if (matched) {
        detail.value = matched;
        detailVisible.value = true;
      } else {
        errorMessage.value =
          "该场景不存在、已停用或当前岗位无权使用。请联系部门负责人选择可用场景。";
      }
    }
    const requestedLaunch = String(route.query.launch ?? "");
    if (requestedLaunch) {
      const matched = launches.value.find(
        (item) => item.id === requestedLaunch,
      );
      if (matched) {
        selectedLaunch.value = matched;
        launchVisible.value = true;
        currentView.value = "my";
      } else {
        errorMessage.value =
          "该场景记录不存在或不属于当前用户。请返回我的场景重新选择。";
      }
    }
  } catch (error) {
    showError(error);
  } finally {
    loading.value = false;
  }
}

function openDetail(scenario: Scenario) {
  detail.value = scenario;
  detailVisible.value = true;
  void router.replace({
    path: "/scenarios",
    query: { ...route.query, scenario: scenario.id },
  });
}

function startScenario(scenario: Scenario) {
  detail.value = scenario;
  detailVisible.value = false;
  startVisible.value = true;
}

async function onStarted(launch: ScenarioLaunch) {
  await load();
  const summary = launches.value.find((item) => item.id === launch.id);
  if (summary) {
    selectedLaunch.value = summary;
    currentView.value = "my";
  }
}

function openLaunch(item: ScenarioLaunchSummary) {
  selectedLaunch.value = item;
  launchVisible.value = true;
}

function openLink(path: string) {
  void router.push(path);
}

function openLinkRecord(
  type: string,
  id: string,
  name: string,
  role: string,
  path: string,
) {
  openLink(businessLinkTarget({ type, id, name, role, detailPath: path }));
}

function dueText(value: string | null): string {
  if (!value) return "未设置";
  return new Intl.DateTimeFormat("zh-CN", {
    timeZone: "Asia/Shanghai",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(new Date(value));
}

function launchedScenarioCount(): number {
  return new Set(launches.value.map((item) => item.scenarioId)).size;
}

function setView(view: "catalog" | "my" | "manage") {
  currentView.value = view;
  void router.replace({
    path: "/scenarios",
    query: view === "catalog" ? {} : { view },
  });
}

watch(activeLaunches, (items) => {
  if (currentView.value === "my" && !items.length && !launches.value.length) {
    currentView.value = "catalog";
  }
});

onMounted(load);
</script>

<template>
  <div class="page-shell scenarios-page" data-testid="scenarios-page">
    <MainColumnPageHeader
      intro-key="scenarios"
      title="岗位业务场景"
      scope-label="当前部门与岗位可办理范围"
      kicker="按业务目标办事 · Demo 环境"
    >
      <template #actions>
        <div class="hero-actions">
          <el-button @click="setView('my')">我的场景</el-button>
          <el-button v-if="canManage" type="primary" @click="setView('manage')"
            >场景配置</el-button
          >
        </div>
      </template>
    </MainColumnPageHeader>

    <section class="question-strip" aria-label="场景中心回答的问题">
      <article>
        <span>1</span><strong>我现在要办什么事？</strong
        ><small>按岗位与业务目标查找</small>
      </article>
      <article>
        <span>2</span><strong>我需要准备什么？</strong
        ><small>材料、文件、听记和真实对象</small>
      </article>
      <article>
        <span>3</span><strong>下一步是谁处理？</strong
        ><small>责任岗位、审批路径和人工边界</small>
      </article>
      <article>
        <span>4</span><strong>结果和证据在哪里？</strong
        ><small>对象、流程、任务、文件与审计</small>
      </article>
    </section>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <div class="view-tabs">
      <button
        :class="{ active: currentView === 'catalog' }"
        @click="setView('catalog')"
      >
        场景目录 <span>{{ scenarios.length }}</span>
      </button>
      <button :class="{ active: currentView === 'my' }" @click="setView('my')">
        我的场景 <span>{{ launches.length }}</span>
      </button>
      <button
        v-if="canManage"
        :class="{ active: currentView === 'manage' }"
        @click="setView('manage')"
      >
        配置治理
      </button>
    </div>

    <template v-if="currentView === 'catalog'">
      <section class="section-card filter-panel">
        <el-input
          v-model="search"
          clearable
          placeholder="搜索业务事项、岗位或准备材料"
          class="search-input"
        >
          <template #prefix>搜索</template>
        </el-input>
        <el-select v-model="departmentFilter" clearable placeholder="适用部门">
          <el-option
            v-for="department in departments"
            :key="department.id"
            :label="department.name"
            :value="department.code"
          />
        </el-select>
        <el-select
          v-model="roleFilter"
          clearable
          filterable
          placeholder="岗位/业务角色"
        >
          <el-option
            v-for="role in roles"
            :key="role"
            :label="role"
            :value="role"
          />
        </el-select>
        <el-select v-model="categoryFilter" clearable placeholder="场景分类">
          <el-option
            v-for="category in categories"
            :key="category"
            :label="category"
            :value="category"
          />
        </el-select>
      </section>

      <section v-loading="loading" class="catalog-body">
        <div class="section-heading">
          <div>
            <p class="page-kicker">推荐场景</p>
            <h2>从你所在岗位最常办理的事项开始</h2>
          </div>
          <el-tag type="warning" effect="plain"
            >受控 Demo 试点样例，不代表生产事项</el-tag
          >
        </div>
        <div v-if="recommended.length" class="scenario-grid featured">
          <article
            v-for="scenario in recommended"
            :key="scenario.id"
            class="scenario-card"
            tabindex="0"
            @click="openDetail(scenario)"
            @keydown.enter="openDetail(scenario)"
          >
            <div class="card-top">
              <span class="category">{{ scenarioCategory(scenario) }}</span>
              <el-tag type="success" effect="plain">当前可发起</el-tag>
            </div>
            <h3>{{ scenario.name }}</h3>
            <p>{{ scenario.description }}</p>
            <dl>
              <div>
                <dt>适用岗位</dt>
                <dd>{{ scenarioPositions(scenario) }}</dd>
              </div>
              <div>
                <dt>预计步骤</dt>
                <dd>
                  {{
                    scenario.currentPublishedVersion?.payload.steps.length ?? 0
                  }}
                  步
                </dd>
              </div>
              <div>
                <dt>发起材料</dt>
                <dd>
                  {{
                    scenario.currentPublishedVersion?.payload.preparationItems
                      .map((item) => item.name)
                      .join("、") || "按业务信息准备"
                  }}
                </dd>
              </div>
            </dl>
            <div class="card-tags">
              <el-tag
                :type="scenarioRequiresApproval(scenario) ? 'warning' : 'info'"
                effect="plain"
              >
                {{
                  scenarioRequiresApproval(scenario)
                    ? "需要审批"
                    : "经理真人确认"
                }}
              </el-tag>
              <el-tag
                :type="scenarioUsesAgent(scenario) ? 'primary' : 'info'"
                effect="plain"
              >
                {{
                  scenarioUsesAgent(scenario)
                    ? "使用数字员工建议"
                    : "不使用数字员工"
                }}
              </el-tag>
              <el-tag type="danger" effect="plain">永久人工边界</el-tag>
            </div>
            <p class="risk-copy">
              <strong>风险提示：</strong>{{ scenarioRiskText(scenario) }}
            </p>
            <div class="card-actions">
              <el-button @click.stop="openDetail(scenario)">查看详情</el-button>
              <el-button type="primary" @click.stop="startScenario(scenario)"
                >开始办理</el-button
              >
            </div>
          </article>
        </div>

        <div class="section-heading all-heading">
          <div>
            <p class="page-kicker">全部可用场景</p>
            <h2>搜索结果</h2>
          </div>
          <span>{{ filteredScenarios.length }} 个场景</span>
        </div>
        <div v-if="filteredScenarios.length" class="scenario-grid">
          <article
            v-for="scenario in filteredScenarios"
            :key="scenario.id"
            class="scenario-card"
            tabindex="0"
            @click="openDetail(scenario)"
            @keydown.enter="openDetail(scenario)"
          >
            <div class="card-top">
              <span class="category">{{ scenarioCategory(scenario) }}</span>
              <el-tag
                :type="
                  initiatedScenarioIds.has(scenario.id) ? 'success' : 'info'
                "
                effect="plain"
              >
                {{
                  initiatedScenarioIds.has(scenario.id)
                    ? "已有发起记录"
                    : "当前可发起"
                }}
              </el-tag>
            </div>
            <h3>{{ scenario.name }}</h3>
            <p>{{ scenario.description }}</p>
            <dl>
              <div>
                <dt>适用岗位</dt>
                <dd>{{ scenarioPositions(scenario) }}</dd>
              </div>
              <div>
                <dt>步骤</dt>
                <dd>
                  {{ scenario.currentPublishedVersion?.payload.steps.length }}
                  步
                </dd>
              </div>
              <div>
                <dt>材料</dt>
                <dd>
                  {{
                    scenario.currentPublishedVersion?.payload.preparationItems
                      .length
                  }}
                  项
                </dd>
              </div>
            </dl>
            <div class="card-tags">
              <el-tag
                :type="scenarioRequiresApproval(scenario) ? 'warning' : 'info'"
                effect="plain"
              >
                {{
                  scenarioRequiresApproval(scenario) ? "需要审批" : "经理确认"
                }}
              </el-tag>
              <el-tag
                :type="scenarioUsesAgent(scenario) ? 'primary' : 'info'"
                effect="plain"
              >
                {{ scenarioUsesAgent(scenario) ? "数字员工辅助" : "纯人工" }}
              </el-tag>
            </div>
            <p class="risk-copy">{{ scenarioRiskText(scenario) }}</p>
            <div class="card-actions">
              <el-button @click.stop="openDetail(scenario)">详情</el-button>
              <el-button type="primary" @click.stop="startScenario(scenario)"
                >开始办理</el-button
              >
            </div>
          </article>
        </div>
        <el-empty
          v-else-if="!loading"
          description="没有符合筛选条件的场景。可清空条件，或联系部门负责人配置新的业务场景。"
        >
          <el-button
            @click="
              search = '';
              departmentFilter = '';
              roleFilter = '';
              categoryFilter = '';
            "
          >
            清空筛选
          </el-button>
        </el-empty>
      </section>
    </template>

    <template v-else-if="currentView === 'my'">
      <section class="launch-summary section-card">
        <article>
          <span>最近进行中</span><strong>{{ activeLaunches.length }}</strong>
        </article>
        <article>
          <span>我发起过</span><strong>{{ launchedScenarioCount() }}</strong>
        </article>
        <article>
          <span>缺少材料</span
          ><strong>{{
            launches.filter((item) => item.missingMaterials.length).length
          }}</strong>
        </article>
        <article>
          <span>已完成</span
          ><strong>{{
            launches.filter((item) => item.isCompleted).length
          }}</strong>
        </article>
      </section>
      <section class="my-scenario-layout">
        <main class="section-card">
          <div class="section-heading">
            <div>
              <h2>我发起的场景</h2>
              <p>进度、等待人和下一步由流程、任务、项目和事件实时计算。</p>
            </div>
            <el-button @click="load">刷新</el-button>
          </div>
          <div v-if="launches.length" class="launch-list">
            <article
              v-for="item in launches"
              :key="item.id"
              class="launch-card"
            >
              <div class="launch-main">
                <div class="launch-title">
                  <span>{{ item.scenarioName }}</span>
                  <el-tag v-if="item.isDemo" type="warning" effect="plain"
                    >Demo 环境</el-tag
                  >
                  <el-tag
                    :type="item.isCompleted ? 'info' : 'success'"
                    effect="plain"
                  >
                    {{ item.isCompleted ? "已结束" : "进行中" }}
                  </el-tag>
                </div>
                <p><strong>当前步骤：</strong>{{ item.currentStep }}</p>
                <p>
                  <strong>等待谁：</strong>{{ item.waitingFor }} ·
                  <strong>截止：</strong>{{ dueText(item.dueAt) }}
                </p>
                <p>
                  <strong>流程：</strong>{{ item.workflowStatus }} ·
                  <strong>任务：</strong>{{ item.taskStatus }}
                </p>
                <p><strong>下一步：</strong>{{ item.nextAction }}</p>
                <div class="launch-tags">
                  <el-tag effect="plain"
                    >已上传 {{ item.uploadedMaterialCount }} 项</el-tag
                  >
                  <el-tag
                    v-if="item.missingMaterials.length"
                    type="danger"
                    effect="plain"
                  >
                    缺少：{{ item.missingMaterials.join("、") }}
                  </el-tag>
                  <el-tag v-else type="success" effect="plain">材料齐全</el-tag>
                  <el-tag type="info" effect="plain">{{
                    item.projectName
                  }}</el-tag>
                </div>
              </div>
              <div class="launch-actions">
                <el-button @click="openLaunch(item)">查看下一步</el-button>
                <el-button type="primary" @click="openLink(item.evidencePath)"
                  >完整证据</el-button
                >
              </div>
            </article>
          </div>
          <el-empty
            v-else
            description="你还没有发起过场景。可从目录选择一个与你岗位相关的业务目标。"
          >
            <el-button type="primary" @click="setView('catalog')"
              >选择业务场景</el-button
            >
          </el-empty>
        </main>
        <aside class="section-card">
          <h2>最近进行中的场景</h2>
          <button
            v-for="item in activeLaunches.slice(0, 6)"
            :key="item.id"
            @click="openLaunch(item)"
          >
            <strong>{{ item.scenarioName }}</strong>
            <span>{{ item.currentStep }} · 等待 {{ item.waitingFor }}</span>
          </button>
          <el-empty
            v-if="!activeLaunches.length"
            description="没有进行中的场景"
          />
          <h3>我发起过的场景</h3>
          <p>
            {{
              initiatedScenarios.map((item) => item.name).join("、") ||
              "暂无历史场景"
            }}
          </p>
        </aside>
      </section>
    </template>

    <ScenarioManagementPanel v-else-if="currentView === 'manage'" />

    <el-dialog
      v-model="detailVisible"
      :title="detail?.name || '场景详情'"
      width="min(900px, 96vw)"
    >
      <div v-if="detail && detail.currentPublishedVersion" class="detail-body">
        <div class="detail-hero">
          <div>
            <p class="page-kicker">
              {{ scenarioCategory(detail) }} · 受控 Demo 试点样例
            </p>
            <h2>{{ detail.description }}</h2>
          </div>
          <el-button type="primary" @click="startScenario(detail)"
            >按此场景开始办理</el-button
          >
        </div>
        <div class="detail-grid">
          <article>
            <h3>业务目标</h3>
            <p>{{ detail.currentPublishedVersion.payload.problemSolved }}</p>
          </article>
          <article>
            <h3>发起前准备</h3>
            <ul>
              <li
                v-for="item in detail.currentPublishedVersion.payload
                  .preparationItems"
                :key="item.code"
              >
                {{ item.name
                }}{{ item.isRequired ? "（必填）" : "（可选）" }}：{{
                  item.description
                }}
              </li>
            </ul>
          </article>
        </div>
        <section>
          <h3>每一步做什么、谁负责、形成什么证据</h3>
          <el-timeline>
            <el-timeline-item
              v-for="step in detail.currentPublishedVersion.payload.steps"
              :key="step.sequence"
              :type="step.permanentHumanBoundary ? 'danger' : 'primary'"
            >
              <strong>{{ step.name }} · {{ step.responsibleRole }}</strong>
              <p>{{ step.action }}</p>
              <small
                >完成条件：{{ step.expectedResult }} · 证据：{{
                  step.evidence
                }}</small
              >
              <el-tag
                v-if="step.permanentHumanBoundary"
                type="danger"
                effect="plain"
                >永久人工边界</el-tag
              >
            </el-timeline-item>
          </el-timeline>
        </section>
        <div class="detail-grid">
          <article>
            <h3>文件与听记</h3>
            <p>
              文件来源：{{
                detail.currentPublishedVersion.payload.supportedFileSources.join(
                  "、",
                ) || "未配置"
              }}
            </p>
            <p>
              听记来源：{{
                detail.currentPublishedVersion.payload.supportedMinutesSources.join(
                  "、",
                ) || "未配置"
              }}
            </p>
          </article>
          <article>
            <h3>数字员工</h3>
            <p
              v-for="agent in detail.currentPublishedVersion.payload
                .allowedAgents"
              :key="agent.agentProfileCode"
            >
              {{ agent.capability }}；{{ agent.boundary }}
            </p>
            <p>
              <strong>不会做：</strong
              >自动确认、自动审批、自动付款、质量放行、修改批准配方、控制设备或自动关闭项目。
            </p>
          </article>
        </div>
        <section class="boundary-panel">
          <h3>永久人工边界与风险提示</h3>
          <div class="boundary-tags">
            <el-tag
              v-for="boundary in detail.currentPublishedVersion.payload
                .permanentHumanBoundaries"
              :key="boundary"
              type="danger"
              effect="plain"
            >
              {{ boundary }}
            </el-tag>
          </div>
          <p>{{ scenarioRiskText(detail) }}</p>
        </section>
        <section>
          <h3>完成后形成的数据和证据</h3>
          <ul>
            <li
              v-for="evidence in detail.currentPublishedVersion.payload
                .completionEvidence"
              :key="evidence"
            >
              {{ evidence }}
            </li>
          </ul>
        </section>
      </div>
      <template #footer>
        <el-button @click="detailVisible = false">关闭</el-button>
        <el-button v-if="detail" type="primary" @click="startScenario(detail)"
          >开始办理</el-button
        >
      </template>
    </el-dialog>

    <el-dialog
      v-model="launchVisible"
      :title="selectedLaunch?.scenarioName || '我的场景'"
      width="min(820px, 96vw)"
    >
      <div v-if="selectedLaunch" class="launch-detail">
        <div class="launch-title">
          <strong>{{ selectedLaunch.scenarioName }}</strong>
          <el-tag v-if="selectedLaunch.isDemo" type="warning" effect="plain"
            >Demo 环境</el-tag
          >
          <el-tag :type="selectedLaunch.isCompleted ? 'info' : 'success'">
            {{ selectedLaunch.isCompleted ? "已结束" : "进行中" }}
          </el-tag>
        </div>
        <div class="detail-grid">
          <article>
            <span>当前步骤</span
            ><strong>{{ selectedLaunch.currentStep }}</strong>
          </article>
          <article>
            <span>等待谁处理</span
            ><strong>{{ selectedLaunch.waitingFor }}</strong>
          </article>
          <article>
            <span>截止时间</span
            ><strong>{{ dueText(selectedLaunch.dueAt) }}</strong>
          </article>
          <article>
            <span>关联项目</span
            ><strong>{{ selectedLaunch.projectName }}</strong>
          </article>
          <article>
            <span>流程状态</span
            ><strong>{{ selectedLaunch.workflowStatus }}</strong>
          </article>
          <article>
            <span>任务状态</span
            ><strong>{{ selectedLaunch.taskStatus }}</strong>
          </article>
        </div>
        <el-alert
          :type="selectedLaunch.missingMaterials.length ? 'error' : 'success'"
          :closable="false"
          :title="
            selectedLaunch.missingMaterials.length
              ? `缺少材料：${selectedLaunch.missingMaterials.join('、')}。请联系发起部门补齐。`
              : `已上传 ${selectedLaunch.uploadedMaterialCount} 项材料，当前没有缺失项。`
          "
        />
        <article class="next-box">
          <span>数字员工建议</span>
          <p>{{ selectedLaunch.agentSuggestion }}</p>
          <span>下一步动作</span>
          <strong>{{ selectedLaunch.nextAction }}</strong>
        </article>
        <h3>业务证据入口</h3>
        <div class="evidence-links">
          <el-button
            v-for="link in selectedLaunch.businessLinks"
            :key="`${link.type}-${link.id}`"
            @click="
              openLinkRecord(
                link.type,
                link.id,
                link.name,
                link.role,
                link.detailPath,
              )
            "
          >
            {{ link.role }} · {{ link.name }}
          </el-button>
          <el-button
            type="primary"
            @click="openLink(selectedLaunch.evidencePath)"
            >完整审计证据</el-button
          >
        </div>
      </div>
    </el-dialog>

    <ScenarioStartWizard
      v-model:visible="startVisible"
      :scenario="detail"
      :departments="departments"
      :object-types="objectTypes"
      :agents="agents"
      @started="onStarted"
    />
  </div>
</template>

<style scoped>
.scenario-hero,
.view-tabs,
.section-heading,
.card-top,
.card-actions,
.launch-title,
.launch-actions,
.detail-hero {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.scenario-hero {
  align-items: flex-start;
  border-radius: 24px;
  background:
    radial-gradient(
      circle at 90% 0%,
      rgb(247 214 159 / 38%),
      transparent 19rem
    ),
    linear-gradient(145deg, #ffffff, #edf5ef);
  border: 1px solid var(--aidt-border);
  padding: clamp(20px, 4vw, 36px);
  box-shadow: var(--aidt-shadow);
}

.hero-actions,
.card-tags,
.launch-tags,
.boundary-tags,
.evidence-links {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
}

.question-strip {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.question-strip article {
  display: grid;
  gap: 6px;
  border: 1px solid var(--aidt-border);
  border-radius: 15px;
  background: #fff;
  padding: 14px;
}

.question-strip span {
  display: grid;
  width: 27px;
  height: 27px;
  place-items: center;
  border-radius: 9px;
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand-strong);
  font-size: 0.74rem;
  font-weight: 800;
}

.question-strip strong {
  font-size: 0.86rem;
}

.question-strip small {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  line-height: 1.5;
}

.view-tabs {
  justify-content: flex-start;
  border-bottom: 1px solid var(--aidt-border);
}

.view-tabs button {
  border: 0;
  border-bottom: 3px solid transparent;
  background: transparent;
  padding: 10px 14px;
  color: var(--aidt-ink-muted);
  font-weight: 700;
  cursor: pointer;
}

.view-tabs button.active {
  border-color: var(--aidt-brand);
  color: var(--aidt-brand-strong);
}

.view-tabs span {
  border-radius: 999px;
  background: #e9f1ec;
  padding: 2px 7px;
  font-size: 0.68rem;
}

.filter-panel {
  display: grid;
  grid-template-columns: minmax(220px, 1.4fr) repeat(3, minmax(140px, 1fr));
  gap: 10px;
  padding: 14px;
}

.search-input :deep(input) {
  width: 100%;
}

.catalog-body {
  display: grid;
  gap: 15px;
  min-height: 200px;
}

.section-heading h2,
.section-heading h3,
.section-heading p {
  margin: 0;
}

.section-heading p {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.all-heading {
  margin-top: 10px;
}

.scenario-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.scenario-card {
  display: grid;
  align-content: start;
  gap: 11px;
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 18px;
  background: #fff;
  padding: 18px;
  box-shadow: 0 10px 24px rgb(23 66 46 / 5%);
  cursor: pointer;
  transition:
    transform 160ms ease,
    border-color 160ms ease;
}

.scenario-card:hover,
.scenario-card:focus-visible {
  transform: translateY(-2px);
  border-color: #9fc1ae;
}

.scenario-card h3 {
  margin: 0;
  font-size: 1.12rem;
}

.scenario-card > p {
  margin: 0;
  color: var(--aidt-ink-muted);
  line-height: 1.65;
}

.category {
  border-radius: 999px;
  background: #e9f3ed;
  padding: 5px 9px;
  color: var(--aidt-brand-strong);
  font-size: 0.68rem;
  font-weight: 800;
}

.scenario-card dl {
  display: grid;
  gap: 7px;
  margin: 0;
}

.scenario-card dl > div {
  display: grid;
  grid-template-columns: 70px minmax(0, 1fr);
  gap: 8px;
  border-top: 1px solid #eef2ef;
  padding-top: 7px;
}

.scenario-card dt {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.scenario-card dd {
  overflow: hidden;
  margin: 0;
  font-size: 0.74rem;
  text-overflow: ellipsis;
}

.risk-copy {
  border-radius: 11px;
  background: var(--aidt-warning-soft);
  padding: 9px;
  color: #6d4b17 !important;
  font-size: 0.72rem;
}

.card-actions {
  justify-content: flex-end;
  margin-top: auto;
}

.launch-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.launch-summary article {
  border-right: 1px solid var(--aidt-border);
}

.launch-summary article:last-child {
  border-right: 0;
}

.launch-summary span,
.launch-summary strong {
  display: block;
}

.launch-summary span {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.launch-summary strong {
  margin-top: 4px;
  color: var(--aidt-brand-strong);
  font-size: 1.6rem;
}

.my-scenario-layout {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 280px;
  align-items: start;
  gap: 14px;
}

.my-scenario-layout main,
.my-scenario-layout aside {
  display: grid;
  gap: 12px;
}

.launch-list {
  display: grid;
  gap: 10px;
}

.launch-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 15px;
  border: 1px solid var(--aidt-border);
  border-radius: 15px;
  padding: 14px;
}

.launch-main {
  min-width: 0;
}

.launch-main p {
  margin: 6px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.launch-title span,
.launch-title strong {
  font-size: 1rem;
  font-weight: 800;
}

.launch-actions {
  flex: 0 0 auto;
  align-items: flex-end;
  flex-direction: column;
}

.my-scenario-layout aside button {
  display: grid;
  gap: 3px;
  border: 0;
  border-left: 3px solid var(--aidt-brand);
  background: #f6faf7;
  padding: 10px;
  color: var(--aidt-ink);
  text-align: left;
  cursor: pointer;
}

.my-scenario-layout aside span {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.detail-body,
.launch-detail {
  display: grid;
  gap: 16px;
}

.detail-hero {
  align-items: flex-start;
  border-radius: 16px;
  background: #f0f6f2;
  padding: 16px;
}

.detail-hero h2 {
  margin: 0;
  font-size: 1.1rem;
  line-height: 1.55;
}

.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.detail-grid article,
.boundary-panel,
.next-box {
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fbfdfb;
  padding: 14px;
}

.detail-grid h3,
.detail-grid p,
.boundary-panel h3,
.next-box p {
  margin: 0;
}

.detail-grid p,
.detail-grid li,
.boundary-panel p {
  color: var(--aidt-ink-muted);
  line-height: 1.65;
}

.boundary-panel {
  background: var(--aidt-danger-soft);
}

.next-box {
  display: grid;
  gap: 5px;
  background: #eef5f1;
}

.next-box > span {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.next-box > p {
  margin-bottom: 8px;
}

@media (max-width: 1100px) {
  .scenario-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filter-panel {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .scenario-hero,
  .section-heading,
  .launch-card,
  .detail-hero {
    align-items: stretch;
    flex-direction: column;
  }

  .question-strip,
  .scenario-grid,
  .filter-panel,
  .launch-summary,
  .my-scenario-layout,
  .detail-grid {
    grid-template-columns: 1fr;
  }

  .launch-summary article {
    border-right: 0;
    border-bottom: 1px solid var(--aidt-border);
    padding-bottom: 8px;
  }

  .launch-summary article:last-child {
    border-bottom: 0;
  }

  .launch-actions {
    align-items: stretch;
  }

  .view-tabs {
    overflow-x: auto;
  }

  .view-tabs button {
    flex: 0 0 auto;
  }
}
</style>
