using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace Votify.Commands;

public class CancelVoteCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public CancelVoteCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config,
        translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "cancelvote";
        Description = "cancels the current vote";
        Alias = "cv";
        Permission = EFClient.Permission.Moderator;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (_voteManager.InProgressVote(gameEvent.Owner))
        {
            _voteManager.CancelVote(gameEvent.Owner);
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteCancelled);
        }
        else
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
        }
    }
}
