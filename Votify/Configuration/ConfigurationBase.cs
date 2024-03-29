using Votify.Enums;

namespace Votify.Configuration;

public class ConfigurationBase
{
    /// <summary>
    /// Enable or disable the plugin
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Length of duration in seconds a vote will last
    /// </summary>
    public TimeSpan VoteDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Interval in seconds between vote broadcast messages
    /// </summary>
    public TimeSpan TimeBetweenVoteReminders { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// A list of Server IPs that the plugin will be disabled on
    /// </summary>
    public Dictionary<string, VoteType[]> DisabledServers { get; set; } = new()
    {
        { "example.server.com:8123", [VoteType.Map] },
        { "123.123.123.123:4321", [VoteType.Kick, VoteType.Skip, VoteType.Ban] },
    };

    public VoteBanConfiguration VoteBanConfiguration { get; set; } = new();
    public VoteKickConfiguration VoteKickConfiguration { get; set; } = new();
    public VoteMapConfiguration VoteMapConfiguration { get; set; } = new();
    public VoteSkipConfiguration VoteSkipConfiguration { get; set; } = new();

    /// <summary>
    /// Translation strings for the plugin
    /// </summary>
    public Translation Translations { get; set; } = new();
}
