using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Services;

namespace Votify.Commands;

public class CancelVoteCommand : Command
{
    private readonly VoteState _voteState;
    private readonly ConfigurationBase _voteConfig;

    public CancelVoteCommand(CommandConfiguration config, ITranslationLookup translationLookup, VoteState voteState,
        ConfigurationBase voteConfig) : base(config, translationLookup)
    {
        _voteState = voteState;
        _voteConfig = voteConfig;
        Name = "cancelvote";
        Description = "cancels the current vote";
        Alias = "cv";
        Permission = EFClient.Permission.Moderator;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteState.Votes.TryGetValue(gameEvent.Owner, out var voteBase))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
            return Task.CompletedTask;
        }

        voteBase.Item2.CancelVote(gameEvent.Owner);
        gameEvent.Origin.Tell(_voteConfig.Translations.VoteCancelled);

        return Task.CompletedTask;
    }
}
