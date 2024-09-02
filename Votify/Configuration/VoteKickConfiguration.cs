namespace Votify.Configuration;

public class VoteKickConfiguration : VoteConfigurationBase
{
    public float BadPlayerMinKdr { get; set; } = 1f;
    public bool CanBadPlayersVote { get; set; }
}
