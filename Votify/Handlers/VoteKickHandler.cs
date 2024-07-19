using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify.Handlers;

public class VoteKickHandler : VoteHandler<VoteKick>
{
    private readonly ConfigurationBase _configuration;
    private readonly ILogger<VoteKickHandler> _logger;

    public VoteKickHandler(VoteKickProcessor voteKickProcessor, ConfigurationBase configuration, ILogger<VoteKickHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
        voteKickProcessor.VoteFailed += OnVoteFailed;
        voteKickProcessor.VoteSucceeded += OnVoteSucceeded;
        voteKickProcessor.VoteNotification += OnVoteNotification;
        voteKickProcessor.VoteCancelled += OnVoteCancellation;
    }

    protected override async void OnVoteSucceeded(Server server, VoteKick vote)
    {
        try
        {
            var voteActionMessage = _configuration.Translations.VoteAction.FormatExt(vote.Reason);
            var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
            var votePassedMessage = _configuration.Translations.VotePassed
                .FormatExt(_configuration.Translations.Kick, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, vote.Target.CleanedName);

            server.Broadcast(votePassedMessage);
            await server.Kick(voteActionMessage, vote.Target, vote.Initiator);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote kick");
        }
    }

    protected override void OnVoteFailed(Server server, VoteKick vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (_configuration.VoteKickConfiguration.VotePassPercentage > votePercentage)
        {
            server.Broadcast(_configuration.Translations.NotEnoughYesVotes.FormatExt(_configuration.Translations.Kick));
            return;
        }

        server.Broadcast(_configuration.Translations.NotEnoughVotes.FormatExt(_configuration.Translations.Kick));
    }

    protected override void OnVoteNotification(Server server, VoteKick vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;

        server.Broadcast(_configuration.Translations.OpenVoteAutoMessage.FormatExt(_configuration.Translations.Kick, vote.YesVotes,
            Math.Max(0, abstains), vote.NoVotes, vote.Target.CleanedName));
    }

    protected override void OnVoteCancellation(Server server, VoteCancellation reason, string message)
    {
        switch (reason)
        {
            case VoteCancellation.Disconnect:
                server.Broadcast(message.FormatExt(_configuration.Translations.Kick));
                break;
            case VoteCancellation.Admin:
                server.Broadcast(message);
                break;
        }
    }
}
