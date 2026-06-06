using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteMapHandler(VoteMapProcessor processor, ConfigurationBase configuration, ILogger<VoteMapHandler> logger)
    : VoteHandler<VoteMap>(processor, configuration, logger)
{
    protected override string VoteTypeName => Configuration.Translations.Map;
    protected override VoteConfigurationBase VoteTypeConfig => Configuration.VoteMapConfiguration;
    protected override string GetTargetDisplayName(VoteMap vote) => vote.Map.Alias;

    protected override async Task ExecuteVoteAction(Server server, VoteMap vote)
    {
        await Task.Delay(5_000);
        await server.LoadMap(vote.Map.Name);
    }
}
