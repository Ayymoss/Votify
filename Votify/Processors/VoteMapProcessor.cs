using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Services;

namespace Votify.Processors;

public class VoteMapProcessor(ConfigurationBase configuration, VoteState voteState)
    : VoteProcessor<VoteMap>(configuration, configuration.VoteMapConfiguration, voteState);
