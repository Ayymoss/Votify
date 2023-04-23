using System.Collections.Concurrent;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore;

namespace Votify;

public record VoteModel
{
    public EFClient? Origin { get; set; }
    public EFClient? Target { get; set; }
    public string? Reason { get; set; }
    public VoteEnums.VoteType VoteType { get; set; }
    public DateTimeOffset Creation { get; set; }
    public Map? Map { get; set; }
    public ConcurrentDictionary<EFClient, VoteEnums.Vote> Votes { get; set; } = null!;
    public byte YesVotes { get; set; }
    public byte NoVotes { get; set; }
}
