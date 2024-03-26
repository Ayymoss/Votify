using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Configuration;
using Votify.Enums;
using Votify.Interfaces;
using Votify.Models;

namespace Votify.Services;

public abstract class VoteProcessor<TVote> : IVoteProcessor where TVote : VoteBase
{
    private readonly ConfigurationBase _configuration;
    private readonly VoteConfigurationBase _voteConfiguration;
    private readonly VoteState _voteState;
    private CancellationTokenSource? _cancellationTokenSource;

    public event Action<Server, TVote>? VoteFailed;
    public event Action<Server, TVote>? VoteSucceeded;
    public event Action<Server, TVote>? VoteNotification;
    public event Action<Server, VoteCancellation, string>? VoteCancelled;

    private DateTimeOffset _lastVote;

    protected VoteProcessor(ConfigurationBase configuration, VoteConfigurationBase voteConfiguration, VoteState voteState)
    {
        _configuration = configuration;
        _voteConfiguration = voteConfiguration;
        _voteState = voteState;
    }

    public virtual VoteResult RegisterUserVote(UserVote userVote)
    {
        if (!_voteState.Votes.TryGetValue(userVote.Server, out var voteBase)) return VoteResult.NoVoteInProgress;
        if (voteBase.Item1.Votes.TryAdd(userVote.Client, userVote.Vote)) return VoteResult.Success;
        return VoteResult.AlreadyVoted;
    }

    public virtual VoteResult CreateVote(Server server, TVote voteBase)
    {
        var result = _voteConfiguration.Validate(_lastVote);
        if (!result.IsValid) return VoteResult.VoteCooldown;

        if (!_voteState.Votes.TryAdd(server, new Tuple<VoteBase, IVoteProcessor>(voteBase, this)))
            return VoteResult.VoteInProgress;

        _cancellationTokenSource = new CancellationTokenSource();
        Utilities.ExecuteAfterDelay(_configuration.TimeBetweenVoteReminders, token => NotifyServer(server, token),
            _cancellationTokenSource!.Token);
        Utilities.ExecuteAfterDelay(_configuration.VoteDuration, token => EndVote(server, false, token),
            _cancellationTokenSource!.Token);
        return VoteResult.Success;
    }

    private void ProcessVote(Server server, TVote voteBase)
    {
        var result = _voteConfiguration.Validate(server, voteBase);

        if (result.IsValid)
        {
            VoteSucceeded?.Invoke(server, voteBase);
        }
        else
        {
            VoteFailed?.Invoke(server, voteBase);
        }
    }

    private Task EndVote(Server server, bool endImmediately, CancellationToken token)
    {
        if (!_voteState.Votes.TryRemove(server, out var voteBase)) return Task.CompletedTask;
        _lastVote = DateTimeOffset.UtcNow;

        if (!endImmediately) ProcessVote(server, (TVote)voteBase.Item1);

        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    private Task NotifyServer(Server server, CancellationToken token)
    {
        if (!_voteState.Votes.TryGetValue(server, out var voteBase)) return Task.CompletedTask;
        if (_configuration.VoteKickConfiguration.MinimumPlayersRequired > server.ConnectedClients.Count)
        {
            VoteCancelled?.Invoke(server, VoteCancellation.Disconnect, _configuration.Translations.VoteCancelledDueToPlayerDisconnect);
            EndVote(server, true, CancellationToken.None);
            return Task.CompletedTask;
        }

        VoteNotification?.Invoke(server, (TVote)voteBase.Item1);
        Utilities.ExecuteAfterDelay(_configuration.TimeBetweenVoteReminders, t => NotifyServer(server, t),
            _cancellationTokenSource!.Token);
        return Task.CompletedTask;
    }

    public virtual void CancelVote(Server server)
    {
        VoteCancelled?.Invoke(server, VoteCancellation.Admin, _configuration.Translations.VoteCancelled);
        EndVote(server, true, CancellationToken.None);
    }

    public void RemoveClient(EFClient client)
    {
        if (!_voteState.Votes.TryGetValue(client.CurrentServer, out var voteBase)) return;
        voteBase.Item1.Votes.TryRemove(client, out _);
    }
}
