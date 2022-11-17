using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class VoteMapCommand : Command
{
    public VoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup) : base(config,
        translationLookup)
    {
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
        if (!Plugin.Configuration.IsVoteTypeEnabled.VoteMap)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteDisabled);
            return;
        }
        
        var input = gameEvent.Data.Trim();
        var foundMap = gameEvent.Owner.Maps.FirstOrDefault(map =>
            map.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase) ||
            map.Alias.Equals(input, StringComparison.InvariantCultureIgnoreCase));
        
        if (foundMap is null)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.MapNotFound);
            return;
        }

        var result = Plugin.VoteManager.CreateVote(gameEvent.Owner, VoteType.Map,gameEvent.Origin, mapName: foundMap.Name);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteSuccess);
                gameEvent.Owner.Broadcast(Plugin.Configuration.Translations.MapVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, foundMap.Name));
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
