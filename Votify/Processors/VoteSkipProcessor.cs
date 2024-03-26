using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteSkipProcessor : VoteProcessor<VoteSkip>
{
    public VoteSkipProcessor(ConfigurationBase configuration, VoteState voteState)
        : base(configuration, configuration.VoteSkipConfiguration, voteState)
    {
    }
}
