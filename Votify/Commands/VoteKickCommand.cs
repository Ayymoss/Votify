using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Enums;

namespace Votify.Commands;

public class VoteKickCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public VoteKickCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config, translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
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
        if (!_voteConfig.VoteConfigurations.VoteKick.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Kick));
            return;
        }

        if (_voteConfig.Core.DisabledServers.ContainsKey(gameEvent.Owner.Id) && _voteConfig.Core.DisabledServers[gameEvent.Owner.Id].Contains(VoteType.Kick))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (gameEvent.Target.IsBot && !_voteConfig.IsDebug)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteBot);
            return;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId && !_voteConfig.IsDebug)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.DenySelfTarget);
            return;
        }

        if (gameEvent.Target.Level is not EFClient.Permission.User && !_voteConfig.IsDebug)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteRanked);
            return;
        }

        if (_voteConfig.VoteConfigurations.VoteKick.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var result = _voteManager.CreateVote(gameEvent.Owner, VoteType.Kick, gameEvent.Origin,
            target: gameEvent.Target, reason: gameEvent.Data);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                gameEvent.Target.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteNo));
                gameEvent.Owner.Broadcast(_voteConfig.Translations.KickBanVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, VoteType.Kick, gameEvent.Target.CleanedName,
                        gameEvent.Data));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_voteConfig.Translations.TooRecentVote);
                break;
        }
    }
}
