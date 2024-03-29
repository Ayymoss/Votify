namespace Votify.Enums;

public enum VoteResult
{
    Success,
    AlreadyVoted,
    VoteInProgress,
    VoteCooldown,
    VoteFailed,
    NoVoteInProgress,
    NotEnoughPlayers,
    NotEnoughVotes
}
