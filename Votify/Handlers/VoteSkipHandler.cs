using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteSkipHandler : VoteHandler<VoteSkip>
{
    private readonly ConfigurationBase _configuration;
    private readonly ILogger<VoteSkipHandler> _logger;

    public VoteSkipHandler(VoteSkipProcessor voteKickProcessor, ConfigurationBase configuration, ILogger<VoteSkipHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        voteKickProcessor.VoteFailed += OnVoteFailed;
        voteKickProcessor.VoteSucceeded += OnVoteSucceeded;
        voteKickProcessor.VoteNotification += OnVoteNotification;
        voteKickProcessor.VoteCancelled += OnVoteCancellation;
    }

    protected override async void OnVoteSucceeded(Server server, VoteSkip vote)
    {
        try
        {
            var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
            var votePassedMessage = _configuration.Translations.VotePassed
                .FormatExt(_configuration.Translations.Skip, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, _configuration.Translations.Skip);

            server.Broadcast(votePassedMessage);
            await Task.Delay(5_000);
            await server.ExecuteCommandAsync("map_rotate");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote kick");
        }
    }

    protected override void OnVoteFailed(Server server, VoteSkip vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (_configuration.VoteSkipConfiguration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_configuration.Translations.NotEnoughYesVotes.FormatExt(_configuration.Translations.Skip));
            return;
        }

        server.Broadcast(_configuration.Translations.NotEnoughVotes.FormatExt(_configuration.Translations.Skip));
    }

    protected override void OnVoteNotification(Server server, VoteSkip vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;

        server.Broadcast(_configuration.Translations.OpenVoteAutoMessageNoTarget.FormatExt(_configuration.Translations.Skip, vote.YesVotes,
            Math.Max(0, abstains), vote.NoVotes));
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
