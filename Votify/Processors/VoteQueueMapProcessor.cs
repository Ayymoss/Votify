using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Interfaces;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteQueueMapProcessor : IVoteProcessor<VoteQueueMap>
{
    private readonly QueueState _queueState;
    public VoteType VoteType => VoteType.Map;

    public event IVoteProcessor<VoteQueueMap>.VoteSucceededEventHandler? VoteSucceeded;
    public event IVoteProcessor<VoteQueueMap>.VoteFailedEventHandler? VoteFailed;
    public event IVoteProcessor<VoteQueueMap>.VoteNotificationEventHandler? VoteNotification;
    public event IVoteProcessor<VoteQueueMap>.VoteCancelledEventHandler? VoteCancelled;

    public VoteQueueMapProcessor(QueueState queueState)
    {
        _queueState = queueState;
    }

    public void OnVoteStart(Server server, VoteQueueMap vote)
    {
        VoteNotification?.Invoke(server, vote);
    }

    public void OnVoteEnd(Server server, VoteQueueMap vote, VoteResult result)
    {
        switch (result)
        {
            case VoteResult.Passed:
                VoteSucceeded?.Invoke(server, vote);
                break;
            case VoteResult.Failed:
                VoteFailed?.Invoke(server, vote);
                break;
            case VoteResult.Cancelled:
                VoteCancelled?.Invoke(server, VoteCancellation.Other, string.Empty);
                break;
        }
    }

    public Task ExecuteAsync(Server server, VoteQueueMap vote)
    {
        _queueState.SetQueuedMap(server, vote.Map);
        return Task.CompletedTask;
    }
}
