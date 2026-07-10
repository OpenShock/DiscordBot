import { PUBLIC_API_BASE } from '$env/static/public';
import {
  applyRoster,
  markHubLeft,
  onShock,
  setConsent,
  upsertHubParticipant,
} from '$lib/stores/room.svelte';
import { session } from '$lib/stores/session.svelte';
import type { ConsentChangedEvent, Participant, ShockDeliveredEvent } from '$lib/types';
import * as signalR from '@microsoft/signalr';

const BASE = PUBLIC_API_BASE;

let connection: signalR.HubConnection | null = null;

export async function connectRealtime(): Promise<void> {
  if (!session.jwt || !session.instanceId) return;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${BASE}/hubs/room?instanceId=${encodeURIComponent(session.instanceId)}`, {
      accessTokenFactory: () => session.jwt ?? '',
      // WebSockets only, skipping the negotiate step: the JWT rides the WS URL as ?access_token
      // (the backend reads it for /hubs). This also removes any need for load-balancer sticky
      // sessions across API replicas — the client connects straight to whichever replica answers.
      transport: signalR.HttpTransportType.WebSockets,
      skipNegotiation: true,
    })
    .withAutomaticReconnect()
    .build();

  // Event names match the strongly-typed IRoomClient method names on the backend (PascalCase — SignalR
  // sends the method name verbatim). Payload properties stay camelCase via the JSON protocol.
  connection.on('Roster', (participants: Participant[]) => applyRoster(participants));
  connection.on('ParticipantJoined', (p: Participant) => upsertHubParticipant(p));
  connection.on('ParticipantLeft', (discordId: string) => markHubLeft(discordId));
  connection.on('ConsentChanged', (e: ConsentChangedEvent) =>
    setConsent(e.discordId, e.allowRoomShocks)
  );
  connection.on('ShockDelivered', (e: ShockDeliveredEvent) =>
    onShock(e, session.user?.discordId ?? null)
  );

  await connection.start();
}

export async function disconnectRealtime(): Promise<void> {
  await connection?.stop();
  connection = null;
}
