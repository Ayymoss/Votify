using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteQueueMapHandler : VoteHandler<VoteQueueMap>
{
    private readonly ConfigurationBase _configuration;
    private readonly ILogger<VoteQueueMapHandler> _logger;

    public VoteQueueMapHandler(VoteQueueMapProcessor processor, ConfigurationBase configuration, ILogger<VoteQueueMapHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        processor.VoteFailed += OnVoteFailed;
        processor.VoteSucceeded += OnVoteSucceeded;
        processor.VoteNotification += OnVoteNotification;
        processor.VoteCancelled += OnVoteCancellation;
    }

    protected override void OnVoteSucceeded(Server server, VoteQueueMap vote)
    {
        try
        {
            var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
            var votePassedMessage = _configuration.Translations.VoteQueued.FormatExt(
                _configuration.Translations.Map,
                vote.YesVotes,
                Math.Max(0, abstains),
                vote.NoVotes,
                vote.Map.Alias);
            server.Broadcast(votePassedMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote kick");
        }
    }

    protected override void OnVoteFailed(Server server, VoteQueueMap vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (_configuration.VoteQueueMapConfiguration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_configuration.Translations.NotEnoughYesVotes.FormatExt(_configuration.Translations.Map));
            return;
        }

        server.Broadcast(_configuration.Translations.NotEnoughVotes.FormatExt(_configuration.Translations.Map));
    }

    protected override void OnVoteNotification(Server server, VoteQueueMap vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;

        var message = _configuration.Translations.QueueVoteStarted.FormatExt(
            ("origin", vote.Origin.CleanedName),
            ("mapName", vote.Map.Alias));

        server.Broadcast(message);
        server.Broadcast(_configuration.Translations.OpenVoteAutoMessage.FormatExt(_configuration.Translations.Map, vote.YesVotes,
            Math.Max(0, abstains), vote.NoVotes, vote.Map.Alias));
    }

    protected override void OnVoteCancellation(Server server, VoteCancellation reason, string message)
    {
        switch (reason)
        {
            case VoteCancellation.Admin:
                server.Broadcast(message);
                break;
            case VoteCancellation.Other:
                server.Broadcast(_configuration.Translations.VoteCancelled);
                break;
        }
    }
}
