using System.Collections.Concurrent;
using Data.Abstractions;
using Data.Context;
using Data.Models.Client.Stats;
using Microsoft.EntityFrameworkCore;
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

namespace Votify.Commands;

public class VoteBanCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteBanProcessor _processor;
    private readonly IDatabaseContextFactory _contextFactory;

    public VoteBanCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteBanProcessor processor, IDatabaseContextFactory contextFactory)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _processor = processor;
        _contextFactory = contextFactory;
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

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteBanConfiguration.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Ban));
            return;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType.Ban))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (!_voteConfig.VoteBanConfiguration.CanBadPlayersVote &&
            !gameEvent.Owner.GametypeName.Contains("zom", StringComparison.CurrentCultureIgnoreCase))
        {
            var context = _contextFactory.CreateContext(false);

            var stats = await context.ClientStatistics
                .Where(x => x.ClientId == gameEvent.Origin.ClientId)
                .GroupBy(x => x.ClientId)
                .Select(g => new KdrStats(g.Sum(x => x.Kills), g.Sum(x => x.Deaths)))
                .FirstOrDefaultAsync();

            if (stats is null || stats.Deaths is 0 || stats.Kills is 0)
            {
                var matchStats = gameEvent.Origin.GetAdditionalProperty<EFClientStatistics>("ClientStats");
                stats = new KdrStats(matchStats?.MatchData?.Kills ?? 0, matchStats?.MatchData?.Deaths ?? 0);
            }

            // Small constant to prevent divide by zero.
            var kdr = stats.Kills / (stats.Deaths + 0.00001f);
            var targetKdr = _voteConfig.VoteBanConfiguration.BadPlayerMinKdr;

            if (kdr < targetKdr)
            {
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledPoorPerformance
                    .FormatExt(kdr.ToString("N2"), targetKdr.ToString("N2")));
                return;
            }
        }

        if (gameEvent.Target.IsBot)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteBot);
            return;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.DenySelfTarget);
            return;
        }

        if (gameEvent.Target.Level > Data.Models.Client.EFClient.Permission.Flagged)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.CannotVoteRanked);
            return;
        }

        if (_voteConfig.VoteBanConfiguration.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
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
            case VoteResult.NotEnoughPlayers:
                gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
                break;
            case VoteResult.AbusiveVoter:
                gameEvent.Origin.Tell(_voteConfig.Translations.AbusiveVoter);
                break;
        }
    }
}
