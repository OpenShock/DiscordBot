import { PUBLIC_DISCORD_CLIENT_ID } from '$env/static/public';
import { api } from '$lib/api';
import { connectRealtime } from '$lib/realtime';
import { setDiscordParticipants } from '$lib/stores/room.svelte';
import { session } from '$lib/stores/session.svelte';
import { DiscordSDK, type Types } from '@discord/embedded-app-sdk';

const CLIENT_ID = PUBLIC_DISCORD_CLIENT_ID;

export let discordSdk: DiscordSDK | null = null;

/**
 * Full Discord Activity boot: SDK handshake → OAuth authorize → exchange code for our JWT →
 * authenticate the SDK → seed participants → open the realtime connection.
 */
export async function initDiscord(): Promise<void> {
  try {
    discordSdk = new DiscordSDK(CLIENT_ID);
    await discordSdk.ready();

    const { code } = await discordSdk.commands.authorize({
      client_id: CLIENT_ID,
      response_type: 'code',
      state: '',
      prompt: 'none',
      // application_identities.write lets the bot push stats to the user's Discord profile widget.
      // The SDK's OAuthScopes union predates that scope (its RPC schema already knows it), hence the cast.
      scope: ['identify', 'application_identities.write' as Types.OAuthScopes],
    });

    const token = await api.exchangeToken(code);
    session.jwt = token.jwt;
    session.user = token.user;
    session.instanceId = discordSdk.instanceId;

    await discordSdk.commands.authenticate({ access_token: token.discordAccessToken });

    await refreshParticipants();
    discordSdk.subscribe('ACTIVITY_INSTANCE_PARTICIPANTS_UPDATE', onParticipantsUpdate);

    await connectRealtime();

    session.status = 'ready';
  } catch (e) {
    session.error = e instanceof Error ? e.message : String(e);
    session.status = 'error';
  }
}

async function refreshParticipants(): Promise<void> {
  if (!discordSdk) return;
  const res = await discordSdk.commands.getInstanceConnectedParticipants();
  setDiscordParticipants(res.participants ?? []);
}

function onParticipantsUpdate(data: { participants?: unknown[] }): void {
  setDiscordParticipants((data.participants ?? []) as never[]);
}
