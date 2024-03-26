using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Enums;

namespace Votify.Models;

public class UserVote
{
    public Server Server { get; set; }
    public EFClient Client { get; set; }
    public Vote Vote { get; set; }
}
