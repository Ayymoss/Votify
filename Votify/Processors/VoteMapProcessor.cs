using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteMapProcessor : VoteProcessor<VoteMap>
{
    public VoteMapProcessor(ConfigurationBase configuration, VoteState voteState)
        : base(configuration, configuration.VoteMapConfiguration, voteState)
    {
    }
}
