using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;

namespace Votify.Commands;

public class VoteSkipCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteSkipProcessor _processor;

    public VoteSkipCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteSkipProcessor processor)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _processor = processor;
        Name = "voteskip";
        Description = "starts a vote to skip the map";
        Alias = "vs";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteSkipConfiguration.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Skip));
            return Task.CompletedTask;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType.Skip))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return Task.CompletedTask;
        }

        if (_voteConfig.VoteSkipConfiguration.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return Task.CompletedTask;
        }

        var vote = new VoteSkip
        {
            Initiator = gameEvent.Origin,
            Created = DateTimeOffset.UtcNow,
            Votes = new ConcurrentDictionary<EFClient, Vote>
            {
                [gameEvent.Origin] = Vote.Yes
            }
        };

        var result = _processor.CreateVote(gameEvent.Owner, vote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Owner.Broadcast(_voteConfig.Translations.SkipVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName));
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

        return Task.CompletedTask;
    }
}
