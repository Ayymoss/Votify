using SharedLibraryCore.Interfaces;
using Votify.Models;

namespace Votify.Services;

public class MetaManager(IMetaServiceV2 metaService)
{
    public async Task<bool> IsUserVoteBlockedAsync(int clientId)
    {
        var state = await metaService.GetPersistentMetaValue<VoteBanState>(Plugin.BannedVoterKey, clientId);
        return state?.Banned ?? false;
    }

    public async Task<bool> BlockUserAsync(int clientId)
    {
        var result = await IsUserVoteBlockedAsync(clientId);
        if (result) return false;

        await metaService.SetPersistentMetaValue(Plugin.BannedVoterKey, new VoteBanState { Banned = true }, clientId);
        return true;
    }

    public async Task<bool> UnblockUserAsync(int clientId)
    {
        var result = await IsUserVoteBlockedAsync(clientId);
        if (!result) return false;

        await metaService.SetPersistentMetaValue(Plugin.BannedVoterKey, new VoteBanState { Banned = false }, clientId);
        return true;
    }
}
