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

        UpdateLastCreatedVote(server);

        var votes = InitializeVotes(origin, target);
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

    private ConcurrentDictionary<EFClient, VoteEnums.Vote> InitializeVotes(EFClient origin, EFClient? target)
    {
        var votes = new ConcurrentDictionary<EFClient, VoteEnums.Vote> {[origin] = VoteEnums.Vote.Yes};
        if (target != null) votes[target] = VoteEnums.Vote.No;
        return votes;
    }

    private void UpdateLastCreatedVote(Server server)
    {
        if (_lastCreatedVote.ContainsKey(server)) _lastCreatedVote.TryRemove(server, out _);
        _lastCreatedVote.TryAdd(server, DateTimeOffset.UtcNow);
    }

    public VoteEnums.VoteResult CastVote(Server server, EFClient origin, VoteEnums.Vote vote)
    {
        if (!InProgressVote(server)) return VoteEnums.VoteResult.NoVoteInProgress;

        if (_votes[server].Votes!.ContainsKey(origin)) return VoteEnums.VoteResult.AlreadyVoted;

        _votes[server].Votes.TryAdd(origin, vote);
        UpdateVoteCount(server, vote);

        return VoteEnums.VoteResult.Success;
    }

    private void UpdateVoteCount(Server server, VoteEnums.Vote vote)
    {
        switch (vote)
        {
            case VoteEnums.Vote.Yes:
                _votes[server].YesVotes++;
                break;
            case VoteEnums.Vote.No:
                _votes[server].NoVotes++;
                break;
        }
    }

    public async Task OnNotify()
    {
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys.ToList())
            {
                var vote = _votes[server];

                if (_voteConfig.MinimumPlayersRequired > server.ClientNum)
                {
                    server.Broadcast(_voteConfig.Translations.VoteCancelledDueToPlayerDisconnect
                        .FormatExt(vote.VoteType));
                    _votes.TryRemove(server, out _);
                    continue;
                }

                BroadcastRunningVote(server, vote);
                UpdateBroadcastTime(server);

                if (DateTimeOffset.UtcNow - vote.Creation >
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

    private void BroadcastRunningVote(Server server, VoteModel vote)
    {
        var target = vote.Target?.CurrentAlias.Name ?? GetVoteTarget(vote);
        server.Broadcast(_voteConfig.Translations.OpenVoteAutoMessage
            .FormatExt(vote.VoteType, vote.YesVotes, vote.NoVotes, target));
    }

    private void UpdateBroadcastTime(Server server)
    {
        if (_lastBroadcastTime.ContainsKey(server))
        {
            _lastBroadcastTime[server] = DateTimeOffset.UtcNow;
        }
        else
        {
            _lastBroadcastTime.TryAdd(server, DateTimeOffset.UtcNow);
        }
    }

    private async Task EndVote(Server server)
    {
        var vote = _votes[server];
        var yesVotes = vote.YesVotes;
        var noVotes = vote.NoVotes;
        var totalVotes = yesVotes + noVotes;
        var playerVotePercentage = (float)totalVotes / server.ClientNum;

        if (_voteConfig.MinimumVotingPlayersPercentage > playerVotePercentage)
        {
            server.Broadcast(_voteConfig.Translations.NotEnoughVotes.FormatExt(vote.VoteType));
            _votes.TryRemove(server, out _);
            return;
        }

        var votePercentage = (float)yesVotes / totalVotes;
        if (_voteConfig.VotePassPercentage > votePercentage)
        {
            var target = vote.Target?.CleanedName ?? GetVoteTarget(vote);
            server.Broadcast(_voteConfig.Translations.NotEnoughYesVotes.FormatExt(vote.VoteType, yesVotes, noVotes, target));
            _votes.TryRemove(server, out _);
            return;
        }

        await PerformVoteAction(server, vote, yesVotes, noVotes);
        _votes.TryRemove(server, out _);
    }

    private string GetVoteTarget(VoteModel vote)
    {
        return vote.VoteType == VoteEnums.VoteType.Map ? vote.Map.Alias : VoteEnums.VoteType.Skip.ToString();
    }

    private async Task PerformVoteAction(Server server, VoteModel vote, int yesVotes, int noVotes)
    {
        var voteActionMessage =
            _voteConfig.Translations.VoteAction.FormatExt(vote.Origin?.CurrentAlias.Name, vote.Origin?.ClientId, vote.Reason);
        var votePassedMessage =
            _voteConfig.Translations.VotePassed.FormatExt(vote.VoteType, yesVotes, noVotes, vote.Target?.CurrentAlias.Name);

        switch (vote.VoteType)
        {
            case VoteEnums.VoteType.Kick:
                await server.Kick(voteActionMessage, vote.Target, Utilities.IW4MAdminClient());
                server.Broadcast(votePassedMessage);
                break;
            case VoteEnums.VoteType.Ban:
                await server.TempBan(voteActionMessage, _voteConfig.VoteBanDuration, vote.Target, Utilities.IW4MAdminClient());
                server.Broadcast(votePassedMessage);
                break;
            case VoteEnums.VoteType.Skip:
                await server.ExecuteCommandAsync("map_rotate");
                break;
            case VoteEnums.VoteType.Map:
                await server.LoadMap(vote.Map?.Name);
                break;
        }
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
            vote.Votes.TryRemove(client, out _);
        }
    }
}
