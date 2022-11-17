using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class VoteSkipCommand : Command
{
    public VoteSkipCommand(CommandConfiguration config, ITranslationLookup translationLookup) : base(config,
        translationLookup)
    {
        Name = "voteskip";
        Description = "starts a vote to skip the map";
        Alias = "vs";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!Plugin.Configuration.IsVoteTypeEnabled.VoteSkip)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteDisabled);
            return;
        }

        if (Plugin.Configuration.MinimumPlayersRequired > gameEvent.Owner.ClientNum)
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.NotEnoughPlayers);
            return;
        }

        var result = Plugin.VoteManager.CreateVote(gameEvent.Owner, VoteType.Skip, gameEvent.Origin);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteSuccess
                    .FormatExt(Plugin.Configuration.Translations.VoteYes));
                gameEvent.Owner.Broadcast(Plugin.Configuration.Translations.SkipVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName));
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
