using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace Votify.Commands;

public class VoteMapCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public VoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config,
        translationLookup)
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
        if (!_voteConfig.IsVoteTypeEnabled.VoteMap)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteEnums.VoteType.Map));
            return;
        }

        if (_voteConfig.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
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

        var result = _voteManager.CreateVote(gameEvent.Owner, VoteEnums.VoteType.Map, gameEvent.Origin, map: foundMap);

        switch (result)
        {
            case VoteEnums.VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                gameEvent.Owner.Broadcast(_voteConfig.Translations.MapVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, foundMap.Alias));
                break;
            case VoteEnums.VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteInProgress);
                break;
            case VoteEnums.VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_voteConfig.Translations.TooRecentVote);
                break;
        }
    }
}
