export type ConfirmationStatus =
  | 'Pending'
  | 'NeedsInformation'
  | 'Expired'
  | 'Confirmed'
  | 'Rejected'
  | 'Failed'

export function eventConfirmationStatus(status: string): ConfirmationStatus {
  if (status === 'Received') return 'NeedsInformation'
  if (status === 'AwaitingConfirmation') return 'Pending'
  if (status === 'Confirmed') return 'Confirmed'
  if (status === 'Rejected') return 'Rejected'
  return 'Pending'
}

export function minutesConfirmationStatus(input: {
  status: string
  assigneeId: string | null
  dueAt: string | null
}): ConfirmationStatus {
  if (input.status === 'Confirmed') return 'Confirmed'
  return input.assigneeId && input.dueAt ? 'Pending' : 'NeedsInformation'
}

export function mcpConfirmationStatus(
  status: 'Pending' | 'Confirmed' | 'Rejected' | 'Expired',
  expiresAt: string,
  now = Date.now(),
): ConfirmationStatus {
  if (status === 'Pending' && new Date(expiresAt).getTime() <= now) return 'Expired'
  return status
}

export function agentConfirmationStatus(status: string): ConfirmationStatus {
  if (status === 'AwaitingConfirmation') return 'Pending'
  if (status === 'Completed' || status === 'Running') return 'Confirmed'
  if (status === 'Failed') return 'Failed'
  if (status === 'Stopped') return 'Rejected'
  return 'Pending'
}

export function canConfirmMinutesAction(input: {
  status: string
  assigneeId: string | null
  dueAt: string | null
  content: string
}) {
  return input.status === 'Draft'
    && Boolean(input.assigneeId)
    && Boolean(input.dueAt)
    && Boolean(input.content.trim())
}
