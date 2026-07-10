import { session } from '$lib/stores/session.svelte';
import { client } from './generated/client.gen';
import {
  deleteLink,
  deleteWhitelistByFriendId,
  getConsent,
  getMe,
  getShockers,
  getWhitelist,
  postAuthToken,
  postControl,
  postLink,
  postWhitelistByFriendId,
  putConsent,
  putShockers,
} from './generated/sdk.gen';
import type { ControlRequestDto } from './generated/types.gen';

// Attach the session JWT to every request. The generated client is bearer-agnostic (the spec has no
// security scheme); the token only exists after the Discord auth flow, so unauthenticated calls (token
// exchange) simply go out without it.
client.interceptors.request.use((request) => {
  if (session.jwt) request.headers.set('Authorization', `Bearer ${session.jwt}`);
  return request;
});

/**
 * Normalizes a rejected request into a plain Error. The client throws the parsed RFC 7807
 * `OpenShockProblem` body on non-2xx; surface its detail/title so callers can show `e.message`.
 */
function toError(e: unknown): Error {
  if (e instanceof Error) return e;
  if (e && typeof e === 'object') {
    const problem = e as { detail?: string | null; title?: string | null };
    if (problem.detail) return new Error(problem.detail);
    if (problem.title) return new Error(problem.title);
  }
  return new Error('Request failed');
}

async function call<T>(request: Promise<T>): Promise<T> {
  try {
    return await request;
  } catch (e) {
    throw toError(e);
  }
}

// Thin, app-shaped facade over the generated hey-api SDK. Keeps call sites terse and centralizes error
// normalization; the underlying functions and types are fully generated from the API's OpenAPI document.
export const api = {
  exchangeToken: (code: string) => call(postAuthToken({ body: { code } })),

  me: () => call(getMe()),

  link: (apiToken: string, apiServer: string) => call(postLink({ body: { apiToken, apiServer } })),
  unlink: () => call(deleteLink()),

  shockers: () => call(getShockers()),
  setShockers: (enabledIds: string[]) => call(putShockers({ body: { enabledIds } })),

  whitelist: () => call(getWhitelist()),
  addWhitelist: (friendId: string) => call(postWhitelistByFriendId({ path: { friendId } })),
  removeWhitelist: (friendId: string) => call(deleteWhitelistByFriendId({ path: { friendId } })),

  consent: () => call(getConsent()),
  setConsent: (allowRoomShocks: boolean, roomMaxIntensity: number, roomMaxDurationMs: number) =>
    call(putConsent({ body: { allowRoomShocks, roomMaxIntensity, roomMaxDurationMs } })),

  control: (payload: ControlRequestDto) => call(postControl({ body: payload })),
};
