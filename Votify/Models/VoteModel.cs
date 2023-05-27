using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Enums;

namespace Votify.Models;

public class VoteModel
{
    public EFClient? Origin { get; set; }
    public EFClient? Target { get; set; }
    public string? Reason { get; set; }
    public VoteType VoteType { get; set; }
    public DateTimeOffset Creation { get; set; }
    public Map? Map { get; set; }
    public ConcurrentDictionary<EFClient, Vote> Votes { get; set; } = null!;
    public byte YesVotes { get; set; }
    public byte NoVotes { get; set; }
}
