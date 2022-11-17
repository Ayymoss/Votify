using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class CancelVoteCommand : Command
{
    public CancelVoteCommand(CommandConfiguration config, ITranslationLookup translationLookup) : base(config,
        translationLookup)
    {
        Name = "cancelvote";
        Description = "cancels the current vote";
        Alias = "cv";
        Permission = EFClient.Permission.Moderator;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (Plugin.VoteManager.InProgressVote(gameEvent.Owner))
        {
            Plugin.VoteManager.CancelVote(gameEvent.Owner);
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteCancelled);
        }
        else
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.NoVoteInProgress);
        }
    }
}
