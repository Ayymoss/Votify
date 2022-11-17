using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace Votify.Commands;

public class VoteKickCommand : Command
{
    public VoteKickCommand(CommandConfiguration config, ITranslationLookup translationLookup) : base(config,
        translationLookup)
    {
        Name = "votekick";
        Description = "starts a vote to kick a player";
        Alias = "vk";
        Permission = EFClient.Permission.User;
        RequiresTarget = true;
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
        if (!Plugin.Configuration.IsVoteTypeEnabled.VoteKick)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteDisabled);
            return;
        }

        if (Plugin.Configuration.MinimumPlayersRequired > gameEvent.Owner.ClientNum)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.NotEnoughPlayers);
            return;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.DenySelfTarget);
            return;
        }

        var result = Plugin.Votify.CreateVote(gameEvent.Owner, VoteType.Kick, gameEvent.Origin,
            target: gameEvent.Target, reason: gameEvent.Data);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteSuccess
                    .FormatExt(Plugin.Configuration.Translations.VoteYes));
                gameEvent.Target.Tell(Plugin.Configuration.Translations.VoteSuccess
                    .FormatExt(Plugin.Configuration.Translations.VoteNo));
                gameEvent.Owner.Broadcast(Plugin.Configuration.Translations.KickBanVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, VoteType.Kick, gameEvent.Target.CleanedName,
                        gameEvent.Data));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(Plugin.Configuration.Translations.TooRecentVote);
                break;
        }
    }
}
