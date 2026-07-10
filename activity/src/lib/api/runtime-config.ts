import { PUBLIC_API_BASE } from '$env/static/public';
import type { CreateClientConfig } from './generated/client.gen';

// The API serves its routes at root; the browser calls them under "/api" (the prefix Discord strips in
// production and the Vite dev proxy strips locally). PUBLIC_API_BASE can override this for other setups.
export const createClientConfig: CreateClientConfig = (config) => ({
  ...config,
  baseUrl: PUBLIC_API_BASE,
  throwOnError: true,
  responseStyle: 'data',
});
