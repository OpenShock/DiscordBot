import { session } from '$lib/stores/session.svelte';
import type {
	AuthUser,
	ControlPayload,
	LinkResponse,
	MeResponse,
	ShockerDto,
	WhitelistEntry
} from '$lib/types';

const BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? '/api';

async function req<T>(path: string, options: RequestInit = {}): Promise<T> {
	const headers = new Headers(options.headers);
	if (session.jwt) headers.set('Authorization', `Bearer ${session.jwt}`);
	if (options.body) headers.set('Content-Type', 'application/json');

	const res = await fetch(`${BASE}${path}`, { ...options, headers });
	if (!res.ok) {
		let message = `Request failed (${res.status})`;
		try {
			const body = await res.json();
			if (body?.error) message = body.error;
		} catch {
			/* ignore non-JSON error bodies */
		}
		throw new Error(message);
	}

	if (res.status === 204) return undefined as T;
	return (await res.json()) as T;
}

export interface TokenResponse {
	discordAccessToken: string;
	jwt: string;
	user: AuthUser;
}

export const api = {
	exchangeToken: (code: string) =>
		req<TokenResponse>('/auth/token', { method: 'POST', body: JSON.stringify({ code }) }),

	me: () => req<MeResponse>('/me'),

	link: (apiToken: string, apiServer: string) =>
		req<LinkResponse>('/link', { method: 'POST', body: JSON.stringify({ apiToken, apiServer }) }),
	unlink: () => req<void>('/link', { method: 'DELETE' }),

	shockers: () => req<ShockerDto[]>('/shockers'),
	setShockers: (enabledIds: string[]) =>
		req<void>('/shockers', { method: 'PUT', body: JSON.stringify({ enabledIds }) }),

	whitelist: () => req<WhitelistEntry[]>('/whitelist'),
	addWhitelist: (friendId: string) => req<void>(`/whitelist/${friendId}`, { method: 'POST' }),
	removeWhitelist: (friendId: string) => req<void>(`/whitelist/${friendId}`, { method: 'DELETE' }),

	consent: () => req<MeResponse>('/consent'),
	setConsent: (allowRoomShocks: boolean, roomMaxIntensity: number, roomMaxDurationMs: number) =>
		req<MeResponse>('/consent', {
			method: 'PUT',
			body: JSON.stringify({ allowRoomShocks, roomMaxIntensity, roomMaxDurationMs })
		}),

	control: (payload: ControlPayload) =>
		req<{ intensity: number; durationSeconds: number }>('/control', {
			method: 'POST',
			body: JSON.stringify(payload)
		})
};
