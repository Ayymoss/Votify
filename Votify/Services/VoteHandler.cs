using SharedLibraryCore;
using Votify.Enums;
using Votify.Models;
using Votify.Models.VoteModel;

namespace Votify.Services;

public abstract class VoteHandler<TVote> where TVote : VoteBase
{
    protected abstract void OnVoteSucceeded(Server server, TVote vote);
    protected abstract void OnVoteFailed(Server server, TVote vote);
    protected abstract void OnVoteNotification(Server server, TVote vote);
    protected abstract void OnVoteCancellation(Server server, VoteCancellation reason, string message);
}
