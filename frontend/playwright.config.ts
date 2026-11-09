import { defineConfig } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const edgePath = 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'
const frontendDir = fileURLToPath(new URL('.', import.meta.url))
const rootDir = path.resolve(frontendDir, '..')
const useExistingDeploy = process.env.PLAYWRIGHT_USE_EXISTING_DEPLOY === '1'
const apiPort = process.env.AIDT_E2E_API_PORT ?? '5080'
const workerPort = process.env.AIDT_E2E_WORKER_PORT ?? '5081'
const aiGatewayPort = process.env.AIDT_E2E_AI_GATEWAY_PORT ?? '5100'
const mcpPort = process.env.AIDT_E2E_MCP_PORT ?? '5085'
const frontendPort = process.env.AIDT_E2E_FRONTEND_PORT ?? '5173'

export default defineConfig({
  testDir: './e2e',
  outputDir: '../docs/project/test-artifacts/playwright-results',
  timeout: 90_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [
    ['list'],
    ['json', { outputFile: '../docs/project/test-artifacts/playwright-report.json' }],
    ['html', { outputFolder: '../docs/project/test-artifacts/playwright-html', open: 'never' }],
  ],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${frontendPort}`,
    browserName: 'chromium',
    executablePath: edgePath,
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'desktop-edge',
      grep: /@desktop(?![\w])/,
      use: { viewport: { width: 1440, height: 960 } },
    },
    {
      name: 'desktop-edge-1280',
      grep: /@desktop1280/,
      use: { viewport: { width: 1280, height: 900 } },
    },
    {
      name: 'mobile-edge-390',
      grep: /@mobile/,
      use: { viewport: { width: 390, height: 844 }, isMobile: true },
    },
    {
      name: 'responsive-edge',
      grep: /@responsive/,
      use: { viewport: { width: 1280, height: 900 } },
    },
  ],
  webServer: useExistingDeploy ? undefined : [
    {
      command: `dotnet run --no-build --no-launch-profile --project backend/AIDT.Api -- --urls http://127.0.0.1:${apiPort}`,
      cwd: rootDir,
      url: `http://127.0.0.1:${apiPort}/health/live`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    },
    {
      command: `dotnet run --no-build --no-launch-profile --project backend/AIDT.Worker -- --urls http://127.0.0.1:${workerPort}`,
      cwd: rootDir,
      url: `http://127.0.0.1:${workerPort}/health/live`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    },
    {
      command: `dotnet run --no-build --no-launch-profile --project backend/AIDT.AiGateway -- --urls http://127.0.0.1:${aiGatewayPort}`,
      cwd: rootDir,
      url: `http://127.0.0.1:${aiGatewayPort}/health/live`,
      timeout: 120_000,
      reuseExistingServer: false,
    },
    {
      command: `dotnet run --no-build --no-launch-profile --project backend/AIDT.Mcp -- --urls http://127.0.0.1:${mcpPort}`,
      cwd: rootDir,
      url: `http://127.0.0.1:${mcpPort}/health/live`,
      timeout: 120_000,
      reuseExistingServer: false,
    },
    {
      command: `npm run dev -- --host 127.0.0.1 --port ${frontendPort}`,
      cwd: frontendDir,
      url: `http://127.0.0.1:${frontendPort}/login`,
      timeout: 120_000,
      reuseExistingServer: false,
      env: { VITE_API_PROXY_TARGET: `http://127.0.0.1:${apiPort}` },
    },
  ],
})
