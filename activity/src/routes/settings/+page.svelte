<script lang="ts">
	import { onMount } from 'svelte';
	import { api } from '$lib/api';
	import { session } from '$lib/stores/session.svelte';
	import { room, setConsent as setRoomConsent } from '$lib/stores/room.svelte';
	import type { MeResponse, ShockerDto, WhitelistEntry } from '$lib/types';

	type Tab = 'account' | 'shockers' | 'whitelist' | 'consent';
	let tab = $state<Tab>('account');
	const tabs: { id: Tab; label: string }[] = [
		{ id: 'account', label: 'Account' },
		{ id: 'shockers', label: 'Shockers' },
		{ id: 'whitelist', label: 'Whitelist' },
		{ id: 'consent', label: 'Consent' }
	];

	let me = $state<MeResponse | null>(null);
	let error = $state<string | null>(null);

	// --- Account ---
	let apiToken = $state('');
	let apiServer = $state('https://api.openshock.app');
	let linking = $state(false);
	let linkedName = $state<string | null>(null);

	// --- Shockers ---
	let shockers = $state<ShockerDto[]>([]);
	let shockersLoading = $state(false);
	let savingShockers = $state(false);

	// --- Whitelist ---
	let whitelist = $state<WhitelistEntry[]>([]);

	// --- Consent ---
	let allowRoom = $state(false);
	let maxIntensity = $state(30);
	let maxDurationMs = $state(3000);
	let savingConsent = $state(false);

	onMount(async () => {
		await refreshMe();
	});

	async function refreshMe() {
		try {
			me = await api.me();
			allowRoom = me.allowRoomShocks;
			maxIntensity = me.roomMaxIntensity;
			maxDurationMs = me.roomMaxDurationMs;
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		}
	}

	async function link() {
		linking = true;
		error = null;
		try {
			const res = await api.link(apiToken.trim(), apiServer.trim());
			linkedName = res.openShockName;
			apiToken = '';
			await refreshMe();
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			linking = false;
		}
	}

	async function loadShockers() {
		shockersLoading = true;
		error = null;
		try {
			shockers = await api.shockers();
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			shockersLoading = false;
		}
	}

	async function saveShockers() {
		savingShockers = true;
		try {
			await api.setShockers(shockers.filter((s) => s.enabled).map((s) => s.id));
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			savingShockers = false;
		}
	}

	async function loadWhitelist() {
		try {
			whitelist = await api.whitelist();
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		}
	}

	async function addFriend(id: string) {
		await api.addWhitelist(id);
		await loadWhitelist();
	}
	async function removeFriend(id: string) {
		await api.removeWhitelist(id);
		await loadWhitelist();
	}

	async function saveConsent() {
		savingConsent = true;
		try {
			me = await api.setConsent(allowRoom, maxIntensity, maxDurationMs);
			if (session.user) setRoomConsent(session.user.discordId, allowRoom);
		} catch (e) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			savingConsent = false;
		}
	}

	// Lazy-load per tab.
	$effect(() => {
		if (tab === 'shockers' && shockers.length === 0) void loadShockers();
		if (tab === 'whitelist') void loadWhitelist();
	});

	const whitelistIds = $derived(new Set(whitelist.map((w) => w.discordId)));
	const addableParticipants = $derived(
		Object.values(room.members).filter(
			(m) => m.discordId !== session.user?.discordId && !whitelistIds.has(m.discordId)
		)
	);
	const shockersByHub = $derived(
		Object.entries(
			shockers.reduce<Record<string, ShockerDto[]>>((acc, s) => {
				(acc[s.hubName] ??= []).push(s);
				return acc;
			}, {})
		)
	);
	const nameFor = (id: string) => room.members[id]?.name ?? id;
</script>

<div class="max-w-3xl mx-auto">
	<h1 class="text-lg font-semibold mb-4">Settings</h1>

	<div class="flex gap-1 mb-6 border-b border-ink-700">
		{#each tabs as t (t.id)}
			<button
				onclick={() => (tab = t.id)}
				class="px-4 py-2 text-sm -mb-px border-b-2 transition-colors {tab === t.id
					? 'border-shock-500 text-text'
					: 'border-transparent text-mute hover:text-text'}"
			>
				{t.label}
			</button>
		{/each}
	</div>

	{#if error}
		<div class="mb-4 rounded-lg border border-shock-600/40 bg-shock-600/10 text-shock-400 text-sm px-3 py-2">
			{error}
		</div>
	{/if}

	{#if tab === 'account'}
		<section class="space-y-4">
			<div class="rounded-xl border border-ink-700 bg-ink-850 p-3 text-sm">
				{#if me?.linked}
					<span class="text-good-500">● Linked</span>
					{#if linkedName}<span class="text-mute"> as {linkedName}</span>{/if}
				{:else}
					<span class="text-mute">● Not linked yet — enter your OpenShock API token below.</span>
				{/if}
			</div>

			<label class="block">
				<span class="text-sm text-mute">API Token</span>
				<input
					type="password"
					bind:value={apiToken}
					placeholder="Your OpenShock API token"
					class="mt-1 w-full rounded-lg bg-ink-900 border-ink-600 text-text placeholder:text-mute/60 focus:border-shock-500 focus:ring-shock-500"
				/>
			</label>
			<label class="block">
				<span class="text-sm text-mute">API Server</span>
				<input
					type="url"
					bind:value={apiServer}
					class="mt-1 w-full rounded-lg bg-ink-900 border-ink-600 text-text focus:border-shock-500 focus:ring-shock-500"
				/>
			</label>

			<button
				onclick={link}
				disabled={linking || !apiToken.trim()}
				class="px-4 py-2 rounded-lg bg-shock-500 text-white font-medium hover:bg-shock-400 disabled:opacity-40"
			>
				{linking ? 'Linking…' : me?.linked ? 'Re-link' : 'Link account'}
			</button>
			<p class="text-xs text-mute/70">
				Your token is stored server-side and never exposed to other participants.
			</p>
		</section>
	{:else if tab === 'shockers'}
		<section class="space-y-4">
			{#if shockersLoading}
				<p class="text-mute text-sm">Loading shockers…</p>
			{:else if shockers.length === 0}
				<p class="text-mute text-sm">No shockers found. Link your account first.</p>
			{:else}
				{#each shockersByHub as [hub, list] (hub)}
					<div class="rounded-xl border border-ink-700 bg-ink-850 overflow-hidden">
						<div class="px-3 py-2 text-xs uppercase tracking-wide text-mute bg-ink-900">{hub}</div>
						{#each list as s (s.id)}
							<label class="flex items-center gap-3 px-3 py-2.5 border-t border-ink-700/60 cursor-pointer">
								<input type="checkbox" bind:checked={s.enabled} class="rounded bg-ink-900 border-ink-500 text-shock-500 focus:ring-shock-500" />
								<span>{s.name}</span>
							</label>
						{/each}
					</div>
				{/each}
				<button
					onclick={saveShockers}
					disabled={savingShockers}
					class="px-4 py-2 rounded-lg bg-shock-500 text-white font-medium hover:bg-shock-400 disabled:opacity-40"
				>
					{savingShockers ? 'Saving…' : 'Save shockers'}
				</button>
			{/if}
		</section>
	{:else if tab === 'whitelist'}
		<section class="space-y-5">
			<div>
				<h2 class="text-sm font-medium mb-2">Allowed to shock you</h2>
				{#if whitelist.length === 0}
					<p class="text-mute text-sm">No one yet.</p>
				{:else}
					<div class="space-y-2">
						{#each whitelist as w (w.discordId)}
							<div class="flex items-center justify-between rounded-lg border border-ink-700 bg-ink-850 px-3 py-2">
								<span>{nameFor(w.discordId)}</span>
								<button onclick={() => removeFriend(w.discordId)} class="text-xs text-shock-400 hover:text-shock-500">
									Remove
								</button>
							</div>
						{/each}
					</div>
				{/if}
			</div>

			{#if addableParticipants.length > 0}
				<div>
					<h2 class="text-sm font-medium mb-2">Add from this room</h2>
					<div class="flex flex-wrap gap-2">
						{#each addableParticipants as m (m.discordId)}
							<button
								onclick={() => addFriend(m.discordId)}
								class="px-3 py-1.5 rounded-lg text-sm border border-ink-600 hover:border-shock-500 hover:text-shock-400"
							>
								+ {m.name}
							</button>
						{/each}
					</div>
				</div>
			{/if}
		</section>
	{:else if tab === 'consent'}
		<section class="space-y-5">
			<label class="flex items-center gap-3 rounded-xl border border-ink-700 bg-ink-850 p-4 cursor-pointer">
				<input type="checkbox" bind:checked={allowRoom} class="rounded bg-ink-900 border-ink-500 text-shock-500 focus:ring-shock-500" />
				<span>
					<span class="font-medium">Allow room participants to shock me</span>
					<span class="block text-xs text-mute mt-0.5">
						Anyone in this Activity can shock you without being whitelisted — within the caps below.
					</span>
				</span>
			</label>

			<div class="rounded-xl border border-ink-700 bg-ink-850 p-4 space-y-4 {allowRoom ? '' : 'opacity-50 pointer-events-none'}">
				<label class="block">
					<span class="flex justify-between text-sm text-mute mb-1">
						Max intensity <span class="tabular text-text">{maxIntensity}%</span>
					</span>
					<input type="range" min="1" max="100" bind:value={maxIntensity} class="w-full accent-shock-500" />
				</label>
				<label class="block">
					<span class="flex justify-between text-sm text-mute mb-1">
						Max duration <span class="tabular text-text">{(maxDurationMs / 1000).toFixed(1)}s</span>
					</span>
					<input type="range" min="300" max="30000" step="100" bind:value={maxDurationMs} class="w-full accent-shock-500" />
				</label>
			</div>

			<button
				onclick={saveConsent}
				disabled={savingConsent}
				class="px-4 py-2 rounded-lg bg-shock-500 text-white font-medium hover:bg-shock-400 disabled:opacity-40"
			>
				{savingConsent ? 'Saving…' : 'Save consent'}
			</button>
		</section>
	{/if}
</div>
