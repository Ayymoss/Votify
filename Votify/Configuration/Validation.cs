using FluentValidation;
using SharedLibraryCore.Interfaces;
using Votify.Enums;
using Votify.Models;

namespace Votify.Configuration;

public class Validation : AbstractValidator<VoteConfigurationBase>
{
    public Validation(DateTimeOffset lastVote)
    {
        RuleFor(x => x.VoteCooldown)
            .LessThan(DateTimeOffset.UtcNow - lastVote)
            .WithMessage(nameof(VoteResult.VoteCooldown));
    }

    public Validation(IGameServer server, VoteBase voteBase)
    {
        RuleFor(x => x)
            .Must(config => IsEnoughVotes(config, server, voteBase))
            .WithMessage(nameof(VoteResult.NotEnoughVotes));

        RuleFor(x => x)
            .Must(config => IsEnoughPlayers(config, voteBase))
            .WithMessage(nameof(VoteResult.NotEnoughPlayers));

        RuleFor(x => x)
            .Must(config => HasVotePercentage(config, voteBase))
            .WithMessage(nameof(VoteResult.VoteFailed));
    }

    // MinimumVotingPlayersPercentage
    private static bool IsEnoughVotes(VoteConfigurationBase config, IGameServer server, VoteBase voteBase)
    {
        var totalVotes = voteBase.YesVotes + voteBase.NoVotes;
        var votingPercentage = (float)totalVotes / server.ConnectedClients.Count(x => !x.IsBot);
        return votingPercentage >= config.MinimumVotingPlayersPercentage;
    }

    // MinimumPlayersRequired
    private static bool IsEnoughPlayers(VoteConfigurationBase config, VoteBase voteBase)
    {
        return voteBase.Votes.Count >= config.MinimumPlayersRequired;
    }

    // VotePassPercentage
    private static bool HasVotePercentage(VoteConfigurationBase config, VoteBase voteBase)
    {
        var totalVotes = voteBase.YesVotes + voteBase.NoVotes;
        var votePercentage = (float)voteBase.YesVotes / totalVotes;
        return votePercentage >= config.VotePassPercentage;
    }
}
