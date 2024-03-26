using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteMapHandler : VoteHandler<VoteMap>
{
    private readonly ConfigurationBase _configuration;
    private readonly ILogger<VoteMapHandler> _logger;

    public VoteMapHandler(VoteMapProcessor voteKickProcessor, ConfigurationBase configuration, ILogger<VoteMapHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        voteKickProcessor.VoteFailed += OnVoteFailed;
        voteKickProcessor.VoteSucceeded += OnVoteSucceeded;
        voteKickProcessor.VoteNotification += OnVoteNotification;
        voteKickProcessor.VoteCancelled += OnVoteCancellation;
    }

    protected override async void OnVoteSucceeded(Server server, VoteMap vote)
    {
        try
        {
            var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
            var votePassedMessage = _configuration.Translations.VotePassed
                .FormatExt(_configuration.Translations.Map, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, vote.Map.Alias);

            server.Broadcast(votePassedMessage);
            await Task.Delay(5_000);
            await server.LoadMap(vote.Map.Name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote kick");
        }
    }

    protected override void OnVoteFailed(Server server, VoteMap vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (_configuration.VoteMapConfiguration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_configuration.Translations.NotEnoughYesVotes.FormatExt(_configuration.Translations.Map));
            return;
        }

        server.Broadcast(_configuration.Translations.NotEnoughVotes.FormatExt(_configuration.Translations.Map));
    }

    protected override void OnVoteNotification(Server server, VoteMap vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;

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
        }
    }
}
