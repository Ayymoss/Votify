using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Enums;

namespace Votify.Commands;

public class NoCommand : Command
{
    private readonly VoteManager _voteManager;
    private readonly VoteConfiguration _voteConfig;

    public NoCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteManager voteManager,
        VoteConfiguration voteConfiguration) : base(config, translationLookup)
    {
        _voteManager = voteManager;
        _voteConfig = voteConfiguration;
        Name = "no";
        Description = "vote no on the current vote";
        Alias = "n";
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

        var result = _voteManager.CastVote(gameEvent.Owner, gameEvent.Origin, Vote.No);
        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteConfig.Translations.VoteNo));
                break;
            case VoteResult.NoVoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
                break;
            case VoteResult.AlreadyVoted:
                gameEvent.Origin.Tell(_voteConfig.Translations.AlreadyVoted);
                break;
        }
    }
}
