using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class VoteMapCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public VoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "votemap";
        Description = "starts a vote to change the map";
        Alias = "vm";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
        _configuration = configurationHandler.Configuration();
        Arguments = new[]
        {
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_MAP"],
                Required = true
            }
        };
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_configuration.IsVoteTypeEnabled.VoteMap)
        {
            gameEvent.Origin.Tell(_configuration.VoteMessages.VoteDisabled);
            return;
        }

        var result = Plugin.VoteManager.CreateVote(gameEvent.Owner, gameEvent.Origin, gameEvent.Target,
            VoteType.Map, mapName: gameEvent.Data);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteSuccess);
                gameEvent.Owner.Broadcast(_configuration.VoteMessages.MapVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, gameEvent.Data));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteCooldown);
                break;
        }
    }
}
