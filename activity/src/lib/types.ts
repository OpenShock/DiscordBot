// Shared shapes mirroring the OpenShock.Activity.Api DTOs. All Discord IDs are strings (they are
// 64-bit snowflakes that exceed JS's safe-integer range — the API serializes them as strings).

export type ControlType = 'Shock' | 'Vibrate' | 'Sound' | 'Stop';
export type ShockMode = 'Random' | 'All';

export interface AuthUser {
  discordId: string;
  name: string;
  avatar: string | null;
}

export interface MeResponse {
  linked: boolean;
  allowRoomShocks: boolean;
  roomMaxIntensity: number;
  roomMaxDurationMs: number;
}

export interface LinkResponse {
  openShockName: string;
}

export interface ShockerDto {
  id: string;
  name: string;
  hubName: string;
  enabled: boolean;
}

export interface WhitelistEntry {
  discordId: string;
}

export interface Participant {
  discordId: string;
  name: string;
  allowRoomShocks: boolean;
}

export interface ShockDeliveredEvent {
  fromDiscordId: string;
  fromName: string;
  toDiscordId: string;
  intensity: number;
  durationSeconds: number;
  type: string;
}

export interface ConsentChangedEvent {
  discordId: string;
  allowRoomShocks: boolean;
}

export interface ControlPayload {
  targetDiscordId: string;
  instanceId: string;
  intensity: number;
  duration: number;
  type: ControlType;
  mode: ShockMode;
}
