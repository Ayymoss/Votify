using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Enums;

namespace Votify.Commands;

public class VoteBanCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public VoteBanCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config, translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "voteban";
        Description = "starts a vote to ban a player";
        Alias = "vb";
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
        if (!_voteConfig.VoteConfigurations.VoteBan.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Ban));
            return;
        }

        if (_voteConfig.Core.DisabledServers.ContainsKey(gameEvent.Owner.Id) && _voteConfig.Core.DisabledServers[gameEvent.Owner.Id].Contains(VoteType.Ban))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (gameEvent.Target.IsBot)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteBot);
            return;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.DenySelfTarget);
            return;
        }

        if (gameEvent.Target.Level is not EFClient.Permission.User)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteRanked);
            return;
        }

        if (_voteConfig.VoteConfigurations.VoteBan.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var result = _voteManager.CreateVote(gameEvent.Owner, VoteType.Ban, gameEvent.Origin,
            target: gameEvent.Target, reason: gameEvent.Data);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                gameEvent.Target.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteNo));
                gameEvent.Owner.Broadcast(_voteConfig.Translations.KickBanVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, VoteType.Ban, gameEvent.Target.CleanedName,
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
