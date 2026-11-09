import { describe, expect, it } from 'vitest'
import {
  createObjectField,
  formatFieldValue,
  validateFieldValue,
  validateObjectTypeDraft,
} from './objectModelRules'

describe('objectModelRules', () => {
  it('detects duplicate keys, missing select options and invalid defaults', () => {
    const first = {
      ...createObjectField(1),
      key: 'Status',
      code: 'Status',
      displayName: '状态',
      dataType: 'Select',
      options: [],
    }
    const second = {
      ...createObjectField(2),
      key: 'status',
      code: 'status',
      defaultValue: 'Unknown',
    }

    const errors = validateObjectTypeDraft('', [first, second])
    expect(errors).toContain('对象类型名称不能为空')
    expect(errors).toContain('字段编码 status 重复')
    expect(errors.some((message) => message.includes('至少需要一个下拉选项'))).toBe(true)
  })

  it('validates all supported business field values', () => {
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Number' }, '12.5')).toBeNull()
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Number' }, 'abc')).toContain('必须是数字')
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Date' }, '2026-11-02')).toBeNull()
    expect(validateFieldValue({ ...createObjectField(), dataType: 'DateTime' }, '2026-11-02T18:00:00+08:00')).toBeNull()
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Boolean' }, 'true')).toBeNull()
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Boolean' }, 'yes')).toContain('true 或 false')
    expect(validateFieldValue({ ...createObjectField(), dataType: 'Select', options: ['A'] }, 'B')).toContain('不在允许选项中')
    expect(validateFieldValue({ ...createObjectField(), dataType: 'User' }, 'not-a-guid')).toContain('有效记录')
  })

  it('formats boolean and date-time values for detail display', () => {
    const field = createObjectField()
    expect(formatFieldValue({ ...field, dataType: 'Boolean' }, 'true')).toBe('是')
    expect(formatFieldValue({ ...field, dataType: 'Date' }, '2026-11-02')).toBe('2026-11-02')
    expect(formatFieldValue({ ...field, dataType: 'DateTime' }, '2026-11-02T10:00:00Z')).not.toBe('—')
  })
})
