namespace Votify.Configuration;

public class VoteBanConfiguration : VoteConfigurationBase
{
    public TimeSpan VoteBanDuration { get; set; } = TimeSpan.FromMinutes(30);
    public bool CanBadPlayersVote { get; set; }
    public float BadPlayerMinKdr { get; set; } = 1.2f;
}
