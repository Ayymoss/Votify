using System.Drawing;

namespace Votify.Configuration;

public class Translation
{
// @formatter:off
    public string NotEnoughVotes { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): (Color::Red)Failed! Not enough votes!";
    public string NotEnoughYesVotes { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): (Color::Red)Failed! Not enough Yes votes!";
    public string OpenVoteAutoMessage { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):{{abstains}}:(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
    public string OpenVoteAutoMessageNoTarget { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):{{abstains}}:(Color::Red){{no}}(Color::White)] Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
    public string VotePassed { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): [(Color::Green){{yes}}(Color::White):{{abstains}}:(Color::Red){{no}}(Color::White) @ (Color::Yellow){{target}}(Color::White)] (Color::Green)Passed!";
    public string VoteQueued { get; set; } = "(Color::Yellow)VOTE {{type}}(Color::White): (Color::Green)Queued! (Color::Yellow){{target}} (Color::White)will be played next";
    public string VoteCancelledDueToPlayerDisconnect { get; set; } = "(Color::Yellow)VOTE(Color::White): Vote {{type}} cancelled due to player disconnect";
    public string VoteInProgress { get; set; } = "(Color::Yellow)VOTE(Color::White): There is already a vote in progress";
    public string KickBanVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): (Color::Accent){{origin}} (Color::White)wants to (Color::Accent){{type}} (Color::Yellow){{target}} (Color::White)for (Color::Accent){{reason}}";
    public string MapVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): (Color::Accent){{origin}} (Color::White)wants to (Color::Accent)Change (Color::White)the map to (Color::Yellow){{mapName}}";
    public string QueueVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): (Color::Accent){{origin}} (Color::White)wants to (Color::Accent)Queue (Color::White)the map (Color::Yellow){{mapName}} (Color::White)for the next round";
    public string SkipVoteStarted { get; set; } = "(Color::Yellow)VOTE(Color::White): (Color::Accent){{origin}} (Color::White)wants to (Color::Accent)Skip (Color::White)this map";
    public string VoteCancelled { get; set; } = "(Color::Yellow)VOTE(Color::White): Vote cancelled";
    public string NoVoteInProgress { get; set; } = "(Color::Yellow)VOTE(Color::White): No vote in progress";
    public string VoteSuccess { get; set; } = "(Color::Yellow)VOTE(Color::White): Your {{vote}} vote has been counted";
    public string AlreadyVoted { get; set; } = "(Color::Yellow)VOTE(Color::White): You have already voted";
    public string VoteDisabled { get; set; } = "(Color::Yellow)VOTE(Color::White): {{type}} votes are disabled";
    public string TooRecentVote { get; set; } = "(Color::Yellow)VOTE(Color::White): There was a recent vote, please wait";
    public string VoteAction { get; set; } = "VOTE: {{reason}} [y{{yes}}:a{{abstains}}:n{{no}}]";
    public string MapNotFound { get; set; } = "(Color::Yellow)VOTE(Color::White): Map not found";
    public string NotEnoughPlayers { get; set; } = "(Color::Yellow)VOTE(Color::White): Not enough players to start a vote";
    public string DenySelfTarget { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot target yourself";
    public string CannotVoteBot { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot vote on bots";
    public string CannotVoteRanked { get; set; } = "(Color::Yellow)VOTE(Color::White): You cannot vote on ranked players";
    public string VoteYes { get; set; } = "(Color::Green)Yes(Color::White)";
    public string VoteNo { get; set; } = "(Color::Red)No(Color::White)";
    public string VoteDisabledServer { get; set; } = "(Color::Yellow)VOTE(Color::White): Votify disabled on this server";
    public string Kick { get; set; } = "(Color::Accent)Kick(Color::White)";
    public string Ban { get; set; } = "(Color::Accent)Ban(Color::White)";
    public string Skip { get; set; } = "(Color::Accent)Skip(Color::White)";
    public string Map { get; set; } = "(Color::Accent)Map(Color::White)";
    public string AbusiveVoter { get; set; } = "(Color::Yellow)VOTE(Color::White): You have voted too many times recently. Please wait before voting again";
    public string VoteDisabledPoorPerformance { get; set; } = "[(Color::Red){{kdr}} (Color::White)< (Color::Green){{target}}(Color::White)] (Color::Yellow)Improve your KDR to unlock player voting";
    public string VoteBlocked { get; set; } = "(Color::Yellow)VOTE(Color::White): You do not have permission to create player votes";
    public string UserVoteBlocked { get; set; } = "(Color::Yellow)VOTE(Color::White): You have blocked player voting for {{target}}";
    public string UserVoteUnblocked { get; set; } = "(Color::Yellow)VOTE(Color::White): You have unblocked player voting for {{target}}";
    // @formatter:on
}
