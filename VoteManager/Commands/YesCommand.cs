using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class YesCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public YesCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "yes";
        Description = "vote yes on the current vote";
        Alias = "y";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
        _configuration = configurationHandler.Configuration();
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (Plugin.VoteManager.InProgressVote(gameEvent.Owner))
        {
            var result = Plugin.VoteManager.CastVote(gameEvent.Owner, gameEvent.Origin, Vote.Yes);
            switch (result)
            {
                case VoteResult.Success:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.VoteSuccess);
                    break;
                case VoteResult.NoVoteInProgress:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.NoVoteInProgress);
                    break;
                case VoteResult.AlreadyVoted:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.AlreadyVoted);
                    break;
            }
        }
        else
        {
            gameEvent.Origin.Tell("There is no vote in progress");
        }
    }
}
