using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Database.Models;

namespace Votify;

public class VoteManager
{
    private readonly VoteConfiguration _voteConfig;
    private readonly ConcurrentDictionary<Server, VoteModel> _votes = new();
    private readonly ConcurrentDictionary<Server, DateTimeOffset> _lastCreatedVote = new();
    private readonly SemaphoreSlim _onUpdateLock = new(1, 1);

    public VoteManager(VoteConfiguration voteConfig)
    {
        _voteConfig = voteConfig;
    }

    public bool InProgressVote(Server server) => _votes.ContainsKey(server);
    public void CancelVote(Server server) => _votes.Remove(server, out _);

    public VoteEnums.VoteResult CreateVote(Server server, VoteEnums.VoteType voteType, EFClient origin,
        EFClient? target = null,
        string? reason = null, Map? map = null)
    {
        if (InProgressVote(server)) return VoteEnums.VoteResult.VoteInProgress;

        var voteOnCooldown = _lastCreatedVote.ContainsKey(server) &&
                             DateTimeOffset.UtcNow - _lastCreatedVote[server] < _voteConfig.VoteCooldown;
        if (voteOnCooldown) return VoteEnums.VoteResult.VoteCooldown;

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
        if (!_lastCreatedVote.ContainsKey(server)) _lastCreatedVote.TryAdd(server, DateTimeOffset.UtcNow);
        _lastCreatedVote[server] = DateTimeOffset.UtcNow;
    }

    public VoteEnums.VoteResult CastVote(Server server, EFClient origin, VoteEnums.Vote vote)
    {
        if (!InProgressVote(server)) return VoteEnums.VoteResult.NoVoteInProgress;

        if (_votes[server].Votes.ContainsKey(origin)) return VoteEnums.VoteResult.AlreadyVoted;

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
        if (!_votes.Any()) return;
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys.ToList())
            {
                var vote = _votes[server];

                if (_voteConfig.MinimumPlayersRequired > server.ConnectedClients.Count)
                {
                    server.Broadcast(_voteConfig.Translations.VoteCancelledDueToPlayerDisconnect
                        .FormatExt(vote.VoteType));
                    _votes.TryRemove(server, out _);
                    continue;
                }

                var abstain = server.ConnectedClients.Count - vote.YesVotes - vote.NoVotes;
                BroadcastRunningVote(server, vote, abstain);
                var voteExpired = DateTimeOffset.UtcNow - vote.Creation > _voteConfig.VoteDuration;
                if (voteExpired) await EndVote(server, vote, abstain);
            }
        }
        finally
        {
            if (_onUpdateLock.CurrentCount == 0) _onUpdateLock.Release();
        }
    }

    private void BroadcastRunningVote(Server server, VoteModel vote, int abstain)
    {
        var target = vote.Target?.CurrentAlias.Name ?? GetVoteTarget(vote);
        server.Broadcast(_voteConfig.Translations.OpenVoteAutoMessage.FormatExt(vote.VoteType, vote.YesVotes,
            abstain, vote.NoVotes, target));
    }

    private async Task EndVote(Server server, VoteModel vote, int abstain)
    {
        var yesVotes = vote.YesVotes;
        var noVotes = vote.NoVotes;
        var totalVotes = yesVotes + noVotes;
        var playerVotePercentage = (float)totalVotes / server.ConnectedClients.Count;

        if (_voteConfig.MinimumVotingPlayersPercentage > playerVotePercentage)
        {
            server.Broadcast(_voteConfig.Translations.NotEnoughVotes.FormatExt(vote.VoteType));
            _votes.TryRemove(server, out _);
            return;
        }

        var votePercentage = (float)yesVotes / totalVotes;
        if (_voteConfig.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_voteConfig.Translations.NotEnoughYesVotes.FormatExt(vote.VoteType));
            _votes.TryRemove(server, out _);
            return;
        }

        await PerformVoteAction(server, vote, yesVotes, noVotes, abstain);
        _votes.TryRemove(server, out _);
    }

    private string GetVoteTarget(VoteModel vote) => vote.VoteType == VoteEnums.VoteType.Map
        ? vote.Map!.Alias
        : VoteEnums.VoteType.Skip.ToString();

    private async Task PerformVoteAction(Server server, VoteModel vote, int yesVotes, int noVotes, int abstain)
    {
        var voteActionMessage = _voteConfig.Translations.VoteAction
            .FormatExt(vote.Origin?.CleanedName, vote.Origin?.ClientId, vote.Reason);
        var target = vote.VoteType is VoteEnums.VoteType.Kick or VoteEnums.VoteType.Ban
            ? vote.Target?.CleanedName
            : vote.VoteType.ToString();
        var votePassedMessage = _voteConfig.Translations.VotePassed
            .FormatExt(vote.VoteType, yesVotes, abstain, noVotes, target);

        switch (vote.VoteType)
        {
            case VoteEnums.VoteType.Kick:
                await server.Kick(voteActionMessage, vote.Target, Utilities.IW4MAdminClient());
                server.Broadcast(votePassedMessage);
                break;
            case VoteEnums.VoteType.Ban:
                await server.TempBan(voteActionMessage, _voteConfig.VoteBanDuration, vote.Target,
                    Utilities.IW4MAdminClient());
                server.Broadcast(votePassedMessage);
                break;
            case VoteEnums.VoteType.Skip:
                server.Broadcast(votePassedMessage);
                await Task.Delay(5_000);
                await server.ExecuteCommandAsync("map_rotate");
                break;
            case VoteEnums.VoteType.Map:
                server.Broadcast(votePassedMessage);
                await Task.Delay(5_000);
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

        var filteredVotes = _votes.Values
            .Where(vote => vote.Origin?.ClientId == client.ClientId || vote.Target?.ClientId == client.ClientId);
        foreach (var vote in filteredVotes) vote.Votes.TryRemove(client, out _);
    }
}
