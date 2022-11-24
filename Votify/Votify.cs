using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Database.Models;

namespace Votify;

public class Votify
{
    private readonly ConcurrentDictionary<Server, VoteModel> _votes = new();
    private readonly Dictionary<Server, DateTimeOffset> _lastBroadcastTime = new();
    private readonly Dictionary<Server, DateTimeOffset> _lastCreatedVote = new();
    private readonly SemaphoreSlim _onUpdateLock = new(1, 1);

    public bool InProgressVote(Server server) => _votes.ContainsKey(server);

    public void CancelVote(Server server) => _votes.Remove(server, out _);

    public VoteResult CreateVote(Server server, VoteType voteType, EFClient origin, EFClient? target = null,
        string? reason = null, Map? map = null)
    {
        if (InProgressVote(server)) return VoteResult.VoteInProgress;
        if (_lastCreatedVote.ContainsKey(server) && DateTimeOffset.UtcNow - _lastCreatedVote[server] <
            TimeSpan.FromSeconds(Plugin.Configuration.VoteCooldown))
        {
            return VoteResult.VoteCooldown;
        }

        if (_lastCreatedVote.ContainsKey(server)) _lastCreatedVote.Remove(server);
        _lastCreatedVote.Add(server, DateTimeOffset.UtcNow);

        _votes.TryAdd(server, new VoteModel
        {
            Origin = origin,
            Target = target,
            Reason = reason,
            VoteType = voteType,
            Creation = DateTimeOffset.UtcNow,
            Map = map,
            Votes = target is not null
                ? new Dictionary<EFClient, Vote> {{origin, Vote.Yes}, {target, Vote.No}}
                : new Dictionary<EFClient, Vote> {{origin, Vote.Yes}},
            YesVotes = 1,
            NoVotes = target is not null ? (byte)1 : (byte)0
        });

        return VoteResult.Success;
    }

    public VoteResult CastVote(Server server, EFClient origin, Vote vote)
    {
        if (!InProgressVote(server)) return VoteResult.NoVoteInProgress;
        if (_votes[server].Votes!.ContainsKey(origin)) return VoteResult.AlreadyVoted;

        _votes[server].Votes!.Add(origin, vote);

        switch (vote)
        {
            case Vote.Yes:
                _votes[server].YesVotes++;
                break;
            case Vote.No:
                _votes[server].NoVotes++;
                break;
        }

        return VoteResult.Success;
    }

    public async Task OnUpdate()
    {
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys.ToList())
            {
                // Check if anyone has left and brought it below the player threshold
                if (Plugin.Configuration.MinimumPlayersRequired > server.ClientNum)
                {
                    server.Broadcast(Plugin.Configuration.Translations.VoteCancelledDueToPlayerDisconnect
                        .FormatExt(_votes[server].VoteType));
                    _votes.Remove(server, out _);
                    continue;
                }

                // Broadcast a currently running vote.
                if (_lastBroadcastTime.ContainsKey(server))
                {
                    if (DateTimeOffset.UtcNow - _lastBroadcastTime[server] >
                        TimeSpan.FromSeconds(Plugin.Configuration.TimeBetweenVoteReminders))
                    {
                        server.Broadcast(Plugin.Configuration.Translations.OpenVoteAutoMessage
                            .FormatExt(_votes[server].VoteType, _votes[server].YesVotes, _votes[server].NoVotes,
                                _votes[server].Target is not null
                                    ? _votes[server].Target?.CleanedName
                                    : _votes[server].VoteType is VoteType.Map
                                        ? _votes[server].Map?.Alias
                                        : VoteType.Skip));
                        _lastBroadcastTime[server] = DateTimeOffset.UtcNow;
                    }
                }
                else
                {
                    _lastBroadcastTime.Add(server, DateTimeOffset.UtcNow);
                }

                // End expired votes.
                if (DateTimeOffset.UtcNow - _votes[server].Creation >
                    TimeSpan.FromSeconds(Plugin.Configuration.VoteDuration))
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
        // If only few people vote, we shouldn't really action it if there's X than Y on the server. (16 people on server, 2 vote. Shouldn't action)
        if (Plugin.Configuration.MinimumPlayersRequiredForSuccessfulVote > _votes[server].Votes!.Count)
        {
            server.Broadcast(Plugin.Configuration.Translations.NotEnoughVotes.FormatExt(_votes[server].VoteType));
            _votes.Remove(server, out _);
            return;
        }

        // Check if the vote passed or failed.
        var yesVotes = _votes[server].YesVotes;
        var noVotes = _votes[server].NoVotes;
        var votePercentage = (float)yesVotes / (yesVotes + noVotes);
        if (Plugin.Configuration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(Plugin.Configuration.Translations.NotEnoughYesVotes
                .FormatExt(_votes[server].VoteType, yesVotes, noVotes,
                    _votes[server].Target is not null
                        ? _votes[server].Target?.CleanedName
                        : _votes[server].VoteType is VoteType.Map
                            ? _votes[server].Map?.Alias
                            : VoteType.Skip));
            _votes.Remove(server, out _);
            return;
        }

        // Vote passed, perform the action.
        switch (_votes[server].VoteType)
        {
            case VoteType.Kick:
                await server.Kick(Plugin.Configuration.Translations.VoteAction
                    .FormatExt(_votes[server].Origin?.CleanedName, _votes[server].Origin?.ClientId,
                        _votes[server].Reason), _votes[server].Target, Utilities.IW4MAdminClient());
                server.Broadcast(Plugin.Configuration.Translations.VotePassed
                    .FormatExt(_votes[server].VoteType, yesVotes, noVotes, _votes[server].Target?.CleanedName));
                break;
            case VoteType.Ban:
                await server.TempBan(Plugin.Configuration.Translations.VoteAction
                        .FormatExt(_votes[server].Origin?.CleanedName, _votes[server].Origin?.ClientId,
                            _votes[server].Reason), TimeSpan.FromHours(1), _votes[server].Target,
                    Utilities.IW4MAdminClient());
                server.Broadcast(Plugin.Configuration.Translations.VotePassed
                    .FormatExt(_votes[server].VoteType, yesVotes, noVotes, _votes[server].Target?.CleanedName));
                break;
            case VoteType.Skip:
                await server.ExecuteCommandAsync("map_rotate");
                break;
            case VoteType.Map:
                await server.LoadMap(_votes[server].Map?.Name);
                break;
        }

        _votes.Remove(server, out _);
    }

    public void HandleDisconnect(Server server, EFClient client)
    {
        if (!InProgressVote(server)) return;

        // If the player who disconnected was the target of the vote, cancel the vote.
        if (_votes[server].VoteType == VoteType.Kick && _votes[server].Target?.ClientId == client.ClientId)
        {
            server.Broadcast(Plugin.Configuration.Translations.VoteKickCancelledDueToTargetDisconnect);
            _votes.Remove(server, out _);
            return;
        }

        foreach (var vote in _votes.Values.Where(vote => vote.Origin?.ClientId == client.ClientId
                                                         || vote.Target?.ClientId == client.ClientId))
        {
            vote.Votes?.Remove(client);
        }
    }
}

public class VoteModel
{
    public EFClient? Origin { get; set; }
    public EFClient? Target { get; set; }
    public string? Reason { get; set; }
    public VoteType VoteType { get; set; }
    public DateTimeOffset Creation { get; set; }
    public Map? Map { get; set; }
    public Dictionary<EFClient, Vote>? Votes { get; set; }
    public byte YesVotes { get; set; }
    public byte NoVotes { get; set; }
}

public enum VoteType
{
    Kick,
    Map,
    Ban,
    Skip
}

public enum VoteResult
{
    Success,
    AlreadyVoted,
    NoVoteInProgress,
    VoteInProgress,
    VoteCooldown,
}

public enum Vote
{
    Yes,
    No
}
