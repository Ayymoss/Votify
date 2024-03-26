namespace Votify.Configuration;

public class VoteBanConfiguration : VoteConfigurationBase
{
    public TimeSpan VoteBanDuration { get; set; } = TimeSpan.FromMinutes(30);
}
