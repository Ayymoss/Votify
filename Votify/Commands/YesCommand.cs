using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Services;

namespace Votify.Commands;

public class YesCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteCastRunner _runner;

    public YesCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteState voteState)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _runner = new VoteCastRunner(voteConfig, voteState);
        Name = "yes";
        Description = "vote yes on the current vote";
        Alias = "y";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent) =>
        _runner.ExecuteAsync(gameEvent, Vote.Yes, _voteConfig.Translations.VoteYes);
}
