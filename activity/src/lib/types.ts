// REST DTOs are generated from the OpenShock.Activity.Api OpenAPI document (see src/lib/api/generated).
// They're re-exported here under the names the app uses so components import from one place.
export type {
  AuthUserDto as AuthUser,
  ControlType,
  LinkResponse,
  MeResponse,
  ShockMode,
  ShockerDto,
  WhitelistEntryDto as WhitelistEntry,
} from '$lib/api/generated';

// SignalR event payloads (RoomHub) are not part of the REST API, so they stay hand-written here.
// All Discord IDs are strings (64-bit snowflakes exceed JS's safe-integer range).

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
