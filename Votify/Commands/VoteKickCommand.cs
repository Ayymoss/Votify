using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;

namespace Votify.Commands;

public class VoteKickCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteKickProcessor _processor;

    public VoteKickCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteKickProcessor processor)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _processor = processor;
        Name = "votekick";
        Description = "starts a vote to kick a player";
        Alias = "vk";
        Permission = Data.Models.Client.EFClient.Permission.User;
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

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteKickConfiguration.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Kick));
            return Task.CompletedTask;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType.Kick))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return Task.CompletedTask;
        }

        if (gameEvent.Target.IsBot)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteBot);
            return Task.CompletedTask;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.DenySelfTarget);
            return Task.CompletedTask;
        }

        if (gameEvent.Target.Level is not Data.Models.Client.EFClient.Permission.User)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteRanked);
            return Task.CompletedTask;
        }

        if (_voteConfig.VoteKickConfiguration.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return Task.CompletedTask;
        }

        var vote = new VoteKick
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

        var result = _processor.CreateVote(gameEvent.Owner, vote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Owner.Broadcast(_voteConfig.Translations.KickBanVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, VoteType.Kick, gameEvent.Target.CleanedName,
                        gameEvent.Data));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_voteConfig.Translations.TooRecentVote);
                break;
            case VoteResult.NotEnoughPlayers:
                gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
                break;
            case VoteResult.AbusiveVoter:
                gameEvent.Origin.Tell(_voteConfig.Translations.AbusiveVoter);
                break;
        }

        return Task.CompletedTask;
    }
}
