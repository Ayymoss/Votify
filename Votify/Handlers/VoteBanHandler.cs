using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteBanHandler : VoteHandler<VoteBan>
{
    private readonly ConfigurationBase _configuration;
    private readonly ILogger<VoteBanHandler> _logger;

    public VoteBanHandler(VoteBanProcessor voteBanProcessor, ConfigurationBase configuration, ILogger<VoteBanHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        voteBanProcessor.VoteFailed += OnVoteFailed;
        voteBanProcessor.VoteSucceeded += OnVoteSucceeded;
        voteBanProcessor.VoteNotification += OnVoteNotification;
        voteBanProcessor.VoteCancelled += OnVoteCancellation;
    }

    protected override async void OnVoteSucceeded(Server server, VoteBan vote)
    {
        try
        {
            var voteActionMessage = _configuration.Translations.VoteAction
                .FormatExt(vote.Initiator.CleanedName, vote.Initiator.ClientId, vote.Reason);
            var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
            var votePassedMessage = _configuration.Translations.VotePassed
                .FormatExt(_configuration.Translations.Ban, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, vote.Target.CleanedName);

            server.Broadcast(votePassedMessage);
            await server.TempBan(voteActionMessage, _configuration.VoteBanConfiguration.VoteBanDuration, vote.Target, vote.Initiator);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote ban");
        }
    }

    protected override void OnVoteFailed(Server server, VoteBan vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (_configuration.VoteBanConfiguration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_configuration.Translations.NotEnoughYesVotes.FormatExt(_configuration.Translations.Ban));
            return;
        }

        server.Broadcast(_configuration.Translations.NotEnoughVotes.FormatExt(_configuration.Translations.Ban));
    }

    protected override void OnVoteNotification(Server server, VoteBan vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;

        server.Broadcast(_configuration.Translations.OpenVoteAutoMessage.FormatExt(_configuration.Translations.Ban, vote.YesVotes,
            Math.Max(0, abstains), vote.NoVotes, vote.Target.CleanedName));
    }

    protected override void OnVoteCancellation(Server server, VoteCancellation reason, string message)
    {
        switch (reason)
        {
            case VoteCancellation.Disconnect:
                server.Broadcast(message.FormatExt(_configuration.Translations.Ban));
                break;
            case VoteCancellation.Admin:
                server.Broadcast(message);
                break;
        }
    }
}
