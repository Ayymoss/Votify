using SharedLibraryCore.Interfaces;

namespace Votify;

public class VoteConfiguration
{
    /// <summary>
    /// Enable or disable the plugin
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Required percentage of yes votes to pass a vote
    /// </summary>
    public float VotePassPercentage { get; set; } = 0.51f;

    /// <summary>
    /// Required number of voters to pass a vote
    /// </summary>
    public float MinimumVotingPlayersPercentage { get; set; } = 0.20f;

    /// <summary>
    /// Required number of players to start a vote
    /// </summary>
    public int MinimumPlayersRequired { get; set; } = 4;

    /// <summary>
    /// Length of duration in seconds a vote will last
    /// </summary>
    public TimeSpan VoteDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Length of duration in seconds until the next vote can be started
    /// </summary>
    public TimeSpan VoteCooldown { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The length of time the target player is banned for when a successful vote to be banned
    /// </summary>
    public TimeSpan VoteBanDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Interval in seconds between vote broadcast messages
    /// </summary>
    public TimeSpan TimeBetweenVoteReminders { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Enabled or disabled vote types
    /// </summary>
    public VoteTypeConfiguration IsVoteTypeEnabled { get; set; } = new();

    /// <summary>
    /// Translation strings for the plugin
    /// </summary>
    public Translation Translations { get; set; } = new();

    /// <summary>
    /// For debugging purposes
    /// </summary>
    public bool IsDebug { get; set; } = false;
}

public class VoteTypeConfiguration
{
    /// <summary>
    /// Enable or disable Vote Ban
    /// </summary>
    public bool VoteBan { get; set; } = false;

    /// <summary>
    /// Enable or disable Vote Kick
    /// </summary>
    public bool VoteKick { get; set; } = true;

    /// <summary>
    /// Enable or disable Vote Map
    /// </summary>
    public bool VoteMap { get; set; } = true;

    /// <summary>
    /// Enable or disable Vote Skip
    /// </summary>
    public bool VoteSkip { get; set; } = true;
}

public class Translation
{
    public string NotEnoughVotes { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): (Color::Red)Failed, not enough votes!";
    public string NotEnoughYesVotes { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): (Color::Red)Failed, not enough yes votes!";
    public string OpenVoteAutoMessage { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):{{abstains}}:(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
    public string VotePassed { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):{{abstains}}:(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] (Color::Green)Passed!";
    public string VoteCancelledDueToPlayerDisconnect { get; set; } = "(Color::Yellow)VOTE(Color::White): Vote {{type}} cancelled due to player disconnect";
    public string VoteInProgress { get; set; } = "(Color::Yellow)VOTE(Color::White): There is already a vote in progress";
    public string KickBanVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): {{origin}} wants to {{type}} {{target}} for {{reason}}";
    public string MapVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): {{origin}} wants to change the map to {{mapName}}";
    public string SkipVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): {{origin}} wants to skip this map";
    public string VoteCancelled { get; set; } = "(Color::Yellow)VOTE(Color::White): Vote cancelled";
    public string NoVoteInProgress { get; set; } = "(Color::Yellow)VOTE(Color::White): No vote in progress";
    public string VoteSuccess { get; set; } = "(Color::Yellow)VOTE(Color::White): Your {{vote}} vote has been counted";
    public string AlreadyVoted { get; set; } = "(Color::Yellow)VOTE(Color::White): You have already voted";
    public string VoteDisabled { get; set; } = "(Color::Yellow)VOTE(Color::White): {{type}} votes are disabled";
    public string TooRecentVote { get; set; } = "(Color::Yellow)VOTE(Color::White): There was a recent vote, please wait";
    public string VoteAction { get; set; } = "VOTE by {{origin}} (@{{originId}}): {{reason}}";
    public string MapNotFound { get; set; } = "(Color::Yellow)VOTE(Color::White): Map not found";
    public string NotEnoughPlayers { get; set; } = "(Color::Yellow)VOTE(Color::White): Not enough players to start a vote";
    public string VoteKickCancelledDueToTargetDisconnect { get; set; } = "(Color::Yellow)VOTE(Color::White): Vote kick cancelled due to target disconnect";
    public string DenySelfTarget { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot target yourself";
    public string CannotVoteBot { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot vote on bots";
    public string CannotVoteRanked { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot vote on ranked players";
    public string VoteYes { get; set; } = "(Color::Green)Yes(Color::White)";
    public string VoteNo { get; set; } = "(Color::Red)No(Color::White)";
}
