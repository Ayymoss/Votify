using System.Collections.Concurrent;
using Data.Abstractions;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Commands;

public class VoteBanCommand : TargetedVoteCommand<VoteBan>
{
    public VoteBanCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteBanProcessor processor, IDatabaseContextFactory contextFactory, MetaManager metaManager)
        : base(config, translationLookup, voteConfig, processor, contextFactory, metaManager)
    {
        Name = "voteban";
        Description = "starts a vote to ban a player";
        Alias = "vb";
        Permission = EFClient.Permission.User;
        RequiresTarget = true;
        Arguments =
        [
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_PLAYER"],
                Required = true
            },
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_REASON"],
                Required = true
            }
        ];
    }

    protected override VoteType VoteType => VoteType.Ban;
    protected override VoteConfigurationBase VoteTypeConfig => _voteConfig.VoteBanConfiguration;

    protected override VoteBan CreateVoteObject(GameEvent gameEvent) => new()
    {
        Initiator = gameEvent.Origin,
        Created = DateTimeOffset.UtcNow,
        Votes = new ConcurrentDictionary<EFClient, Vote>
        {
            [gameEvent.Origin] = Vote.Yes,
            [gameEvent.Target] = Vote.No
        },
        Target = gameEvent.Target,
        Reason = gameEvent.Data
    };

    protected override string GetSuccessMessage(GameEvent gameEvent) =>
        _voteConfig.Translations.KickBanVoteStarted
            .FormatExt(gameEvent.Origin.CleanedName, VoteType.Ban, gameEvent.Target.CleanedName, gameEvent.Data);
}
