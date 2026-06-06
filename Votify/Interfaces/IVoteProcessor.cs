using SharedLibraryCore;
using SharedLibraryCore.Database.Models;
using Votify.Enums;
using Votify.Models;

namespace Votify.Interfaces;

public interface IVoteProcessor
{
    VoteResult CreateVote(Server server, VoteBase voteBase);
    VoteResult RegisterUserVote(UserVote userVote);
    void CancelVote(Server server, string cancelledBy);
    void RemoveClient(EFClient client);
}
