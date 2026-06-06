namespace Votify.Configuration;

public interface IKdrVoteConfiguration
{
    bool CanBadPlayersVote { get; }
    float BadPlayerMinKdr { get; }
}
