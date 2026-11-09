import type { ApiEnvelope } from '@/api/types/contracts'

export class ApiError extends Error {
  readonly code: string
  readonly status: number
  readonly traceId: string

  constructor(
    code: string,
    message: string,
    status: number,
    traceId: string,
  ) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
    this.traceId = traceId
  }
}

const baseUrl = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')
let csrfToken = ''

export function setCsrfToken(token: string) {
  csrfToken = token
}

function createTraceId(): string {
  return crypto.randomUUID().replaceAll('-', '')
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
): Promise<ApiEnvelope<T>> {
  const traceId = createTraceId()
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  headers.set('X-Trace-Id', traceId)
  if (init.body && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  if (csrfToken && ['POST', 'PUT', 'PATCH', 'DELETE'].includes((init.method ?? 'GET').toUpperCase())) {
    headers.set('X-CSRF-TOKEN', csrfToken)
  }

  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers,
      credentials: 'include',
    })
  } catch {
    throw new ApiError(
      'NETWORK_UNAVAILABLE',
      '无法连接 AIDT API，请确认开发服务已启动。',
      0,
      traceId,
    )
  }

  const payload = (await response.json().catch(() => null)) as
    | ApiEnvelope<T>
    | { code?: string; message?: string; traceId?: string }
    | null

  if (!response.ok) {
    throw new ApiError(
      payload?.code ?? 'HTTP_ERROR',
      payload?.message ?? `请求失败（HTTP ${response.status}）。`,
      response.status,
      payload?.traceId ?? response.headers.get('X-Trace-Id') ?? traceId,
    )
  }

  if (!payload || !('data' in payload)) {
    throw new ApiError(
      'INVALID_RESPONSE',
      'API 返回了不符合统一响应契约的内容。',
      response.status,
      response.headers.get('X-Trace-Id') ?? traceId,
    )
  }

  return payload as ApiEnvelope<T>
}

export function createIdempotencyKey(): string {
  return crypto.randomUUID()
}

export async function apiDownload(path: string): Promise<Blob> {
  const traceId = createTraceId()
  const response = await fetch(`${baseUrl}${path}`, {
    headers: {
      Accept: '*/*',
      'X-Trace-Id': traceId,
    },
    credentials: 'include',
  })
  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as
      | { code?: string; message?: string; traceId?: string }
      | null
    throw new ApiError(
      payload?.code ?? 'HTTP_ERROR',
      payload?.message ?? `文件请求失败（HTTP ${response.status}）。`,
      response.status,
      payload?.traceId ?? response.headers.get('X-Trace-Id') ?? traceId,
    )
  }

  return response.blob()
}
