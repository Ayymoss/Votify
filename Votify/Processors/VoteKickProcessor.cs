using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteKickProcessor(ConfigurationBase configuration, VoteState voteState)
    : VoteProcessor<VoteKick>(configuration, configuration.VoteKickConfiguration, voteState);
