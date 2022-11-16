using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class VoteKickCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public VoteKickCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "votekick";
        Description = "starts a vote to kick a player";
        Alias = "vk";
        Permission = EFClient.Permission.User;
        RequiresTarget = true;
        _configuration = configurationHandler.Configuration();
        Arguments = new[]
        {
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_PLAYER"],
                Required = true
            },
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_REASON"],
                Required = true
            }
        };
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (_configuration.IsVoteTypeEnabled.VoteKick)
        {
            gameEvent.Origin.Tell(_configuration.VoteMessages.VoteDisabled);
            return;
        }

        var result = Plugin.VoteManager.CreateVote(gameEvent.Owner, gameEvent.Origin, gameEvent.Target,
            VoteType.Kick, reason: gameEvent.Data);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteSuccess);
                gameEvent.Owner.Broadcast(_configuration.VoteMessages.KickBanVoteStarted.FormatExt(VoteType.Kick,
                    gameEvent.Origin.CleanedName, gameEvent.Target.CleanedName, gameEvent.Data));
                break;

            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_configuration.VoteMessages.VoteInProgress);
                break;
        }
    }
}
