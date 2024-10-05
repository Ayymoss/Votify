using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Services;

namespace Votify.Commands;

public class VoteBlockCommand : Command
{
    private readonly MetaManager _metaManager;
    private readonly ConfigurationBase _voteConfig;

    public VoteBlockCommand(CommandConfiguration config, ITranslationLookup translationLookup, MetaManager metaManager,
        ConfigurationBase voteConfig)
        : base(config, translationLookup)
    {
        _metaManager = metaManager;
        _voteConfig = voteConfig;
        Name = "voteblock";
        Description = "starts a vote to kick a player";
        Alias = "vblock";
        Permission = Data.Models.Client.EFClient.Permission.SeniorAdmin;
        RequiresTarget = true;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        await _metaManager.BlockUserAsync(gameEvent.Target.ClientId);
        gameEvent.Origin.Tell(_voteConfig.Translations.UserVoteBlocked.FormatExt(gameEvent.Target.CleanedName));
    }
}
