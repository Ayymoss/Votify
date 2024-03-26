using FluentValidation;
using SharedLibraryCore;
using SharedLibraryCore.Interfaces;
using Votify.Models;
using Votify.Models.VoteModel;

namespace Votify.Configuration;

public class Validation : AbstractValidator<VoteConfigurationBase>
{
    public Validation(DateTimeOffset lastVote)
    {
        RuleFor(x => x.VoteCooldown)
            .LessThan(DateTimeOffset.UtcNow - lastVote)
            .WithMessage("VoteKick cooldown not met");
    }

    public Validation(Server server, VoteBase voteBase)
    {
        RuleFor(x => x)
            .Must(config => IsEnoughVotes(config, server, voteBase))
            .WithMessage("Not enough votes cast to satisfy the configured minimum");

        RuleFor(x => x)
            .Must(config => IsEnoughPlayers(config, server))
            .WithMessage("Not enough players to satisfy the configured minimum");

        RuleFor(x => x)
            .Must(config => HasVotePercentage(config, voteBase))
            .WithMessage("The vote did not reach the required pass percentage");
    }

    // MinimumVotingPlayersPercentage
    private bool IsEnoughVotes(VoteConfigurationBase config, IGameServer server, VoteBase voteBase)
    {
        var totalVotes = voteBase.YesVotes + voteBase.NoVotes;
        var votingPercentage = (float)totalVotes / server.ConnectedClients.Count(x => !x.IsBot);
        return votingPercentage >= config.MinimumVotingPlayersPercentage;
    }

    // MinimumPlayersRequired
    private bool IsEnoughPlayers(VoteConfigurationBase config, Server server)
    {
        return server.Clients.Count >= config.MinimumPlayersRequired;
    }

    // VotePassPercentage
    private bool HasVotePercentage(VoteConfigurationBase config, VoteBase voteBase)
    {
        var totalVotes = voteBase.YesVotes + voteBase.NoVotes;
        var votePercentage = (float)voteBase.YesVotes / totalVotes;
        return votePercentage >= config.VotePassPercentage;
    }
}
