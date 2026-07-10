<script lang="ts">
  import { onMount } from 'svelte';
  import { api } from '$lib/api';
  import { session } from '$lib/stores/session.svelte';
  import { room, setConsent as setRoomConsent } from '$lib/stores/room.svelte';
  import type { MeResponse, ShockerDto, WhitelistEntry } from '$lib/types';
  import * as Tabs from '@openshock/svelte-core/components/ui/tabs';
  import * as Card from '@openshock/svelte-core/components/ui/card';
  import { Button } from '@openshock/svelte-core/components/ui/button';
  import { Input } from '@openshock/svelte-core/components/ui/input';
  import { Label } from '@openshock/svelte-core/components/ui/label';
  import { Switch } from '@openshock/svelte-core/components/ui/switch';
  import { Checkbox } from '@openshock/svelte-core/components/ui/checkbox';
  import { Slider } from '@openshock/svelte-core/components/ui/slider';
  import { Badge } from '@openshock/svelte-core/components/ui/badge';
  import { Separator } from '@openshock/svelte-core/components/ui/separator';
  import { TriangleAlert, Plus, X } from '@lucide/svelte';

  let tab = $state('account');

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

  {#if error}
    <div
      class="mb-4 flex items-center gap-2 rounded-lg border border-destructive/40 bg-destructive/10 text-shock-400 text-sm px-3 py-2"
    >
      <TriangleAlert class="size-4 shrink-0" />
      {error}
    </div>
  {/if}

  <Tabs.Root bind:value={tab}>
    <Tabs.List class="mb-6">
      <Tabs.Trigger value="account">Account</Tabs.Trigger>
      <Tabs.Trigger value="shockers">Shockers</Tabs.Trigger>
      <Tabs.Trigger value="whitelist">Whitelist</Tabs.Trigger>
      <Tabs.Trigger value="consent">Consent</Tabs.Trigger>
    </Tabs.List>

    <!-- Account -->
    <Tabs.Content value="account" class="space-y-4">
      <Card.Root class="p-3">
        <div class="text-sm">
          {#if me?.linked}
            <Badge variant="outline" class="border-green-500/40 text-green-500 gap-1">
              <span class="size-1.5 rounded-full bg-green-500"></span>Linked
            </Badge>
            {#if linkedName}<span class="text-muted-foreground ml-1">as {linkedName}</span>{/if}
          {:else}
            <span class="text-muted-foreground"
              >Not linked yet — enter your OpenShock API token below.</span
            >
          {/if}
        </div>
      </Card.Root>

      <div class="space-y-2">
        <Label for="api-token">API Token</Label>
        <Input
          id="api-token"
          type="password"
          bind:value={apiToken}
          placeholder="Your OpenShock API token"
        />
      </div>
      <div class="space-y-2">
        <Label for="api-server">API Server</Label>
        <Input id="api-server" type="url" bind:value={apiServer} />
      </div>

      <Button onclick={link} disabled={linking || !apiToken.trim()}>
        {linking ? 'Linking…' : me?.linked ? 'Re-link' : 'Link account'}
      </Button>
      <p class="text-xs text-muted-foreground">
        Your token is stored server-side and never exposed to other participants.
      </p>
    </Tabs.Content>

    <!-- Shockers -->
    <Tabs.Content value="shockers" class="space-y-4">
      {#if shockersLoading}
        <p class="text-muted-foreground text-sm">Loading shockers…</p>
      {:else if shockers.length === 0}
        <p class="text-muted-foreground text-sm">No shockers found. Link your account first.</p>
      {:else}
        {#each shockersByHub as [hub, list] (hub)}
          <Card.Root class="p-0 gap-0 overflow-hidden">
            <div
              class="px-3 py-2 text-xs uppercase tracking-wide text-muted-foreground bg-muted/50"
            >
              {hub}
            </div>
            {#each list as s (s.id)}
              <Label
                class="flex items-center gap-3 px-3 py-2.5 border-t border-border/60 cursor-pointer font-normal"
              >
                <Checkbox bind:checked={s.enabled} />
                <span>{s.name}</span>
              </Label>
            {/each}
          </Card.Root>
        {/each}
        <Button onclick={saveShockers} disabled={savingShockers}>
          {savingShockers ? 'Saving…' : 'Save shockers'}
        </Button>
      {/if}
    </Tabs.Content>

    <!-- Whitelist -->
    <Tabs.Content value="whitelist" class="space-y-5">
      <div>
        <h2 class="text-sm font-medium mb-2">Allowed to shock you</h2>
        {#if whitelist.length === 0}
          <p class="text-muted-foreground text-sm">No one yet.</p>
        {:else}
          <div class="space-y-2">
            {#each whitelist as w (w.discordId)}
              <Card.Root class="flex-row items-center justify-between px-3 py-2">
                <span>{nameFor(w.discordId)}</span>
                <Button
                  variant="ghost"
                  size="sm"
                  class="text-shock-400 hover:text-shock-500"
                  onclick={() => removeFriend(w.discordId)}
                >
                  <X class="size-3.5" />Remove
                </Button>
              </Card.Root>
            {/each}
          </div>
        {/if}
      </div>

      {#if addableParticipants.length > 0}
        <Separator />
        <div>
          <h2 class="text-sm font-medium mb-2">Add from this room</h2>
          <div class="flex flex-wrap gap-2">
            {#each addableParticipants as m (m.discordId)}
              <Button variant="outline" size="sm" onclick={() => addFriend(m.discordId)}>
                <Plus class="size-3.5" />{m.name}
              </Button>
            {/each}
          </div>
        </div>
      {/if}
    </Tabs.Content>

    <!-- Consent -->
    <Tabs.Content value="consent" class="space-y-5">
      <Label
        class="flex items-center gap-3 rounded-xl border border-border bg-card p-4 cursor-pointer font-normal"
      >
        <Switch bind:checked={allowRoom} />
        <span>
          <span class="font-medium">Allow room participants to shock me</span>
          <span class="block text-xs text-muted-foreground mt-0.5">
            Anyone in this Activity can shock you without being whitelisted — within the caps below.
          </span>
        </span>
      </Label>

      <Card.Root class="p-4 gap-4 {allowRoom ? '' : 'opacity-50 pointer-events-none'}">
        <div>
          <div class="flex justify-between text-sm text-muted-foreground mb-2">
            Max intensity <span class="tabular text-foreground">{maxIntensity}%</span>
          </div>
          <Slider type="single" bind:value={maxIntensity} min={1} max={100} step={1} />
        </div>
        <div>
          <div class="flex justify-between text-sm text-muted-foreground mb-2">
            Max duration
            <span class="tabular text-foreground">{(maxDurationMs / 1000).toFixed(1)}s</span>
          </div>
          <Slider type="single" bind:value={maxDurationMs} min={300} max={30000} step={100} />
        </div>
      </Card.Root>

      <Button onclick={saveConsent} disabled={savingConsent}>
        {savingConsent ? 'Saving…' : 'Save consent'}
      </Button>
    </Tabs.Content>
  </Tabs.Root>
</div>
