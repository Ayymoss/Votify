# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

```bash
dotnet build Votify.sln
```

Target: .NET 10.0, C# 13 with implicit usings and nullable reference types enabled.

Primary dependency: `RaidMax.IW4MAdmin.SharedLibraryCore` (2026.1.24.1-preview) — provides the plugin framework, EFClient, Server, GameEvent, commands, meta storage, and DI infrastructure.

No test project exists in this solution.

## What This Is

Votify is an IW4MAdmin plugin that adds democratic voting to game servers. Players can vote to kick, temporarily ban, change map, or skip the current map. Votes are time-limited, require configurable participation thresholds, and include anti-abuse protections (cooldowns, KDR restrictions, vote blocking).

## Architecture

**Plugin entry point:** `Plugin.cs` implements `IPluginV2`. Registers all services via DI, sets up webfront interactions for vote block/unblock, and listens for manager load and client disposal events.

**Generic vote pipeline:** The system uses generics to handle all vote types uniformly:
- `VoteProcessor<TVote>` (in `Services/`) — core voting logic: creation, registration, cancellation, validation, cooldown tracking, and notification scheduling. Emits events: `VoteSucceeded`, `VoteFailed`, `VoteNotification`, `VoteCancelled`.
- `VoteHandler<TVote>` (in `Handlers/`) — subscribes to processor events and executes the actual server actions (kick, ban, map change) and broadcasts results. Four concrete implementations: `VoteKickHandler`, `VoteBanHandler`, `VoteMapHandler`, `VoteSkipHandler`.

**State management:** `VoteState` (in `Services/`) holds active votes per server (`ConcurrentDictionary<Server, Tuple<VoteBase, IVoteProcessor>>`) and player cooldown tracking. Thread-safe throughout.

**Persistent storage:** `MetaManager` (in `Services/`) uses IW4MAdmin's `IMetaServiceV2` to persist vote-blocked users across sessions.

**Vote model hierarchy:** `VoteBase` is the base class with initiator, timestamp, and a `ConcurrentDictionary<EFClient, Vote>` of yes/no votes. `VoteKick`, `VoteBan`, `VoteMap`, `VoteSkip` extend it with type-specific fields (target, reason, map).

**Validation:** `Configuration/Validation.cs` uses FluentValidation to check cooldowns, minimum player counts, participation percentages, and vote pass thresholds.

**Configuration:** `ConfigurationBase` holds global settings (vote duration, reminder intervals, abusive voter thresholds, per-server disables, translations) plus per-type configs (`VoteKickConfiguration`, `VoteBanConfiguration`, `VoteMapConfiguration`, `VoteSkipConfiguration`) that each extend `VoteConfigurationBase` with type-specific options like KDR restrictions and ban duration.

## Commands

| Command | Alias | Min Permission |
|---------|-------|---------------|
| votekick | vk | User |
| voteban | vb | User |
| votemap | vm | User |
| voteskip | vs | User |
| yes | y | User |
| no | n | User |
| cancelvote | cv | Moderator |
| voteblock | vblock | SeniorAdmin |
| voteunblock | vunblock | SeniorAdmin |
