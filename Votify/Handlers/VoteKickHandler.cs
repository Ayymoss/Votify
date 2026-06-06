using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteKickHandler(VoteKickProcessor processor, ConfigurationBase configuration, ILogger<VoteKickHandler> logger)
    : VoteHandler<VoteKick>(processor, configuration, logger)
{
    protected override string VoteTypeName => Configuration.Translations.Kick;
    protected override VoteConfigurationBase VoteTypeConfig => Configuration.VoteKickConfiguration;
    protected override string GetTargetDisplayName(VoteKick vote) => vote.Target.CleanedName;

    protected override async Task ExecuteVoteAction(Server server, VoteKick vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        var voteActionMessage = Configuration.Translations.VoteAction
            .FormatExt(vote.Reason, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes);
        await server.Kick(voteActionMessage, vote.Target, vote.Initiator);
    }
}
