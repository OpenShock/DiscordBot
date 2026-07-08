import devtoolsJson from 'vite-plugin-devtools-json';
import { defineConfig } from 'vitest/config';
import { playwright } from '@vitest/browser-playwright';
import tailwindcss from '@tailwindcss/vite';
import { sveltekit } from '@sveltejs/kit/vite';

// Where the local OpenShock.Activity.Api is listening.
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5199';

// The API serves its routes at root; the browser calls them under "/api" (the same prefix Discord
// strips in production). Locally we replicate that by stripping "/api" here and proxying to the API.
// `ws: true` also carries the SignalR WebSocket at /api/hubs/room.
const proxy = {
	'/api': {
		target: apiTarget,
		changeOrigin: true,
		ws: true,
		rewrite: (path: string) => path.replace(/^\/api/, '')
	}
};

export default defineConfig({
	plugins: [tailwindcss(), sveltekit(), devtoolsJson()],
	server: {
		// Accept the tunnel host (e.g. *.trycloudflare.com) that Discord's URL override points at.
		allowedHosts: true,
		proxy
	},
	preview: {
		allowedHosts: true,
		proxy
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
						instances: [{ browser: 'chromium', headless: true }]
					},
					include: ['src/**/*.svelte.{test,spec}.{js,ts}'],
					exclude: ['src/lib/server/**']
				}
			},
			{
				extends: './vite.config.ts',
				test: {
					name: 'server',
					environment: 'node',
					include: ['src/**/*.{test,spec}.{js,ts}'],
					exclude: ['src/**/*.svelte.{test,spec}.{js,ts}']
				}
			}
		]
	}
});
