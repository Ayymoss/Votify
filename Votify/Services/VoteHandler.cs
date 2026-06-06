using Microsoft.Extensions.Logging;
using SharedLibraryCore;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models;

namespace Votify.Services;

public abstract class VoteHandler<TVote> where TVote : VoteBase
{
    protected readonly ConfigurationBase Configuration;
    private readonly ILogger _logger;

    protected VoteHandler(VoteProcessor<TVote> processor, ConfigurationBase configuration, ILogger logger)
    {
        Configuration = configuration;
        _logger = logger;
        processor.VoteFailed += OnVoteFailed;
        processor.VoteSucceeded += OnVoteSucceeded;
        processor.VoteNotification += OnVoteNotification;
        processor.VoteCancelled += OnVoteCancellation;
    }

    protected abstract string VoteTypeName { get; }
    protected abstract VoteConfigurationBase VoteTypeConfig { get; }
    protected abstract string? GetTargetDisplayName(TVote vote);
    protected abstract Task ExecuteVoteAction(Server server, TVote vote);

    protected virtual async void OnVoteSucceeded(Server server, TVote vote)
    {
        try
        {
            server.Broadcast(GetPassedMessage(server, vote));
            await ExecuteVoteAction(server, vote);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process vote {VoteType}", VoteTypeName);
        }
    }

    protected virtual string GetPassedMessage(Server server, TVote vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        var targetName = GetTargetDisplayName(vote);
        return Configuration.Translations.VotePassed
            .FormatExt(VoteTypeName, vote.YesVotes, Math.Max(0, abstains), vote.NoVotes, targetName);
    }

    protected virtual void OnVoteFailed(Server server, TVote vote)
    {
        var votePercentage = (float)vote.YesVotes / (vote.YesVotes + vote.NoVotes);

        if (VoteTypeConfig.VotePassPercentage > votePercentage)
        {
            server.Broadcast(Configuration.Translations.NotEnoughYesVotes.FormatExt(VoteTypeName));
            return;
        }

        var connected = server.ConnectedClients.Count(x => !x.IsBot);
        var required = (int)Math.Ceiling(VoteTypeConfig.MinimumVotingPlayersPercentage * connected);
        var needed = Math.Max(1, required - (vote.YesVotes + vote.NoVotes));
        server.Broadcast(Configuration.Translations.NotEnoughVotes.FormatExt(VoteTypeName, needed));
    }

    protected virtual void OnVoteNotification(Server server, TVote vote)
    {
        var abstains = server.ConnectedClients.Count(x => !x.IsBot) - vote.Votes.Count;
        var targetName = GetTargetDisplayName(vote);
        var secondsLeft = Math.Max(0,
            (int)(Configuration.VoteDuration - (TimeProvider.System.GetUtcNow() - vote.Created)).TotalSeconds);

        if (targetName is not null)
        {
            server.Broadcast(Configuration.Translations.OpenVoteAutoMessage.FormatExt(VoteTypeName, vote.YesVotes,
                Math.Max(0, abstains), vote.NoVotes, targetName, secondsLeft));
        }
        else
        {
            server.Broadcast(Configuration.Translations.OpenVoteAutoMessageNoTarget.FormatExt(VoteTypeName, vote.YesVotes,
                Math.Max(0, abstains), vote.NoVotes, secondsLeft));
        }
    }

    protected virtual void OnVoteCancellation(Server server, VoteCancellation reason, string message)
    {
        switch (reason)
        {
            case VoteCancellation.Disconnect:
                server.Broadcast(message.FormatExt(VoteTypeName));
                break;
            case VoteCancellation.Admin:
                server.Broadcast(message);
                break;
        }
    }
}
