<script lang="ts">
  import { api } from '$lib/api';
  import { session } from '$lib/stores/session.svelte';
  import { room, pushFeed, type RoomMember } from '$lib/stores/room.svelte';
  import type { ControlType, ShockMode } from '$lib/types';
  import { toast } from 'svelte-sonner';
  import { Button } from '@openshock/svelte-core/components/ui/button';
  import * as Card from '@openshock/svelte-core/components/ui/card';
  import * as ToggleGroup from '@openshock/svelte-core/components/ui/toggle-group';
  import * as Avatar from '@openshock/svelte-core/components/ui/avatar';
  import { Slider } from '@openshock/svelte-core/components/ui/slider';
  import { Badge } from '@openshock/svelte-core/components/ui/badge';
  import { Zap, Vibrate, Volume2 } from '@lucide/svelte';

  let intensity = $state(25);
  let duration = $state(1);
  let type = $state<ControlType>('Shock');
  let mode = $state<ShockMode>('Random');
  let busy = $state<string | null>(null);

  const me = $derived(session.user?.discordId ?? null);

  const members = $derived(
    Object.values(room.members).sort((a, b) => {
      if (a.present !== b.present) return a.present ? -1 : 1;
      return a.name.localeCompare(b.name);
    })
  );

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
        mode,
      });
      if (target.discordId === me) {
        pushFeed(`You ${type.toLowerCase()}ed yourself — ${intensity}% for ${duration}s`, true);
      }
      toast.success(`${type} sent to ${target.name}`);
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Failed');
    } finally {
      busy = null;
    }
  }

  const typeVerb: Record<ControlType, string> = {
    Shock: 'Shock',
    Vibrate: 'Vibrate',
    Sound: 'Beep',
    Stop: 'Stop',
  };
  const typeIcon = { Shock: Zap, Vibrate: Vibrate, Sound: Volume2 } as const;
</script>

<div class="grid lg:grid-cols-[1fr_20rem] gap-6">
  <div class="space-y-6">
    <!-- Control panel -->
    <Card.Root>
      <Card.Header class="flex-row items-center justify-between space-y-0">
        <Card.Title>Controls</Card.Title>
        <ToggleGroup.Root
          type="single"
          value={type}
          onValueChange={(v) => v && (type = v as ControlType)}
          variant="outline"
          size="sm"
        >
          {#each ['Shock', 'Vibrate', 'Sound'] as const as t (t)}
            {@const Icon = typeIcon[t]}
            <ToggleGroup.Item value={t} aria-label={t}>
              <Icon class="size-3.5" />
              {typeVerb[t]}
            </ToggleGroup.Item>
          {/each}
        </ToggleGroup.Root>
      </Card.Header>

      <Card.Content class="space-y-5">
        <div class="grid sm:grid-cols-2 gap-5">
          <div>
            <div class="flex justify-between text-sm text-muted-foreground mb-2">
              Intensity <span class="tabular text-foreground">{intensity}%</span>
            </div>
            <Slider type="single" bind:value={intensity} min={1} max={100} step={1} />
          </div>
          <div>
            <div class="flex justify-between text-sm text-muted-foreground mb-2">
              Duration <span class="tabular text-foreground">{duration.toFixed(1)}s</span>
            </div>
            <Slider type="single" bind:value={duration} min={0.3} max={30} step={0.1} />
          </div>
        </div>

        <div class="flex items-center gap-3 text-sm">
          <span class="text-muted-foreground">Spread</span>
          <ToggleGroup.Root
            type="single"
            value={mode}
            onValueChange={(v) => v && (mode = v as ShockMode)}
            variant="outline"
            size="sm"
          >
            {#each ['Random', 'All'] as const as m (m)}
              <ToggleGroup.Item value={m} aria-label={m}>{m}</ToggleGroup.Item>
            {/each}
          </ToggleGroup.Root>
        </div>
      </Card.Content>
    </Card.Root>

    <!-- Participants -->
    <section>
      <h2 class="font-semibold mb-3">
        In the room <span class="text-muted-foreground font-normal">({members.length})</span>
      </h2>
      {#if members.length === 0}
        <p class="text-muted-foreground text-sm">Waiting for participants…</p>
      {:else}
        <div class="grid sm:grid-cols-2 gap-3">
          {#each members as m (m.discordId)}
            {@const Icon = typeIcon[type as keyof typeof typeIcon] ?? Zap}
            <Card.Root class="p-3 flex-row items-center gap-3 {m.present ? '' : 'opacity-50'}">
              <Avatar.Root class="size-10">
                <Avatar.Image src={m.avatar} alt={m.name} />
                <Avatar.Fallback>{m.name.charAt(0).toUpperCase()}</Avatar.Fallback>
              </Avatar.Root>
              <div class="min-w-0 flex-1">
                <div class="truncate font-medium">
                  {m.name}
                  {#if m.discordId === me}<span class="text-muted-foreground text-xs">(you)</span
                    >{/if}
                </div>
                <div class="mt-1">
                  {#if m.allowRoomShocks}
                    <Badge variant="outline" class="border-green-500/40 text-green-500 gap-1">
                      <span class="size-1.5 rounded-full bg-green-500"></span>open to room
                    </Badge>
                  {:else}
                    <Badge variant="outline" class="text-muted-foreground gap-1">
                      <span class="size-1.5 rounded-full bg-muted-foreground"></span>whitelist only
                    </Badge>
                  {/if}
                </div>
              </div>
              <Button
                onclick={() => act(m)}
                disabled={busy === m.discordId || !session.instanceId}
                size="sm"
                class="shrink-0 glow-shock"
              >
                <Icon class="size-3.5" />
                {busy === m.discordId ? '…' : typeVerb[type]}
              </Button>
            </Card.Root>
          {/each}
        </div>
      {/if}
    </section>
  </div>

  <!-- Live feed -->
  <aside class="lg:sticky lg:top-20 h-max">
    <h2 class="font-semibold mb-3">Live feed</h2>
    <Card.Root class="p-3 gap-2 max-h-[70vh] overflow-y-auto">
      {#if room.feed.length === 0}
        <p class="text-muted-foreground text-sm px-1 py-2">Nothing yet. Zap someone ⚡</p>
      {:else}
        {#each room.feed as item (item.id)}
          <div
            class="text-sm rounded-lg px-3 py-2 {item.self
              ? 'bg-shock-600/15 border border-shock-600/40'
              : 'bg-muted'}"
          >
            {item.text}
          </div>
        {/each}
      {/if}
    </Card.Root>
  </aside>
</div>
