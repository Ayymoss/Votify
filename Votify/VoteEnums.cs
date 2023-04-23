namespace Votify;

public abstract class VoteEnums
{
    public enum VoteType
    {
        Kick,
        Map,
        Ban,
        Skip
    }

    public enum VoteResult
    {
        Success,
        AlreadyVoted,
        NoVoteInProgress,
        VoteInProgress,
        VoteCooldown,
    }

    public enum Vote
    {
        Yes,
        No
    }
}
