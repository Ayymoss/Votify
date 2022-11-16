using SharedLibraryCore.Interfaces;

namespace VoteManager;

public class ConfigurationModel : IBaseConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public int PercentageVotePassed { get; set; } = 50;
    public int MinimumPlayersRequired { get; set; } = 4;
    public int MinimumPlayersRequiredForSuccessfulVote { get; set; } = 2;
    public int VoteDuration { get; set; } = 30;
    public int VoteCooldown { get; set; } = 60;
    public int TimeBetweenVoteReminders { get; set; } = 10;
    public VoteTypeConfiguration IsVoteTypeEnabled { get; set; } = new();
    public VoteMessages VoteMessages { get; set; } = new();

    public string Name() => "VoteManagerSettings";
    public IBaseConfiguration Generate() => new ConfigurationModel();
}

public class VoteTypeConfiguration
{
    public bool VoteBan { get; set; } = true;
    public bool VoteKick { get; set; } = true;
    public bool VoteMap { get; set; } = true;
    public bool VoteSkip { get; set; } = true;
}

public class VoteMessages
{
    public string NotEnoughVotes { get; set; } = "VOTE: {{type}} on {target}} failed, not enough votes";
    public string NotEnoughYesVotes { get; set; } = "VOTE: {{type}} on {{target}} failed, not enough yes votes";
    public string OpenVoteAutoMessage { get; set; } = "VOTE: There is an ongoing {{type}} vote. Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
    public string VoteCancelledDueToPlayerDisconnect { get; set; } = "VOTE: Vote {{type}} cancelled due to player disconnect";
    public string VoteInProgress { get; set; } = "VOTE: There is already a vote in progress";
    public string KickBanVoteStarted { get; set; } = "VOTE: {{origin}} wants to {{type}} {{target}} for {{reason}}";
    public string MapVoteStarted { get; set; } = "VOTE: {{origin}} wants to change the map to {{mapName}}";
    public string SkipVoteStarted { get; set; } = "VOTE: {{origin}} wants to skip this map";
    public string VoteCancelled { get; set; } = "VOTE: {{type}} vote cancelled";
    public string NoVoteInProgress { get; set; } = "VOTE: No vote in progress";
    public string VoteSuccess { get; set; } = "VOTE: Your vote has been counted";
    public string AlreadyVoted { get; set; } = "VOTE: You have already voted";
    public string VoteDisabled { get; set; } = "VOTE: {{type}} votes are disabled";
    public string VoteCooldown { get; set; } = "VOTE: You are voting too quickly";
}
