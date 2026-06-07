using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Services;

namespace Votify.Commands;

public class NoCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteCastRunner _runner;

    public NoCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteState voteState)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _runner = new VoteCastRunner(voteConfig, voteState);
        Name = "no";
        Description = "vote no on the current vote";
        Alias = "n";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent) =>
        _runner.ExecuteAsync(gameEvent, Vote.No, _voteConfig.Translations.VoteNo);
}
