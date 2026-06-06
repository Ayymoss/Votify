using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteBanHandler(VoteBanProcessor processor, ConfigurationBase configuration, ILogger<VoteBanHandler> logger)
    : VoteHandler<VoteBan>(processor, configuration, logger)
{
    protected override string VoteTypeName => Configuration.Translations.Ban;
    protected override VoteConfigurationBase VoteTypeConfig => Configuration.VoteBanConfiguration;
    protected override string GetTargetDisplayName(VoteBan vote) => vote.Target.CleanedName;

    protected override string GetPassedMessage(Server server, VoteBan vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        var duration = Configuration.VoteBanConfiguration.VoteBanDuration;
        var durationText = duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes}m"
            : $"{(int)duration.TotalMinutes}m";

        return Configuration.Translations.VoteBanPassed
            .FormatExt(VoteTypeName, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, vote.Target.CleanedName,
                durationText);
    }

    protected override async Task ExecuteVoteAction(Server server, VoteBan vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        var voteActionMessage = Configuration.Translations.VoteAction
            .FormatExt(vote.Reason, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes);
        await server.TempBan(voteActionMessage, Configuration.VoteBanConfiguration.VoteBanDuration, vote.Target, vote.Initiator);
    }
}
