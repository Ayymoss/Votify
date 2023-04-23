using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;

namespace Votify;

public class VoteManager
{
    private readonly VoteConfiguration _voteConfig;
    private readonly ConcurrentDictionary<Server, VoteModel> _votes = new();
    private readonly ConcurrentDictionary<Server, DateTimeOffset> _lastBroadcastTime = new();
    private readonly ConcurrentDictionary<Server, DateTimeOffset> _lastCreatedVote = new();
    private readonly SemaphoreSlim _onUpdateLock = new(1, 1);

    public VoteManager(VoteConfiguration voteConfig)
    {
        _voteConfig = voteConfig;
    }

    public bool InProgressVote(Server server) => _votes.ContainsKey(server);
    public void CancelVote(Server server) => _votes.Remove(server, out _);

    public VoteEnums.VoteResult CreateVote(Server server, VoteEnums.VoteType voteType, EFClient origin, EFClient? target = null,
        string? reason = null, Map? map = null)
    {
        if (InProgressVote(server)) return VoteEnums.VoteResult.VoteInProgress;
        if (_lastCreatedVote.ContainsKey(server) && DateTimeOffset.UtcNow - _lastCreatedVote[server] <
            TimeSpan.FromSeconds(_voteConfig.VoteCooldown))
        {
            return VoteEnums.VoteResult.VoteCooldown;
        }

        if (_lastCreatedVote.ContainsKey(server)) _lastCreatedVote.TryRemove(server, out _);
        _lastCreatedVote.TryAdd(server, DateTimeOffset.UtcNow);

        var votes = new ConcurrentDictionary<EFClient, VoteEnums.Vote>();
        if (target is not null)
        {
            votes.TryAdd(origin, VoteEnums.Vote.Yes);
            votes.TryAdd(target, VoteEnums.Vote.No);
        }
        else
        {
            votes.TryAdd(origin, VoteEnums.Vote.Yes);
        }

        _votes.TryAdd(server, new VoteModel
        {
            Origin = origin,
            Target = target,
            Reason = reason,
            VoteType = voteType,
            Creation = DateTimeOffset.UtcNow,
            Map = map,
            Votes = votes,
            YesVotes = 1,
            NoVotes = target is not null ? (byte)1 : (byte)0
        });

        return VoteEnums.VoteResult.Success;
    }

    public VoteEnums.VoteResult CastVote(Server server, EFClient origin, VoteEnums.Vote vote)
    {
        if (!InProgressVote(server)) return VoteEnums.VoteResult.NoVoteInProgress;
        if (_votes[server].Votes!.ContainsKey(origin)) return VoteEnums.VoteResult.AlreadyVoted;

        _votes[server].Votes!.TryAdd(origin, vote);

        switch (vote)
        {
            case VoteEnums.Vote.Yes:
                _votes[server].YesVotes++;
                break;
            case VoteEnums.Vote.No:
                _votes[server].NoVotes++;
                break;
        }

        return VoteEnums.VoteResult.Success;
    }

    public async Task OnNotify()
    {
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys.ToList())
            {
                // Check if anyone has left and brought it below the player threshold
                if (_voteConfig.MinimumPlayersRequired > server.ClientNum)
                {
                    server.Broadcast(_voteConfig.Translations.VoteCancelledDueToPlayerDisconnect
                        .FormatExt(_votes[server].VoteType));
                    _votes.TryRemove(server, out _);
                    continue;
                }

                // Broadcast a currently running vote.
                if (_lastBroadcastTime.ContainsKey(server))
                {
                    server.Broadcast(_voteConfig.Translations.OpenVoteAutoMessage
                        .FormatExt(_votes[server].VoteType, _votes[server].YesVotes, _votes[server].NoVotes,
                            _votes[server].Target is not null
                                ? _votes[server].Target?.CurrentAlias.Name
                                : _votes[server].VoteType is VoteEnums.VoteType.Map
                                    ? _votes[server].Map?.Alias
                                    : VoteEnums.VoteType.Skip));
                    _lastBroadcastTime[server] = DateTimeOffset.UtcNow;
                }
                else
                {
                    _lastBroadcastTime.TryAdd(server, DateTimeOffset.UtcNow);
                }

                // End expired votes.
                if (DateTimeOffset.UtcNow - _votes[server].Creation >
                    TimeSpan.FromSeconds(_voteConfig.VoteDuration))
                {
                    await EndVote(server);
                }
            }
        }
        finally
        {
            if (_onUpdateLock.CurrentCount == 0) _onUpdateLock.Release();
        }
    }

    private async Task EndVote(Server server)
    {
        // Cancel the vote if there's not enough votes.
        var yesVotes = _votes[server].YesVotes;
        var noVotes = _votes[server].NoVotes;
        var totalVotes = yesVotes + noVotes;
        var playerVotePercentage = (float)totalVotes / server.ClientNum;
        if (_voteConfig.MinimumVotingPlayersPercentage > playerVotePercentage)
        {
            server.Broadcast(_voteConfig.Translations.NotEnoughVotes.FormatExt(_votes[server].VoteType));
            _votes.TryRemove(server, out _);
            return;
        }

        // Check if the vote passed or failed.
        var votePercentage = (float)yesVotes / totalVotes;
        if (_voteConfig.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_voteConfig.Translations.NotEnoughYesVotes
                .FormatExt(_votes[server].VoteType, yesVotes, noVotes,
                    _votes[server].Target is not null
                        ? _votes[server].Target?.CleanedName
                        : _votes[server].VoteType is VoteEnums.VoteType.Map
                            ? _votes[server].Map?.Alias
                            : VoteEnums.VoteType.Skip));
            _votes.Remove(server, out _);
            return;
        }

        // Vote passed, perform the action.
        switch (_votes[server].VoteType)
        {
            case VoteEnums.VoteType.Kick:
                await server.Kick(_voteConfig.Translations.VoteAction
                    .FormatExt(_votes[server].Origin?.CurrentAlias.Name, _votes[server].Origin?.ClientId,
                        _votes[server].Reason), _votes[server].Target, Utilities.IW4MAdminClient());
                server.Broadcast(_voteConfig.Translations.VotePassed
                    .FormatExt(_votes[server].VoteType, yesVotes, noVotes, _votes[server].Target?.CurrentAlias.Name));
                break;
            case VoteEnums.VoteType.Ban:
                await server.TempBan(_voteConfig.Translations.VoteAction
                        .FormatExt(_votes[server].Origin?.CurrentAlias.Name, _votes[server].Origin?.ClientId,
                            _votes[server].Reason), _voteConfig.VoteBanDuration, _votes[server].Target,
                    Utilities.IW4MAdminClient());
                server.Broadcast(_voteConfig.Translations.VotePassed
                    .FormatExt(_votes[server].VoteType, yesVotes, noVotes, _votes[server].Target?.CurrentAlias.Name));
                break;
            case VoteEnums.VoteType.Skip:
                await server.ExecuteCommandAsync("map_rotate");
                break;
            case VoteEnums.VoteType.Map:
                await server.LoadMap(_votes[server].Map?.Name);
                break;
        }

        _votes.Remove(server, out _);
    }

    public void HandleDisconnect(Server server, EFClient client)
    {
        if (!InProgressVote(server)) return;

        // If the player who disconnected was the target of the vote, cancel the vote.
        if (_votes[server].VoteType == VoteEnums.VoteType.Kick && _votes[server].Target?.ClientId == client.ClientId)
        {
            server.Broadcast(_voteConfig.Translations.VoteKickCancelledDueToTargetDisconnect);
            _votes.TryRemove(server, out _);
            return;
        }

        foreach (var vote in _votes.Values.Where(vote =>
                     vote.Origin?.ClientId == client.ClientId || vote.Target?.ClientId == client.ClientId))
        {
            vote.Votes?.TryRemove(client, out _);
        }
    }
}
