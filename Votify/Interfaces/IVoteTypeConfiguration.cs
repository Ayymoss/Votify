namespace Votify.Interfaces;

public interface IVoteTypeConfiguration
{
    /// <summary>
    /// Is this vote type enabled?
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Required percentage of yes votes to pass a vote
    /// </summary>
    float VotePassPercentage { get; set; }

    /// <summary>
    /// Required number of voters to pass a vote
    /// </summary>
    float MinimumVotingPlayersPercentage { get; set; }

    /// <summary>
    /// Required number of players to start a vote
    /// </summary>
    int MinimumPlayersRequired { get; set; }

    /// <summary>
    /// Length of duration in seconds until the next vote can be started
    /// </summary>
    TimeSpan VoteCooldown { get; set; }
}
