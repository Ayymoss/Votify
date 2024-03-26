using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteBanProcessor : VoteProcessor<VoteBan>
{
    public VoteBanProcessor(ConfigurationBase configuration, VoteState voteState)
        : base(configuration, configuration.VoteBanConfiguration, voteState)
    {
    }
}
