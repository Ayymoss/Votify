using System.Collections.Concurrent;
using Data.Models.Client;
using SharedLibraryCore;
using Votify.Interfaces;
using Votify.Models;

namespace Votify.Services;

public class VoteState
{
    public ConcurrentDictionary<Server, Tuple<VoteBase, IVoteProcessor>> Votes { get; set; } = [];
    public ConcurrentDictionary<EFClient, CooldownData> UserCooldowns { get; set; } = [];
    public record CooldownData(List<DateTimeOffset> LastVotes, DateTimeOffset? CooldownEnd);
}
