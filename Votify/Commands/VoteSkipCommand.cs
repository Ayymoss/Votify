using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Enums;

namespace Votify.Commands;

public class VoteSkipCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public VoteSkipCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config, translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "voteskip";
        Description = "starts a vote to skip the map";
        Alias = "vs";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteConfigurations.VoteSkip.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Skip));
            return;
        }

        if (_voteConfig.Core.DisabledServers.ContainsKey(gameEvent.Owner.Id) && _voteConfig.Core.DisabledServers[gameEvent.Owner.Id].Contains(VoteType.Skip))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (_voteConfig.VoteConfigurations.VoteSkip.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var result = _voteManager.CreateVote(gameEvent.Owner, VoteType.Skip, gameEvent.Origin);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                gameEvent.Owner.Broadcast(_voteConfig.Translations.SkipVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName));
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
