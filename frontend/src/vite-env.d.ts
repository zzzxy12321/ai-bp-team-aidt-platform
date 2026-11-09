/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  readonly VITE_AUTH_ENFORCEMENT?: 'contract-preview' | 'enforced'
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
