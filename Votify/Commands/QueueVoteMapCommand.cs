using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Models.VoteModel;
using Votify.Services;
using Votify.Configuration;

namespace Votify.Commands;

public class QueueVoteMapCommand : Command
{
    private readonly VoteQueueMapProcessor _voteProcessor;
    private readonly ITranslationLookup _translationLookup;
    private readonly ConfigurationBase _votifyConfig;

    public QueueVoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        VoteQueueMapProcessor voteProcessor, ConfigurationBase votifyConfig) : base(config,
        translationLookup)
    {
        _voteProcessor = voteProcessor;
        _translationLookup = translationLookup;
        _votifyConfig = votifyConfig;
        Name = "queuevotemap";
        Alias = "qvm";
        Description = "starts a vote to queue a map for the next round";
        Permission = Data.Models.Client.EFClient.Permission.User;
        RequiresTarget = false;
        Arguments =
        [
            new CommandArgument
            {
                Name = "map",
                Required = true
            }
        ];
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        var mapName = gameEvent.Data;
        var (map, success) = await gameEvent.Owner.GetMap(mapName);

        if (!success)
        {
            gameEvent.Origin.Tell(_votifyConfig.Translations.MapNotFound);
            return;
        }

        var voteMap = new VoteQueueMap
        {
            Origin = gameEvent.Origin,
            Server = gameEvent.Owner,
            Map = map,
            Config = _votifyConfig.VoteQueueMapConfiguration
        };
        _voteProcessor.CreateVote(gameEvent.Owner, voteMap);
    }
}
