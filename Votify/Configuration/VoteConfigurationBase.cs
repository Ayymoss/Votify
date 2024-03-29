using FluentValidation.Results;
using SharedLibraryCore;
using Votify.Models;

namespace Votify.Configuration;

public abstract class VoteConfigurationBase
{
    public bool IsEnabled { get; set; } = true;
    public float VotePassPercentage { get; set; } = 0.51f;
    public float MinimumVotingPlayersPercentage { get; set; } = 0.20f;
    public int MinimumPlayersRequired { get; set; } = 4;
    public TimeSpan VoteCooldown { get; set; } = TimeSpan.FromMinutes(5);

    public virtual ValidationResult Validate(DateTimeOffset lastVote, Server server)
    {
        var validator = new Validation(lastVote, server);
        return validator.Validate(this);
    }

    public virtual ValidationResult Validate(Server server, VoteBase voteBase)
    {
        var validator = new Validation(server, voteBase);
        return validator.Validate(this);
    }
}
