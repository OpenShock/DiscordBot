<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { initDiscord } from '$lib/discord';
	import { session } from '$lib/stores/session.svelte';

	let { children } = $props();

	onMount(() => {
		void initDiscord();
	});

	const nav = [
		{ href: '/', label: 'Room' },
		{ href: '/settings', label: 'Settings' }
	];
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<title>OpenShock Activity</title>
</svelte:head>

<div class="min-h-screen flex flex-col">
	<header
		class="sticky top-0 z-20 border-b border-ink-700/70 bg-ink-950/80 backdrop-blur px-4 sm:px-6 py-3 flex items-center gap-4"
	>
		<div class="flex items-center gap-2 font-semibold tracking-tight">
			<span class="text-shock-500 text-xl leading-none">⚡</span>
			<span>OpenShock</span>
			<span class="text-mute font-normal hidden sm:inline">Activity</span>
		</div>

		<nav class="flex items-center gap-1 ml-2">
			{#each nav as item (item.href)}
				<a
					href={item.href}
					class="px-3 py-1.5 rounded-lg text-sm transition-colors {page.url.pathname === item.href
						? 'bg-ink-700 text-text'
						: 'text-mute hover:text-text hover:bg-ink-800'}"
				>
					{item.label}
				</a>
			{/each}
		</nav>

		<div class="ml-auto flex items-center gap-2 text-sm">
			{#if session.user}
				{#if session.user.avatar}
					<img src={session.user.avatar} alt="" class="w-7 h-7 rounded-full" />
				{:else}
					<div
						class="w-7 h-7 rounded-full bg-ink-600 grid place-items-center text-xs font-semibold"
					>
						{session.user.name.charAt(0).toUpperCase()}
					</div>
				{/if}
				<span class="hidden sm:inline text-mute">{session.user.name}</span>
			{/if}
		</div>
	</header>

	<main class="flex-1 px-4 sm:px-6 py-6 max-w-5xl w-full mx-auto">
		{#if session.status === 'connecting'}
			<div class="grid place-items-center py-24 text-center">
				<div class="animate-pulse text-shock-500 text-4xl mb-3">⚡</div>
				<p class="text-mute">Connecting to Discord…</p>
			</div>
		{:else if session.status === 'error'}
			<div class="max-w-md mx-auto mt-16 rounded-2xl border border-shock-600/40 bg-ink-850 p-6 text-center">
				<div class="text-3xl mb-2">⚠️</div>
				<h1 class="font-semibold mb-1">Could not start</h1>
				<p class="text-mute text-sm">{session.error}</p>
				<p class="text-mute/70 text-xs mt-4">
					This app must be launched as a Discord Activity from a voice channel.
				</p>
			</div>
		{:else}
			{@render children()}
		{/if}
	</main>
</div>
