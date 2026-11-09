import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import WorkflowFormRenderer from './WorkflowFormRenderer.vue'
import type { WorkflowTaskForm } from '@/api/types/contracts'

const form: WorkflowTaskForm = {
  taskId: 'task-1',
  workflowInstanceId: 'instance-1',
  businessObjectId: 'object-1',
  departmentId: 'department-1',
  nodeCode: 'REVIEW',
  nodeName: '交付资料复核',
  businessDescription: '核对资料、责任人和日期。',
  dueAt: null,
  formSchemaVersion: '1.0',
  formDefinitionHash: 'hash',
  fields: [
    {
      key: 'NOTE',
      displayName: '现场记录',
      dataType: 'Text',
      required: true,
      defaultValue: null,
      readOnly: false,
      visibleRoleCodes: [],
      writeRoleCodes: [],
      options: [],
      width: 'Full',
      sortOrder: 10,
      dataSource: 'TaskInput',
      boundObjectFieldCode: null,
      sensitivity: 'Internal',
      helpText: '填写现场核对结果',
    },
  ],
  actions: [
    {
      code: 'APPROVE',
      name: '通过',
      actionType: 'Approve',
      confirmationText: null,
    },
  ],
  aiReview: {
    enabled: true,
    registeredAction: 'CompletenessCheck',
    agentProfileId: null,
    readableFieldKeys: ['NOTE'],
    materialScopes: ['Attachment'],
    outputMode: 'SuggestionOnly',
  },
  notificationPolicyJson: '{}',
  permanentHumanBoundary: true,
  formDataJson: '{}',
}

describe('WorkflowFormRenderer', () => {
  it('renders business form, controlled actions, and permanent human boundary', () => {
    const wrapper = mount(WorkflowFormRenderer, {
      props: {
        form,
        modelValue: {},
      },
      global: {
        stubs: {
          ElAlert: {
            props: ['title'],
            template: '<div class="alert">{{ title }}</div>',
          },
          ElEmpty: true,
          ElTag: { template: '<span><slot /></span>' },
          WorkflowDynamicField: {
            props: ['field'],
            template: '<div class="stub-field">{{ field.displayName }} · {{ field.key }}</div>',
          },
        },
      },
    })

    expect(wrapper.text()).toContain('交付资料复核')
    expect(wrapper.text()).toContain('现场记录 · NOTE')
    expect(wrapper.text()).toContain('永久人工边界')
    expect(wrapper.text()).toContain('通过')
  })
})
