import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import EnvironmentTag from './EnvironmentTag.vue'

describe('EnvironmentTag', () => {
  it('labels demo test data explicitly', () => {
    render(EnvironmentTag, {
      props: { code: 'demo', isTestData: true },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
        },
      },
    })

    expect(screen.getByText('演示环境 · 测试数据')).toBeTruthy()
  })

  it('does not label production as test data', () => {
    render(EnvironmentTag, {
      props: { code: 'prod', isTestData: false },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
        },
      },
    })

    expect(screen.getByText('生产环境')).toBeTruthy()
  })
})
