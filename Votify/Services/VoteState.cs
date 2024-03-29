using System.Collections.Concurrent;
using SharedLibraryCore;
using Votify.Interfaces;
using Votify.Models;

namespace Votify.Services;

public class VoteState
{
    public ConcurrentDictionary<Server, Tuple<VoteBase, IVoteProcessor>> Votes { get; set; } = new();
}
