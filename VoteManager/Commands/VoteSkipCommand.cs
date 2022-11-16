using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class VoteSkipCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public VoteSkipCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "voteskip";
        Description = "starts a vote to skip the map";
        Alias = "vs";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
        _configuration = configurationHandler.Configuration();
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (_configuration.IsVoteTypeEnabled.VoteSkip)
        {
            gameEvent.Origin.Tell(_configuration.VoteMessages.VoteDisabled);
            return;
        }

        var result = Plugin.VoteManager.CreateVote(gameEvent.Owner, gameEvent.Origin, gameEvent.Target,
            VoteType.Map);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteSuccess);
                gameEvent.Owner.Broadcast(_configuration.VoteMessages.KickBanVoteStarted.FormatExt(VoteType.Ban,
                    gameEvent.Origin.CleanedName, gameEvent.Target.CleanedName, gameEvent.Data));
                break;

            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteInProgress);
                break;
        }
    }
}
