using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Enums;

namespace Votify.Commands;

public class VoteMapCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public VoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config, translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "votemap";
        Description = "starts a vote to change the map";
        Alias = "vm";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
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
        if (!_voteConfig.VoteConfigurations.VoteMap.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Map));
            return;
        }

        if (_voteConfig.Core.DisabledServers.ContainsKey(gameEvent.Owner.Id) && _voteConfig.Core.DisabledServers[gameEvent.Owner.Id].Contains(VoteType.Map))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (_voteConfig.VoteConfigurations.VoteMap.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var input = gameEvent.Data.Trim();
        var foundMap = gameEvent.Owner.Maps.FirstOrDefault(map =>
            map.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase) ||
            map.Alias.Equals(input, StringComparison.InvariantCultureIgnoreCase));

        if (foundMap is null)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.MapNotFound);
            return;
        }

        var result = _voteManager.CreateVote(gameEvent.Owner, VoteType.Map, gameEvent.Origin, map: foundMap);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                gameEvent.Owner.Broadcast(_voteConfig.Translations.MapVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, foundMap.Alias));
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
