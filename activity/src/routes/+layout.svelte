<script lang="ts">
  import '../app.css';
  import logo from '$lib/assets/NavbarLogoSpin.svg';
  import { page } from '$app/state';
  import { resolve } from '$app/paths';
  import { onMount } from 'svelte';
  import { initDiscord } from '$lib/discord';
  import { session } from '$lib/stores/session.svelte';
  import { Button } from '@openshock/svelte-core/components/ui/button';
  import * as Avatar from '@openshock/svelte-core/components/ui/avatar';
  import { Toaster } from '@openshock/svelte-core/components/ui/sonner';
  import { Zap, TriangleAlert } from '@lucide/svelte';

  let { children } = $props();

  onMount(() => {
    void initDiscord();
  });

  const nav = [
    { href: '/', label: 'Room' },
    { href: '/settings', label: 'Settings' },
  ];
</script>

<svelte:head>
  <title>OpenShock Activity</title>
</svelte:head>

<Toaster theme="dark" position="bottom-center" richColors />

<div class="min-h-screen flex flex-col">
  <header
    class="sticky top-0 z-20 border-b border-border/70 bg-ink-950/80 backdrop-blur px-4 sm:px-6 py-3 flex items-center gap-4"
  >
    <a href={resolve('/')} class="flex items-center gap-2">
      <img src={logo} alt="OpenShock" class="h-6 w-auto" />
      <span class="text-muted-foreground text-sm hidden sm:inline">Activity</span>
    </a>

    <nav class="flex items-center gap-1 ml-2">
      {#each nav as item (item.href)}
        <Button
          href={item.href}
          variant={page.url.pathname === item.href ? 'secondary' : 'ghost'}
          size="sm"
        >
          {item.label}
        </Button>
      {/each}
    </nav>

    <div class="ml-auto flex items-center gap-2 text-sm">
      {#if session.user}
        <Avatar.Root class="size-7">
          <Avatar.Image src={session.user.avatar} alt={session.user.name} />
          <Avatar.Fallback>{session.user.name.charAt(0).toUpperCase()}</Avatar.Fallback>
        </Avatar.Root>
        <span class="hidden sm:inline text-muted-foreground">{session.user.name}</span>
      {/if}
    </div>
  </header>

  <main class="flex-1 px-4 sm:px-6 py-6 max-w-5xl w-full mx-auto">
    {#if session.status === 'connecting'}
      <div class="grid place-items-center py-24 text-center">
        <Zap class="size-10 text-shock-500 mb-3 animate-pulse" />
        <p class="text-muted-foreground">Connecting to Discord…</p>
      </div>
    {:else if session.status === 'error'}
      <div
        class="max-w-md mx-auto mt-16 rounded-2xl border border-destructive/40 bg-card p-6 text-center"
      >
        <TriangleAlert class="size-8 mx-auto mb-2 text-shock-400" />
        <h1 class="font-semibold mb-1">Could not start</h1>
        <p class="text-muted-foreground text-sm">{session.error}</p>
        <p class="text-muted-foreground/70 text-xs mt-4">
          This app must be launched as a Discord Activity from a voice channel.
        </p>
      </div>
    {:else}
      {@render children()}
    {/if}
  </main>
</div>
