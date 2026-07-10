import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { playwright } from '@vitest/browser-playwright';
import devtoolsJson from 'vite-plugin-devtools-json';
import { defineConfig } from 'vitest/config';

// Where the local OpenShock.Activity.Api is listening.
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000';

// The API serves its routes at root; the browser calls them under "/api" (the same prefix Discord
// strips in production). Locally we replicate that by stripping "/api" here and proxying to the API.
// `ws: true` also carries the SignalR WebSocket at /api/hubs/room.
const proxy = {
  '/api': {
    target: apiTarget,
    changeOrigin: true,
    ws: true,
    rewrite: (path: string) => path.replace(/^\/api/, ''),
  },
};

export default defineConfig({
  plugins: [tailwindcss(), sveltekit(), devtoolsJson()],
  server: {
    // Accept the tunnel host (e.g. *.trycloudflare.com) that Discord's URL override points at.
    allowedHosts: true,
    // `@openshock/svelte-core` is a workspace package consumed from source. pnpm
    // symlinks it into node_modules, but Vite resolves symlinks to their real path
    // (packages/svelte-core/src/...), which falls outside SvelteKit's default
    // fs.allow list. Allow the package dir so its source modules can be served.
    fs: {
      allow: ['./packages/svelte-core'],
    },
    proxy,
  },
  preview: {
    allowedHosts: true,
    proxy,
  },
  test: {
    expect: { requireAssertions: true },
    projects: [
      {
        extends: './vite.config.ts',
        test: {
          name: 'client',
          browser: {
            enabled: true,
            provider: playwright(),
            instances: [{ browser: 'chromium', headless: true }],
          },
          include: ['src/**/*.svelte.{test,spec}.{js,ts}'],
          exclude: ['src/lib/server/**'],
        },
      },
      {
        extends: './vite.config.ts',
        test: {
          name: 'server',
          environment: 'node',
          include: ['src/**/*.{test,spec}.{js,ts}'],
          exclude: ['src/**/*.svelte.{test,spec}.{js,ts}'],
        },
      },
    ],
  },
});
