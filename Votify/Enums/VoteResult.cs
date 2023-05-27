namespace Votify.Enums;

public enum VoteResult
{
    Success,
    AlreadyVoted,
    NoVoteInProgress,
    VoteInProgress,
    VoteCooldown
}
