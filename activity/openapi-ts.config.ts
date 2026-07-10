import { defineConfig } from '@hey-api/openapi-ts';

// Generates the typed client for OpenShock.Activity.Api from its OpenAPI document. The spec snapshot
// (openapi.json) is refreshed from the running API via `pnpm regen-api`. Mirrors the cloud frontend's
// hey-api setup, adapted for the Activity API's JWT bearer auth (injected in src/lib/api/index.ts).
export default defineConfig({
  input: './openapi.json',
  output: {
    path: 'src/lib/api/generated',
    postProcess: ['prettier'],
  },
  plugins: [
    {
      name: '@hey-api/client-fetch',
      runtimeConfigPath: './src/lib/api/runtime-config.ts',
      throwOnError: true,
    },
    {
      name: '@hey-api/sdk',
      auth: false,
      operations: { strategy: 'flat' },
      responseStyle: 'data',
    },
    {
      name: '@hey-api/typescript',
      enums: { mode: 'javascript', case: 'PascalCase' },
    },
  ],
});
