using Data.Abstractions;
using Data.Models.Client.Stats;
using Microsoft.EntityFrameworkCore;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Interfaces;
using Votify.Models;
using Votify.Services;

namespace Votify.Commands;

// Shared execution logic for targeted (player) votes. Deliberately NOT a Command:
// IW4MAdmin's plugin discovery registers and instantiates every class whose BaseType
// is Command, so an abstract/generic Command base would crash host startup. Concrete
// commands derive from Command directly and delegate here.
public sealed class TargetedVoteRunner(
    ConfigurationBase voteConfig,
    IVoteProcessor processor,
    IDatabaseContextFactory contextFactory,
    MetaManager metaManager)
{
    public async Task ExecuteAsync(GameEvent gameEvent, VoteType voteType, VoteConfigurationBase voteTypeConfig,
        Func<GameEvent, VoteBase> createVote, Func<GameEvent, string> getSuccessMessage)
    {
        if (!voteTypeConfig.IsEnabled)
        {
            gameEvent.Origin.Tell(voteConfig.Translations.VoteDisabled.FormatExt(voteType));
            return;
        }

        if (await metaManager.IsUserVoteBlockedAsync(gameEvent.Origin.ClientId))
        {
            gameEvent.Origin.Tell(voteConfig.Translations.VoteBlocked);
            return;
        }

        if (voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var disabled) && disabled.Contains(voteType))
        {
            gameEvent.Origin.Tell(voteConfig.Translations.VoteDisabledServer);
            return;
        }

        if (voteTypeConfig is IKdrVoteConfiguration { CanBadPlayersVote: false } kdrConfig &&
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
                gameEvent.Origin.Tell(voteConfig.Translations.VoteDisabledPoorPerformance
                    .FormatExt(kdr.ToString("N2"), targetKdr.ToString("N2")));
                return;
            }
        }

        if (gameEvent.Target.IsBot)
        {
            gameEvent.Origin.Tell(voteConfig.Translations.CannotVoteBot);
            return;
        }

        if (gameEvent.Origin.ClientId == gameEvent.Target.ClientId)
        {
            gameEvent.Origin.Tell(voteConfig.Translations.DenySelfTarget);
            return;
        }

        if (gameEvent.Target.Level > Data.Models.Client.EFClient.Permission.Flagged)
        {
            gameEvent.Origin.Tell(voteConfig.Translations.CannotVoteRanked);
            return;
        }

        if (voteTypeConfig.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(voteConfig.Translations.NotEnoughPlayers);
            return;
        }

        var vote = createVote(gameEvent);
        var result = processor.CreateVote(gameEvent.Owner, vote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Owner.Broadcast(getSuccessMessage(gameEvent));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(voteConfig.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(voteConfig.Translations.TooRecentVote);
                break;
            case VoteResult.NotEnoughPlayers:
                gameEvent.Origin.Tell(voteConfig.Translations.NotEnoughPlayers);
                break;
            case VoteResult.AbusiveVoter:
                gameEvent.Origin.Tell(voteConfig.Translations.AbusiveVoter);
                break;
        }
    }
}
