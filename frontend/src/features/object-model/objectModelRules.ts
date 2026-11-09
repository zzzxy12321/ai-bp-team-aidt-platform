import type { ObjectFieldDto, ObjectTypeDto } from '@/api/types/contracts'

export const objectFieldTypes = [
  'Text',
  'LongText',
  'Number',
  'Date',
  'DateTime',
  'Boolean',
  'Select',
  'User',
  'Department',
] as const

export const fieldTypeLabels: Record<string, string> = {
  Text: '单行文本',
  LongText: '长文本',
  Number: '数字',
  Date: '日期',
  DateTime: '日期时间',
  Boolean: '布尔',
  Select: '下拉选择',
  User: '人员',
  Department: '部门',
}

export function createObjectField(index = 1): ObjectFieldDto {
  return {
    key: `FIELD_${index}`,
    displayName: `字段 ${index}`,
    dataType: 'Text',
    isRequired: false,
    maxLength: 100,
    sensitivity: 'Internal',
    defaultValue: null,
    options: [],
    showInList: true,
    filterable: false,
    sortOrder: index * 10,
    code: `FIELD_${index}`,
  }
}

export function normalizeField(field: ObjectFieldDto): ObjectFieldDto {
  const code = (field.code || field.key).trim()
  return {
    ...field,
    key: code,
    code,
    displayName: field.displayName.trim(),
    dataType: objectFieldTypes.includes(field.dataType as typeof objectFieldTypes[number])
      ? field.dataType
      : 'Text',
    maxLength: Math.max(1, Math.min(100_000, Number(field.maxLength) || 1)),
    defaultValue: field.defaultValue?.trim() || null,
    options:
      field.dataType === 'Select'
        ? [...new Set(field.options.map((option) => option.trim()).filter(Boolean))]
        : [],
    sortOrder: (field.sortOrder ?? 0) || 1,
  }
}

export function validateFieldValue(
  field: ObjectFieldDto,
  value: string | null | undefined,
): string | null {
  const normalized = value?.trim() ?? ''
  if (!normalized) {
    return field.isRequired ? `${field.displayName}为必填项` : null
  }

  if (normalized.length > field.maxLength) {
    return `${field.displayName}不能超过 ${field.maxLength} 个字符`
  }

  switch (field.dataType) {
    case 'Number':
      return Number.isFinite(Number(normalized)) ? null : `${field.displayName}必须是数字`
    case 'Date':
      return /^\d{4}-\d{2}-\d{2}$/.test(normalized)
        && !Number.isNaN(Date.parse(`${normalized}T00:00:00Z`))
        ? null
        : `${field.displayName}必须是有效日期`
    case 'DateTime':
      return Number.isNaN(Date.parse(normalized))
        ? `${field.displayName}必须是有效日期时间`
        : null
    case 'Boolean':
      return ['true', 'false'].includes(normalized)
        ? null
        : `${field.displayName}必须是 true 或 false`
    case 'Select':
      return field.options.some((option) => option.toLowerCase() === normalized.toLowerCase())
        ? null
        : `${field.displayName}的值不在允许选项中`
    case 'User':
    case 'Department':
      return /^[0-9a-f]{8}-[0-9a-f-]{27,}$/i.test(normalized)
        ? null
        : `${field.displayName}必须选择有效记录`
    default:
      return null
  }
}

export function validateObjectTypeDraft(
  name: string,
  fields: ObjectFieldDto[],
): string[] {
  const errors: string[] = []
  if (!name.trim()) errors.push('对象类型名称不能为空')
  if (!fields.length) errors.push('至少需要一个字段')

  const knownCodes = new Set<string>()
  fields.forEach((field, index) => {
    const normalized = normalizeField(field)
    if (!normalized.displayName) errors.push(`第 ${index + 1} 个字段缺少显示名称`)
    if (!/^[A-Za-z][A-Za-z0-9_-]*$/.test(normalized.key)) {
      errors.push(`字段 ${normalized.key || index + 1} 编码必须以字母开头且只含字母、数字、下划线或短横线`)
    }
    const lowerCode = normalized.key.toLowerCase()
    if (knownCodes.has(lowerCode)) errors.push(`字段编码 ${normalized.key} 重复`)
    knownCodes.add(lowerCode)
    if (normalized.maxLength < 1 || normalized.maxLength > 100_000) {
      errors.push(`字段 ${normalized.displayName} 长度必须在 1—100000 之间`)
    }
    if (normalized.dataType === 'Select' && !normalized.options.length) {
      errors.push(`字段 ${normalized.displayName} 至少需要一个下拉选项`)
    }
    if (normalized.dataType !== 'Select' && normalized.options.length) {
      errors.push(`字段 ${normalized.displayName} 只有下拉选择类型可以配置选项`)
    }
    if (normalized.defaultValue) {
      const valueError = validateFieldValue({ ...normalized, isRequired: false }, normalized.defaultValue)
      if (valueError) errors.push(`默认值错误：${valueError}`)
    }
  })

  return [...new Set(errors)]
}

export function formatFieldValue(
  field: ObjectFieldDto | undefined,
  value: string | null | undefined,
): string {
  if (!value) return '—'
  if (!field) return value
  if (field.dataType === 'Boolean') return value === 'true' ? '是' : '否'
  if (field.dataType === 'Date') return value
  if (field.dataType === 'DateTime') {
    const parsed = new Date(value)
    return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleString('zh-CN')
  }
  return value
}

export function objectTypeStatusLabel(status: ObjectTypeDto['status']): string {
  return {
    Draft: '草稿',
    Validated: '已校验',
    Published: '已发布',
    Retired: '已停用',
    Superseded: '历史版本',
  }[status]
}

export function objectTypeStatusTone(
  status: ObjectTypeDto['status'],
): 'success' | 'warning' | 'danger' | 'info' | 'neutral' {
  if (status === 'Published') return 'success'
  if (status === 'Retired') return 'danger'
  if (status === 'Validated') return 'info'
  if (status === 'Draft') return 'warning'
  return 'neutral'
}

export function buildFieldPreviewValues(fields: ObjectFieldDto[]): Record<string, string> {
  return Object.fromEntries(
    fields.map((field) => [
      field.key,
      field.defaultValue
        ?? (field.dataType === 'Boolean'
          ? 'false'
          : field.dataType === 'Select'
            ? field.options[0] ?? ''
            : ''),
    ]),
  )
}
