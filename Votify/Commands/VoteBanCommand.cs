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

public class VoteBanCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteBanProcessor _processor;

    public VoteBanCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteBanProcessor processor)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _processor = processor;
        Name = "voteban";
        Description = "starts a vote to ban a player";
        Alias = "vb";
        Permission = EFClient.Permission.User;
        RequiresTarget = true;
        Arguments = new[]
        {
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
        };
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteBanConfiguration.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Ban));
            return Task.CompletedTask;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType.Ban))
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

        if (_voteConfig.VoteBanConfiguration.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return Task.CompletedTask;
        }

        var vote = new VoteBan
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
                    .FormatExt(gameEvent.Origin.CleanedName, VoteType.Ban, gameEvent.Target.CleanedName,
                        gameEvent.Data));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_voteConfig.Translations.TooRecentVote);
                break;
        }

        return Task.CompletedTask;
    }
}
