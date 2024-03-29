using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteBanProcessor(ConfigurationBase configuration, VoteState voteState)
    : VoteProcessor<VoteBan>(configuration, configuration.VoteBanConfiguration, voteState);
