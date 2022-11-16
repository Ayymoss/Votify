using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class CancelVoteCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public CancelVoteCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "cancelvote";
        Description = "cancels the current vote";
        Alias = "cv";
        Permission = EFClient.Permission.Moderator;
        RequiresTarget = false;
        _configuration = configurationHandler.Configuration();
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (Plugin.VoteManager.InProgressVote(gameEvent.Owner))
        {
            Plugin.VoteManager.CancelVote(gameEvent.Owner);
            gameEvent.Origin.Tell(_configuration.VoteMessages.VoteCancelled);
        }
        else
        {
            gameEvent.Origin.Tell(_configuration.VoteMessages.VoteInProgress);
        }
    }
}
