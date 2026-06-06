using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteSkipHandler(VoteSkipProcessor processor, ConfigurationBase configuration, ILogger<VoteSkipHandler> logger)
    : VoteHandler<VoteSkip>(processor, configuration, logger)
{
    protected override string VoteTypeName => Configuration.Translations.Skip;
    protected override VoteConfigurationBase VoteTypeConfig => Configuration.VoteSkipConfiguration;
    protected override string GetTargetDisplayName(VoteSkip vote) => Configuration.Translations.Skip;

    protected override void OnVoteNotification(Server server, VoteSkip vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        server.Broadcast(Configuration.Translations.OpenVoteAutoMessageNoTarget.FormatExt(VoteTypeName, vote.YesVotes,
            Math.Max(0, abstains), vote.NoVotes));
    }

    protected override async Task ExecuteVoteAction(Server server, VoteSkip vote)
    {
        await Task.Delay(5_000);
        await server.ExecuteCommandAsync("map_rotate");
    }
}
