import { DiscordSDK } from '@discord/embedded-app-sdk';
import { api } from '$lib/api';
import { session } from '$lib/stores/session.svelte';
import { setDiscordParticipants } from '$lib/stores/room.svelte';
import { connectRealtime } from '$lib/realtime';

const CLIENT_ID = import.meta.env.VITE_DISCORD_CLIENT_ID as string;

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
			scope: ['identify']
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
