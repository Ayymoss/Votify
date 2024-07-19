using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Configuration;
using Votify.Enums;
using Votify.Interfaces;
using Votify.Models;

namespace Votify.Services;

public abstract class VoteProcessor<TVote>(ConfigurationBase configuration, VoteConfigurationBase voteConfiguration, VoteState voteState)
    : IVoteProcessor where TVote : VoteBase
{
    private CancellationTokenSource? _cancellationTokenSource;

    public event Action<Server, TVote>? VoteFailed;
    public event Action<Server, TVote>? VoteSucceeded;
    public event Action<Server, TVote>? VoteNotification;
    public event Action<Server, VoteCancellation, string>? VoteCancelled;

    private DateTimeOffset _lastVote;

    public virtual VoteResult RegisterUserVote(UserVote userVote)
    {
        if (!voteState.Votes.TryGetValue(userVote.Server, out var voteBase)) return VoteResult.NoVoteInProgress;
        if (voteBase.Item1.Votes.TryAdd(userVote.Client, userVote.Vote)) return VoteResult.Success;
        return VoteResult.AlreadyVoted;
    }

    public virtual VoteResult CreateVote(Server server, TVote voteBase)
    {
        var abusiveVoter = IsUserAnAbusiveVoter(voteBase.Initiator);
        if (abusiveVoter) return VoteResult.AbusiveVoter;
        var result = voteConfiguration.Validate(_lastVote, server);
        if (!result.IsValid) return Enum.Parse<VoteResult>(result.ToString());

        if (!voteState.Votes.TryAdd(server, new Tuple<VoteBase, IVoteProcessor>(voteBase, this)))
            return VoteResult.VoteInProgress;

        _cancellationTokenSource = new CancellationTokenSource();
        Utilities.ExecuteAfterDelay(configuration.TimeBetweenVoteReminders, token => NotifyServer(server, token),
            _cancellationTokenSource.Token);
        Utilities.ExecuteAfterDelay(configuration.VoteDuration, token => EndVote(server, false, token),
            _cancellationTokenSource.Token);
        return VoteResult.Success;
    }

    private void ProcessVote(Server server, TVote voteBase)
    {
        var result = voteConfiguration.Validate(server, voteBase);

        HandleAbusiveVoter(voteBase);

        if (result.IsValid)
        {
            VoteSucceeded?.Invoke(server, voteBase);
        }
        else
        {
            VoteFailed?.Invoke(server, voteBase);
        }
    }

    private bool IsUserAnAbusiveVoter(EFClient client)
    {
        if (!voteState.UserCooldowns.TryGetValue(client, out var cooldownData)) return false;

        var nowUtc = TimeProvider.System.GetUtcNow();
        if (cooldownData.CooldownEnd.HasValue && cooldownData.CooldownEnd.Value > nowUtc) return true;

        cooldownData.LastVotes.RemoveAll(voteTime => voteTime < nowUtc.Subtract(configuration.AbusiveVoterWindow));
        var recentVotes = cooldownData.LastVotes.Count;

        return recentVotes >= configuration.AbusiveVoterThreshold;
    }

    private void HandleAbusiveVoter(TVote voteBase)
    {
        voteState.UserCooldowns.AddOrUpdate(voteBase.Initiator,
            new VoteState.CooldownData([TimeProvider.System.GetUtcNow()],
                TimeProvider.System.GetUtcNow().Add(configuration.AbusiveVoterCooldown)), (_, cooldownData) =>
            {
                cooldownData.LastVotes.Add(TimeProvider.System.GetUtcNow());
                return cooldownData;
            }
        );
    }

    private Task EndVote(Server server, bool endImmediately, CancellationToken token)
    {
        if (!voteState.Votes.TryRemove(server, out var voteBase)) return Task.CompletedTask;
        _lastVote = DateTimeOffset.UtcNow;

        if (!endImmediately) ProcessVote(server, (TVote)voteBase.Item1);

        _cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    private Task NotifyServer(Server server, CancellationToken token)
    {
        if (!voteState.Votes.TryGetValue(server, out var voteBase)) return Task.CompletedTask;
        if (configuration.VoteKickConfiguration.MinimumPlayersRequired > server.ConnectedClients.Count)
        {
            VoteCancelled?.Invoke(server, VoteCancellation.Disconnect, configuration.Translations.VoteCancelledDueToPlayerDisconnect);
            EndVote(server, true, CancellationToken.None);
            return Task.CompletedTask;
        }

        VoteNotification?.Invoke(server, (TVote)voteBase.Item1);
        Utilities.ExecuteAfterDelay(configuration.TimeBetweenVoteReminders, t => NotifyServer(server, t),
            _cancellationTokenSource?.Token ?? CancellationToken.None);
        return Task.CompletedTask;
    }

    public virtual void CancelVote(Server server)
    {
        VoteCancelled?.Invoke(server, VoteCancellation.Admin, configuration.Translations.VoteCancelled);
        EndVote(server, true, CancellationToken.None);
    }

    public void RemoveClient(EFClient client)
    {
        if (!voteState.Votes.TryGetValue(client.CurrentServer, out var voteBase)) return;
        voteBase.Item1.Votes.TryRemove(client, out _);
    }
}
