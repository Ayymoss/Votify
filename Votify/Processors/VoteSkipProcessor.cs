using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteSkipProcessor(ConfigurationBase configuration, VoteState voteState)
    : VoteProcessor<VoteSkip>(configuration, configuration.VoteSkipConfiguration, voteState);
