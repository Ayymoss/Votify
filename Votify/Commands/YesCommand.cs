using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace Votify.Commands;

public class YesCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public YesCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config,
        translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "yes";
        Description = "vote yes on the current vote";
        Alias = "y";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteManager.InProgressVote(gameEvent.Owner))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
            return;
        }

        var result = _voteManager.CastVote(gameEvent.Owner, gameEvent.Origin, VoteEnums.Vote.Yes);
        switch (result)
        {
            case VoteEnums.VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteYes));
                break;
            case VoteEnums.VoteResult.NoVoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
                break;
            case VoteEnums.VoteResult.AlreadyVoted:
                gameEvent.Origin.Tell(_voteConfig.Translations.AlreadyVoted);
                break;
        }
    }
}
