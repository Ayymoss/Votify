using System.Collections.Concurrent;
using SharedLibraryCore.Database.Models;
using Votify.Enums;

namespace Votify.Models;

public class VoteBase
{
    public required EFClient Initiator { get; set; }
    public required DateTimeOffset Created { get; set; }
    public ConcurrentDictionary<EFClient, Vote> Votes { get; set; } = [];
    public int YesVotes => Votes.Count(x => x.Value is Vote.Yes);
    public int NoVotes => Votes.Count(x => x.Value is Vote.No);
}
