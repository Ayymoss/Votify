using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteKickProcessor : VoteProcessor<VoteKick>
{
    public VoteKickProcessor(ConfigurationBase configuration, VoteState voteState)
        : base(configuration, configuration.VoteKickConfiguration, voteState)
    {
    }
}
