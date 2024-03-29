using SharedLibraryCore;

namespace Votify.Models.VoteModel;

public class VoteMap : VoteBase
{
    public required Map Map { get; set; }
}
