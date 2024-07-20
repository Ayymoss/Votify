using System.Collections.Concurrent;
using Data.Models.Client;
using SharedLibraryCore;
using Votify.Interfaces;
using Votify.Models;

namespace Votify.Services;

public class VoteState
{
    public ConcurrentDictionary<Server, Tuple<VoteBase, IVoteProcessor>> Votes { get; set; } = [];
    public ConcurrentDictionary<EFClient, VoteCooldownInfo> UserVoteCooldownTracker { get; set; } = [];

    public class VoteCooldownInfo(DateTimeOffset firstVote)
    {
        public List<DateTimeOffset> LastVotes { get; set; } = [firstVote];
        public DateTimeOffset? CooldownEnd { get; set; }
    }
}
