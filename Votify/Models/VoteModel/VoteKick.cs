using SharedLibraryCore.Database.Models;

namespace Votify.Models.VoteModel;

public class VoteKick : VoteBase
{
    public EFClient Target { get; set; } = null!; // TODO: .NET 8 use 'required' keyword
    public string Reason { get; set; } = null!; // TODO: .NET 8 use 'required' keyword
}
