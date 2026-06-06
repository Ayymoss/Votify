using System.Collections.Concurrent;
using Data.Abstractions;
using Data.Models.Client.Stats;
using Microsoft.EntityFrameworkCore;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Interfaces;
using Votify.Models;
using Votify.Services;

namespace Votify.Commands;

public abstract class TargetedVoteCommand<TVote>(
    CommandConfiguration config,
    ITranslationLookup translationLookup,
    ConfigurationBase voteConfig,
    IVoteProcessor processor,
    IDatabaseContextFactory contextFactory,
    MetaManager metaManager)
    : Command(config, translationLookup)
    where TVote : VoteBase
{
    protected readonly ConfigurationBase _voteConfig = voteConfig;

    protected abstract VoteType VoteType { get; }
    protected abstract VoteConfigurationBase VoteTypeConfig { get; }
    protected abstract TVote CreateVoteObject(GameEvent gameEvent);
    protected abstract string GetSuccessMessage(GameEvent gameEvent);

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!VoteTypeConfig.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType));
            return;
        }

        if (await metaManager.IsUserVoteBlockedAsync(gameEvent.Origin.ClientId))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteBlocked);
            return;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (VoteTypeConfig is IKdrVoteConfiguration { CanBadPlayersVote: false } kdrConfig &&
            !gameEvent.Owner.GametypeName.Contains("zom", StringComparison.CurrentCultureIgnoreCase))
        {
            var context = contextFactory.CreateContext(false);

            var dbStats = await context.ClientStatistics
                .Where(x => x.ClientId == gameEvent.Origin.ClientId)
                .GroupBy(x => x.ClientId)
                .Select(g => new { Kills = g.Sum(x => x.Kills), Deaths = g.Sum(x => x.Deaths) })
                .FirstOrDefaultAsync();

            var matchStats = gameEvent.Origin.GetAdditionalProperty<EFClientStatistics>("ClientStats");
            var stats = new KdrStats(dbStats?.Kills ?? 0 + matchStats?.MatchData?.Kills ?? 0,
                dbStats?.Deaths ?? 0 + matchStats?.MatchData?.Deaths ?? 0);

            var kdr = stats.Kills / (stats.Deaths + 0.00001f);
            var targetKdr = kdrConfig.BadPlayerMinKdr;

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

        if (VoteTypeConfig.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var vote = CreateVoteObject(gameEvent);
        var result = processor.CreateVote(gameEvent.Owner, vote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Owner.Broadcast(GetSuccessMessage(gameEvent));
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
