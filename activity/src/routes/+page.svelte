<script lang="ts">
	import { api } from '$lib/api';
	import { session } from '$lib/stores/session.svelte';
	import { room, pushFeed, type RoomMember } from '$lib/stores/room.svelte';
	import type { ControlType, ShockMode } from '$lib/types';

	let intensity = $state(25);
	let duration = $state(1);
	let type = $state<ControlType>('Shock');
	let mode = $state<ShockMode>('Random');
	let busy = $state<string | null>(null);
	let toast = $state<{ text: string; ok: boolean } | null>(null);

	const me = $derived(session.user?.discordId ?? null);

	const members = $derived(
		Object.values(room.members).sort((a, b) => {
			if (a.present !== b.present) return a.present ? -1 : 1;
			return a.name.localeCompare(b.name);
		})
	);

	let toastTimer: ReturnType<typeof setTimeout> | undefined;
	function showToast(text: string, ok: boolean) {
		toast = { text, ok };
		clearTimeout(toastTimer);
		toastTimer = setTimeout(() => (toast = null), 3500);
	}

	async function act(target: RoomMember) {
		if (!session.instanceId) return;
		busy = target.discordId;
		try {
			await api.control({
				targetDiscordId: target.discordId,
				instanceId: session.instanceId,
				intensity,
				duration,
				type,
				mode
			});
			if (target.discordId === me) {
				pushFeed(`You ${type.toLowerCase()}ed yourself — ${intensity}% for ${duration}s`, true);
			}
			showToast(`${type} sent to ${target.name}`, true);
		} catch (e) {
			showToast(e instanceof Error ? e.message : 'Failed', false);
		} finally {
			busy = null;
		}
	}

	const typeVerb: Record<ControlType, string> = {
		Shock: 'Shock',
		Vibrate: 'Vibrate',
		Sound: 'Beep',
		Stop: 'Stop'
	};
</script>

{#if toast}
	<div
		class="fixed bottom-5 left-1/2 -translate-x-1/2 z-30 px-4 py-2 rounded-xl text-sm shadow-lg {toast.ok
			? 'bg-good-500/15 border border-good-500/40 text-good-500'
			: 'bg-shock-600/15 border border-shock-600/50 text-shock-400'}"
	>
		{toast.text}
	</div>
{/if}

<div class="grid lg:grid-cols-[1fr_20rem] gap-6">
	<div class="space-y-6">
		<!-- Control panel -->
		<section class="rounded-2xl border border-ink-700 bg-ink-850/80 p-5">
			<div class="flex items-baseline justify-between mb-4">
				<h2 class="font-semibold">Controls</h2>
				<div class="inline-flex rounded-lg bg-ink-900 p-0.5">
					{#each ['Shock', 'Vibrate', 'Sound'] as const as t (t)}
						<button
							onclick={() => (type = t)}
							class="px-3 py-1 text-xs rounded-md transition-colors {type === t
								? 'bg-shock-500 text-white'
								: 'text-mute hover:text-text'}"
						>
							{typeVerb[t]}
						</button>
					{/each}
				</div>
			</div>

			<div class="grid sm:grid-cols-2 gap-5">
				<label class="block">
					<span class="flex justify-between text-sm text-mute mb-1">
						Intensity <span class="tabular text-text">{intensity}%</span>
					</span>
					<input type="range" min="1" max="100" bind:value={intensity} class="w-full accent-shock-500" />
				</label>
				<label class="block">
					<span class="flex justify-between text-sm text-mute mb-1">
						Duration <span class="tabular text-text">{duration.toFixed(1)}s</span>
					</span>
					<input
						type="range"
						min="0.3"
						max="30"
						step="0.1"
						bind:value={duration}
						class="w-full accent-shock-500"
					/>
				</label>
			</div>

			<div class="mt-4 flex items-center gap-2 text-sm">
				<span class="text-mute">Spread</span>
				<div class="inline-flex rounded-lg bg-ink-900 p-0.5">
					{#each ['Random', 'All'] as const as m (m)}
						<button
							onclick={() => (mode = m)}
							class="px-3 py-1 text-xs rounded-md transition-colors {mode === m
								? 'bg-ink-600 text-text'
								: 'text-mute hover:text-text'}"
						>
							{m}
						</button>
					{/each}
				</div>
			</div>
		</section>

		<!-- Participants -->
		<section>
			<h2 class="font-semibold mb-3">
				In the room <span class="text-mute font-normal">({members.length})</span>
			</h2>
			{#if members.length === 0}
				<p class="text-mute text-sm">Waiting for participants…</p>
			{:else}
				<div class="grid sm:grid-cols-2 gap-3">
					{#each members as m (m.discordId)}
						<div
							class="rounded-xl border border-ink-700 bg-ink-850 p-3 flex items-center gap-3 {m.present
								? ''
								: 'opacity-50'}"
						>
							{#if m.avatar}
								<img src={m.avatar} alt="" class="w-10 h-10 rounded-full" />
							{:else}
								<div class="w-10 h-10 rounded-full bg-ink-600 grid place-items-center font-semibold">
									{m.name.charAt(0).toUpperCase()}
								</div>
							{/if}
							<div class="min-w-0 flex-1">
								<div class="truncate font-medium">
									{m.name}
									{#if m.discordId === me}<span class="text-mute text-xs">(you)</span>{/if}
								</div>
								<div class="text-xs mt-0.5">
									{#if m.allowRoomShocks}
										<span class="text-good-500">● open to room</span>
									{:else}
										<span class="text-mute">● whitelist only</span>
									{/if}
								</div>
							</div>
							<button
								onclick={() => act(m)}
								disabled={busy === m.discordId || !session.instanceId}
								class="shrink-0 px-3 py-1.5 rounded-lg text-sm font-medium bg-shock-500 text-white hover:bg-shock-400 disabled:opacity-40 disabled:cursor-not-allowed glow-shock"
							>
								{busy === m.discordId ? '…' : typeVerb[type]}
							</button>
						</div>
					{/each}
				</div>
			{/if}
		</section>
	</div>

	<!-- Live feed -->
	<aside class="lg:sticky lg:top-20 h-max">
		<h2 class="font-semibold mb-3">Live feed</h2>
		<div
			class="rounded-2xl border border-ink-700 bg-ink-850/60 p-3 space-y-2 max-h-[70vh] overflow-y-auto"
		>
			{#if room.feed.length === 0}
				<p class="text-mute text-sm px-1 py-2">Nothing yet. Zap someone ⚡</p>
			{:else}
				{#each room.feed as item (item.id)}
					<div
						class="text-sm rounded-lg px-3 py-2 {item.self
							? 'bg-shock-600/15 border border-shock-600/40'
							: 'bg-ink-800'}"
					>
						{item.text}
					</div>
				{/each}
			{/if}
		</div>
	</aside>
</div>
