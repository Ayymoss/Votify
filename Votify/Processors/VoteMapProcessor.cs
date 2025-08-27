using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteMapProcessor : VoteProcessor<VoteMap>
{
    private readonly QueueState _queueState;

    public VoteMapProcessor(ConfigurationBase configuration, VoteState voteState, QueueState queueState) : base(
        configuration, configuration.VoteMapConfiguration, voteState)
    {
        _queueState = queueState;
    }

    public override VoteResult CreateVote(Server server, VoteMap voteBase)
    {
        _queueState.ClearQueuedMap(server);
        return base.CreateVote(server, voteBase);
    }
}
