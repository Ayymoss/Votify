using SharedLibraryCore;
using SharedLibraryCore.Database.Models;

namespace VoteManager;

public class VoteManager
{
    private readonly Dictionary<Server, VoteModel> _votes = new();
    private readonly Dictionary<Server, DateTime> _lastBroadcastTime = new();
    private readonly Dictionary<Server, DateTime> _lastCreatedVote = new();
    private readonly SemaphoreSlim _onUpdateLock = new(1, 1);

    public bool InProgressVote(Server server) => _votes.ContainsKey(server);

    public void CancelVote(Server server) => _votes.Remove(server);

    public VoteResult CreateVote(Server server, VoteType voteType, EFClient origin, EFClient? target = null,
        string? reason = null, Map? mapName = null)
    {
        if (InProgressVote(server)) return VoteResult.VoteInProgress;
        if (_lastCreatedVote.ContainsKey(server) &&
            _lastCreatedVote[server].AddSeconds(Plugin.Configuration.VoteCooldown) > DateTime.UtcNow)
        {
            return VoteResult.VoteCooldown;
        }

        if (_lastCreatedVote.ContainsKey(server)) _lastCreatedVote.Remove(server);
        _lastCreatedVote.Add(server, DateTime.UtcNow);

        _votes.Add(server, new VoteModel
        {
            Origin = origin,
            Target = target,
            Reason = reason,
            VoteType = voteType,
            Creation = DateTime.UtcNow,
            Map = mapName,
            Votes = target is not null
                ? new Dictionary<EFClient, Vote> {{origin, Vote.Yes}, {target, Vote.No}}
                : new Dictionary<EFClient, Vote> {{origin, Vote.Yes}}
        });

        return VoteResult.Success;
    }

    public VoteResult CastVote(Server server, EFClient origin, Vote vote)
    {
        if (!InProgressVote(server)) return VoteResult.NoVoteInProgress;
        if (_votes[server].Votes!.ContainsKey(origin)) return VoteResult.AlreadyVoted;

        _votes[server].Votes!.Add(origin, vote);
        return VoteResult.Success;
    }

    public async Task OnUpdate()
    {
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys)
            {
                // Check if anyone has left and brought it below the player threshold
                if (server.ClientNum < Plugin.Configuration.MinimumPlayersRequired)
                {
                    server.Broadcast(Plugin.Configuration.Translations.VoteCancelledDueToPlayerDisconnect
                        .FormatExt(_votes[server].VoteType));
                    _votes.Remove(server);
                    continue;
                }

                // Broadcast a currently running vote.
                if (_lastBroadcastTime.ContainsKey(server))
                {
                    if (_lastBroadcastTime[server].AddSeconds(Plugin.Configuration.TimeBetweenVoteReminders) <
                        DateTime.UtcNow)
                    {
                        var yesVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.Yes);
                        var noVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.No);
                        server.Broadcast(Plugin.Configuration.Translations.OpenVoteAutoMessage
                            .FormatExt(_votes[server].VoteType, yesVotes, noVotes,
                                _votes[server].Target is not null
                                    ? _votes[server].Target?.CleanedName
                                    : _votes[server].VoteType is VoteType.Map
                                        ? _votes[server].Map?.Alias
                                        : VoteType.Skip));
                        _lastBroadcastTime[server] = DateTime.UtcNow;
                    }
                }
                else
                {
                    _lastBroadcastTime.Add(server, DateTime.UtcNow);
                }

                // End expired votes.
                if (_votes[server].Creation.AddSeconds(Plugin.Configuration.VoteDuration) < DateTime.UtcNow)
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
        // If only few people vote, we shouldn't really action it if there's more than more on the server.
        if (Plugin.Configuration.MinimumPlayersRequiredForSuccessfulVote > _votes[server].Votes!.Count)
        {
            server.Broadcast(Plugin.Configuration.Translations.NotEnoughVotes.FormatExt(_votes[server].VoteType));
            _votes.Remove(server);
            return;
        }

        var yesVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.Yes);
        var noVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.No);
        var votePercentage = (float)yesVotes / (yesVotes + noVotes);
        if (votePercentage < Plugin.Configuration.VotePassPercentage)
        {
            server.Broadcast(Plugin.Configuration.Translations.NotEnoughYesVotes
                .FormatExt(_votes[server].VoteType, yesVotes, noVotes,
                    _votes[server].Target is not null
                        ? _votes[server].Target?.CleanedName
                        : _votes[server].VoteType is VoteType.Map
                            ? _votes[server].Map?.Alias
                            : VoteType.Skip));
            _votes.Remove(server);
            return;
        }

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

        _votes.Remove(server);
    }

    public void HandleDisconnect(Server server, EFClient client)
    {
        if (!InProgressVote(server)) return;
        
        if (_votes[server].VoteType == VoteType.Kick && _votes[server].Target?.ClientId == client.ClientId)
        {
            server.Broadcast(Plugin.Configuration.Translations.VoteKickCancelledDueToTargetDisconnect);
            _votes.Remove(server);
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
    public DateTime Creation { get; set; }
    public Map? Map { get; set; }
    public Dictionary<EFClient, Vote>? Votes { get; set; }
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
