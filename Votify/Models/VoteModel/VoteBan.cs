using SharedLibraryCore.Database.Models;

namespace Votify.Models.VoteModel;

public class VoteBan : VoteBase
{
    public required EFClient Target { get; set; }
    public required string Reason { get; set; }
}
