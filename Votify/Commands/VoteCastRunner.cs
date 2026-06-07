using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models;
using Votify.Services;

namespace Votify.Commands;

// Shared execution logic for casting a yes/no vote. Not a Command for the same
// reason as TargetedVoteRunner: IW4MAdmin instantiates every Command-derived class
// during discovery, so an abstract Command base would crash host startup.
public sealed class VoteCastRunner(ConfigurationBase voteConfig, VoteState voteState)
{
    public Task ExecuteAsync(GameEvent gameEvent, Vote voteDirection, string voteTranslation)
    {
        var userVote = new UserVote
        {
            Server = gameEvent.Owner,
            Client = gameEvent.Origin,
            Vote = voteDirection
        };

        if (!voteState.Votes.TryGetValue(gameEvent.Owner, out var voteBase))
        {
            gameEvent.Origin.Tell(voteConfig.Translations.NoVoteInProgress);
            return Task.CompletedTask;
        }

        var result = voteBase.Item2.RegisterUserVote(userVote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(voteConfig.Translations.VoteSuccess.FormatExt(voteTranslation));
                break;
            case VoteResult.AlreadyVoted:
                gameEvent.Origin.Tell(voteConfig.Translations.AlreadyVoted);
                break;
        }

        return Task.CompletedTask;
    }
}
