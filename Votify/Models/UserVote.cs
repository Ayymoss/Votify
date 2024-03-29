using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Enums;

namespace Votify.Models;

public class UserVote
{
    public required Server Server { get; set; }
    public required EFClient Client { get; set; }
    public required Vote Vote { get; set; }
}
