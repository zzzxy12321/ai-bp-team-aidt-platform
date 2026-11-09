import { apiRequest, createIdempotencyKey } from "@/api/http";
import type {
  CreateNextScenarioVersionDraftRequest,
  CreateScenarioDraftRequest,
  PagedResult,
  ReviewScenarioPublishRequest,
  RollbackScenarioRequest,
  Scenario,
  ScenarioActionRequest,
  ScenarioApprovalRequest,
  ScenarioLaunch,
  ScenarioLaunchSummary,
  ScenarioPreflight,
  ScenarioStartInput,
  ScenarioValidation,
  ScenarioVersion,
  UpdateScenarioDraftRequest,
} from "@/api/types/contracts";

export const scenarioApi = {
  async listVisible(page = 1, pageSize = 100): Promise<PagedResult<Scenario>> {
    return (
      await apiRequest<PagedResult<Scenario>>(
        `/api/v1/scenarios?page=${page}&pageSize=${pageSize}`,
      )
    ).data;
  },

  async listManage(
    params: {
      page?: number;
      pageSize?: number;
      status?: string;
      search?: string;
    } = {},
  ): Promise<PagedResult<Scenario>> {
    const query = new URLSearchParams({
      page: String(params.page ?? 1),
      pageSize: String(params.pageSize ?? 100),
    });
    if (params.status) query.set("status", params.status);
    if (params.search) query.set("search", params.search);
    return (
      await apiRequest<PagedResult<Scenario>>(
        `/api/v1/scenarios/manage?${query.toString()}`,
      )
    ).data;
  },

  async get(id: string): Promise<Scenario> {
    return (await apiRequest<Scenario>(`/api/v1/scenarios/${id}`)).data;
  },

  async myLaunches(
    page = 1,
    pageSize = 100,
  ): Promise<PagedResult<ScenarioLaunchSummary>> {
    return (
      await apiRequest<PagedResult<ScenarioLaunchSummary>>(
        `/api/v1/scenarios/launches?page=${page}&pageSize=${pageSize}`,
      )
    ).data;
  },

  async getLaunch(id: string): Promise<ScenarioLaunch> {
    return (
      await apiRequest<ScenarioLaunch>(`/api/v1/scenarios/launches/${id}`)
    ).data;
  },

  async preflight(
    id: string,
    input: ScenarioStartInput,
  ): Promise<ScenarioPreflight> {
    return (
      await apiRequest<ScenarioPreflight>(
        `/api/v1/scenarios/${id}/actions/preflight`,
        { method: "POST", body: JSON.stringify(input) },
      )
    ).data;
  },

  async start(
    id: string,
    input: ScenarioStartInput,
    idempotencyKey = createIdempotencyKey(),
  ): Promise<ScenarioLaunch> {
    return (
      await apiRequest<ScenarioLaunch>(
        `/api/v1/scenarios/${id}/actions/start`,
        {
          method: "POST",
          headers: { "Idempotency-Key": idempotencyKey },
          body: JSON.stringify(input),
        },
      )
    ).data;
  },

  async createDraft(request: CreateScenarioDraftRequest): Promise<Scenario> {
    return (
      await apiRequest<Scenario>("/api/v1/scenarios/drafts", {
        method: "POST",
        headers: { "Idempotency-Key": createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data;
  },

  async createNextDraft(
    id: string,
    request: CreateNextScenarioVersionDraftRequest,
  ): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(`/api/v1/scenarios/${id}/drafts`, {
        method: "POST",
        headers: { "Idempotency-Key": createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data;
  },

  async updateDraft(
    id: string,
    versionId: string,
    request: UpdateScenarioDraftRequest,
  ): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(
        `/api/v1/scenarios/${id}/drafts/${versionId}`,
        {
          method: "PUT",
          headers: { "Idempotency-Key": createIdempotencyKey() },
          body: JSON.stringify(request),
        },
      )
    ).data;
  },

  async validate(
    id: string,
    versionId: string,
    request: ScenarioActionRequest,
  ): Promise<ScenarioValidation> {
    return (
      await apiRequest<ScenarioValidation>(
        `/api/v1/scenarios/${id}/versions/${versionId}/actions/validate`,
        {
          method: "POST",
          headers: { "Idempotency-Key": createIdempotencyKey() },
          body: JSON.stringify(request),
        },
      )
    ).data;
  },

  async publish(
    id: string,
    versionId: string,
    request: ScenarioActionRequest,
  ): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(
        `/api/v1/scenarios/${id}/versions/${versionId}/actions/publish`,
        {
          method: "POST",
          headers: { "Idempotency-Key": createIdempotencyKey() },
          body: JSON.stringify(request),
        },
      )
    ).data;
  },

  async disable(id: string, request: ScenarioActionRequest): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(`/api/v1/scenarios/${id}/actions/disable`, {
        method: "POST",
        headers: { "Idempotency-Key": createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data;
  },

  async restore(id: string, request: ScenarioActionRequest): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(`/api/v1/scenarios/${id}/actions/restore`, {
        method: "POST",
        headers: { "Idempotency-Key": createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data;
  },

  async versions(id: string): Promise<ScenarioVersion[]> {
    return (
      await apiRequest<ScenarioVersion[]>(`/api/v1/scenarios/${id}/versions`)
    ).data;
  },

  async rollback(
    id: string,
    request: RollbackScenarioRequest,
  ): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(`/api/v1/scenarios/${id}/actions/rollback`, {
        method: "POST",
        headers: { "Idempotency-Key": createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data;
  },

  async approvalRequests(
    status = "Pending",
  ): Promise<ScenarioApprovalRequest[]> {
    return (
      await apiRequest<ScenarioApprovalRequest[]>(
        `/api/v1/scenarios/approval-requests?status=${encodeURIComponent(status)}`,
      )
    ).data;
  },

  async reviewApproval(
    requestId: string,
    request: ReviewScenarioPublishRequest,
  ): Promise<Scenario> {
    return (
      await apiRequest<Scenario>(
        `/api/v1/scenarios/approval-requests/${requestId}/actions/review`,
        {
          method: "POST",
          headers: { "Idempotency-Key": createIdempotencyKey() },
          body: JSON.stringify(request),
        },
      )
    ).data;
  },
};
