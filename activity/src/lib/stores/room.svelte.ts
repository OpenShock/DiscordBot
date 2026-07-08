import type { Participant, ShockDeliveredEvent } from '$lib/types';

export interface RoomMember {
	discordId: string;
	name: string;
	avatar: string | null;
	allowRoomShocks: boolean;
	present: boolean;
}

export interface FeedItem {
	id: number;
	text: string;
	ts: number;
	self: boolean;
}

export const room = $state<{
	members: Record<string, RoomMember>;
	feed: FeedItem[];
}>({
	members: {},
	feed: []
});

let feedSeq = 0;

function ensure(discordId: string): RoomMember {
	return (
		room.members[discordId] ?? {
			discordId,
			name: 'Unknown',
			avatar: null,
			allowRoomShocks: false,
			present: false
		}
	);
}

/** Raw Discord participant object from the Embedded App SDK. */
interface DiscordParticipant {
	id: string;
	username?: string;
	global_name?: string | null;
	nickname?: string | null;
	avatar?: string | null;
}

function avatarUrl(p: DiscordParticipant): string | null {
	if (!p.avatar) return null;
	return `https://cdn.discordapp.com/avatars/${p.id}/${p.avatar}.png?size=64`;
}

/** Names/avatars + presence come from the Discord SDK participant list. */
export function setDiscordParticipants(participants: DiscordParticipant[]) {
	const present = new Set(participants.map((p) => String(p.id)));
	const next: Record<string, RoomMember> = {};

	for (const p of participants) {
		const id = String(p.id);
		const existing = room.members[id];
		next[id] = {
			discordId: id,
			name: p.global_name || p.nickname || p.username || existing?.name || 'Unknown',
			avatar: avatarUrl(p) ?? existing?.avatar ?? null,
			allowRoomShocks: existing?.allowRoomShocks ?? false,
			present: true
		};
	}
	// keep hub members that Discord hasn't reported yet
	for (const [id, m] of Object.entries(room.members)) {
		if (!present.has(id)) next[id] = { ...m, present: m.present };
	}
	room.members = next;
}

/** Consent flags + confirmed hub presence come from the SignalR roster. */
export function applyRoster(participants: Participant[]) {
	for (const p of participants) upsertHubParticipant(p);
}

export function upsertHubParticipant(p: Participant) {
	const base = ensure(p.discordId);
	room.members = {
		...room.members,
		[p.discordId]: { ...base, name: base.name === 'Unknown' ? p.name : base.name, allowRoomShocks: p.allowRoomShocks, present: true }
	};
}

export function markHubLeft(discordId: string) {
	const m = room.members[discordId];
	if (m) room.members = { ...room.members, [discordId]: { ...m, present: false } };
}

export function setConsent(discordId: string, allowRoomShocks: boolean) {
	const m = room.members[discordId];
	if (m) room.members = { ...room.members, [discordId]: { ...m, allowRoomShocks } };
}

export function onShock(e: ShockDeliveredEvent, selfId: string | null) {
	const text = `${e.fromName} shocked ${nameOf(e.toDiscordId)} — ${e.intensity}% for ${e.durationSeconds}s`;
	pushFeed(text, e.toDiscordId === selfId);
}

export function pushFeed(text: string, self = false) {
	room.feed = [{ id: ++feedSeq, text, ts: Date.now(), self }, ...room.feed].slice(0, 40);
}

function nameOf(discordId: string): string {
	return room.members[discordId]?.name ?? 'someone';
}
