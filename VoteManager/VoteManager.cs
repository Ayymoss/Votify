using SharedLibraryCore;
using SharedLibraryCore.Database.Models;

namespace VoteManager;

public class VoteManager
{
    private readonly ConfigurationModel _configuration;
    private readonly Dictionary<Server, VoteModel> _votes = new();
    private DateTime _lastBroadcastTime = DateTime.UtcNow;
    private SemaphoreSlim _onUpdateLock = new(1, 1);

    public VoteManager(ConfigurationModel configuration)
    {
        _configuration = configuration;
    }


    public async Task<VoteResult> CreateVote(Server server, EFClient origin, EFClient target, string reason,
        VoteType voteType, string? mapName = null)
    {
        if (_votes.ContainsKey(server)) return VoteResult.VoteInProgress;
        _votes.Add(server, new VoteModel
        {
            Origin = origin,
            Target = target,
            Reason = reason,
            VoteType = voteType,
            Creation = DateTime.UtcNow,
            MapName = mapName,
            Votes = new Dictionary<EFClient, Vote> {{origin, Vote.Yes}, {target, Vote.No}}
        });
        return VoteResult.Success;
    }

    public async Task<VoteResult> CastVote(Server server, EFClient origin, Vote vote)
    {
        if (!_votes.ContainsKey(server)) return VoteResult.NoVoteInProgress;
        if (!_votes[server].Votes!.ContainsKey(origin)) return VoteResult.AlreadyVoted;

        _votes[server].Votes!.Add(origin, vote);
        return VoteResult.Success;
    }

    // If the server pop drops below the threshold, the vote is cancelled
    public async Task OnUpdate()
    {
        try
        {
            await _onUpdateLock.WaitAsync();
            foreach (var server in _votes.Keys)
            {
                if (server.ClientNum < _configuration.MinimumPlayersRequired)
                {
                    _votes.Remove(server);
                    continue;
                }
                
                // Broadcast a currently running vote.
                if (_lastBroadcastTime.AddSeconds(_configuration.TimeBetweenVoteReminders) < DateTime.UtcNow)
                {
                    server.Broadcast(
                        _configuration.VoteMessages.OpenVoteAutoMessage.FormatExt(_votes[server].VoteType));
                    _lastBroadcastTime = DateTime.UtcNow;
                }

                // End expired votes.
                if (_votes[server].Creation.AddSeconds(_configuration.VoteDuration) < DateTime.UtcNow)
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
        if (_configuration.MinimumPlayersRequiredForSuccessfulVote > _votes[server].Votes!.Count)
        {
            server.Broadcast(_configuration.VoteMessages.NotEnoughVotes.FormatExt(_votes[server].VoteType,
                _votes[server].Target?.CleanedName));
            return;
        }

        var yesVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.Yes);
        var noVotes = _votes[server].Votes!.Count(vote => vote.Value == Vote.No);
        var votePercentage = (float)yesVotes / (yesVotes + noVotes);
        if (votePercentage < _configuration.PercentageVotePassed)
        {
            server.Broadcast(_configuration.VoteMessages.NotEnoughYesVotes.FormatExt(_votes[server].VoteType,
                _votes[server].Target?.CleanedName));
            return;
        }

        switch (_votes[server].VoteType)
        {
            case VoteType.Kick:
                await server.Kick($"VOTE by {_votes[server].Origin?.ClientId}: {_votes[server].Reason}",
                    _votes[server].Target, Utilities.IW4MAdminClient());
                server.Broadcast($"Vote to kick {_votes[server].Target?.CleanedName} passed");
                break;
            case VoteType.Map:
                await server.LoadMap(_votes[server].MapName);
                break;
            case VoteType.Ban:
                await server.TempBan($"VOTE by {_votes[server].Origin?.ClientId}: {_votes[server].Reason}",
                    TimeSpan.FromHours(1), _votes[server].Target, Utilities.IW4MAdminClient());
                server.Broadcast($"Vote to ban {_votes[server].Target?.CleanedName} passed");
                break;
            case VoteType.Skip:
                await server.ExecuteCommandAsync("map_rotate");
                break;
        }

        _votes.Remove(server);
    }

    public VoteResult CancelVote(Server server)
    {
        _votes.Remove(server);
        return VoteResult.Success;
    }

    public void HandleDisconnect(Server server, EFClient client)
    {
        if (!_votes.ContainsKey(server)) return;
        foreach (var vote in _votes.Values.Where(vote =>
                     vote.Origin?.ClientId == client.ClientId || vote.Target?.ClientId == client.ClientId))
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
    public string? MapName { get; set; }
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
    Failure,
    AlreadyVoted,
    NoVoteInProgress,
    NotEnoughVotes,
    InvalidTarget,
    InvalidOrigin,
    InvalidVoteType,
    InvalidReason,
    InvalidServer,
    VoteInProgress
}

public enum Vote
{
    Yes,
    No
}
