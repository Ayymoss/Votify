using SharedLibraryCore;

namespace Votify.Models.VoteModel;

public class VoteMap : VoteBase
{
    public Map Map { get; set; } = null!; // TODO: .NET 8 use 'required' keyword
}
