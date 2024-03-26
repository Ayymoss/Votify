using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Enums;
using Votify.Models;

namespace Votify.Interfaces;

public interface IVoteProcessor
{
    VoteResult RegisterUserVote(UserVote userVote);
    void CancelVote(Server server);
    void RemoveClient(EFClient client);
}
