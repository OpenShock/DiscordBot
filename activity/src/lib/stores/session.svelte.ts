import type { AuthUser } from '$lib/types';

export type SessionStatus = 'connecting' | 'ready' | 'error';

// Global session state. Populated by the Discord auth flow in $lib/discord.ts.
export const session = $state<{
  jwt: string | null;
  user: AuthUser | null;
  instanceId: string | null;
  status: SessionStatus;
  error: string | null;
}>({
  jwt: null,
  user: null,
  instanceId: null,
  status: 'connecting',
  error: null,
});
