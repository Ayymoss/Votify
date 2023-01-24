using SharedLibraryCore.Interfaces;

namespace Votify;

public class ConfigurationModel : IBaseConfiguration
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
    public float MinimumVotingPlayersPercentage { get; set; } = 0.35f;
    
    /// <summary>
    /// Required number of players to start a vote
    /// </summary>
    public int MinimumPlayersRequired { get; set; } = 4;
    
    /// <summary>
    /// Length of duration in seconds a vote will last
    /// </summary>
    public int VoteDuration { get; set; } = 30;
    
    /// <summary>
    /// Length of duration in seconds until the next vote can be started
    /// </summary>
    public int VoteCooldown { get; set; } = 60;
    
    /// <summary>
    /// Interval in seconds between vote broadcast messages
    /// </summary>
    public int TimeBetweenVoteReminders { get; set; } = 5;
    
    /// <summary>
    /// Enabled or disabled vote types
    /// </summary>
    public VoteTypeConfiguration IsVoteTypeEnabled { get; set; } = new();
    
    /// <summary>
    /// Translation strings for the plugin
    /// </summary>
    public Translation Translations { get; set; } = new();

    public string Name() => "Votify";
    public IBaseConfiguration Generate() => new ConfigurationModel();
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
    public string NotEnoughVotes { get; set; } = "(Color::Yellow)VOTE(Color::White): {{type}} failed, not enough votes";
    public string NotEnoughYesVotes { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] (Color::Red)Failed, not enough yes votes!";
    public string OpenVoteAutoMessage { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
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
    public string VotePassed { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] (Color::Green)Passed!";
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
