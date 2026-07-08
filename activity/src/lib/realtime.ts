import * as signalR from '@microsoft/signalr';
import { session } from '$lib/stores/session.svelte';
import {
	applyRoster,
	markHubLeft,
	onShock,
	setConsent,
	upsertHubParticipant
} from '$lib/stores/room.svelte';
import type { ConsentChangedEvent, Participant, ShockDeliveredEvent } from '$lib/types';

const BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? '/api';

let connection: signalR.HubConnection | null = null;

export async function connectRealtime(): Promise<void> {
	if (!session.jwt || !session.instanceId) return;

	connection = new signalR.HubConnectionBuilder()
		.withUrl(`${BASE}/hubs/room?instanceId=${encodeURIComponent(session.instanceId)}`, {
			accessTokenFactory: () => session.jwt ?? ''
		})
		.withAutomaticReconnect()
		.build();

	connection.on('roster', (participants: Participant[]) => applyRoster(participants));
	connection.on('participantJoined', (p: Participant) => upsertHubParticipant(p));
	connection.on('participantLeft', (discordId: string) => markHubLeft(discordId));
	connection.on('consentChanged', (e: ConsentChangedEvent) => setConsent(e.discordId, e.allowRoomShocks));
	connection.on('shockDelivered', (e: ShockDeliveredEvent) => onShock(e, session.user?.discordId ?? null));

	await connection.start();
}

export async function disconnectRealtime(): Promise<void> {
	await connection?.stop();
	connection = null;
}
