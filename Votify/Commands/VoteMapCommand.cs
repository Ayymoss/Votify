using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Database.Models;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models.VoteModel;
using Votify.Processors;

namespace Votify.Commands;

public class VoteMapCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteMapProcessor _processor;

    public VoteMapCommand(CommandConfiguration config, ITranslationLookup translationLookup, ConfigurationBase voteConfig,
        VoteMapProcessor processor)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _processor = processor;
        Name = "votemap";
        Description = "starts a vote to change the map";
        Alias = "vm";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
        Arguments = new[]
        {
            new CommandArgument
            {
                Name = translationLookup["COMMANDS_ARGS_MAP"],
                Required = true
            }
        };
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        if (!_voteConfig.VoteMapConfiguration.IsEnabled)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabled.FormatExt(VoteType.Map));
            return Task.CompletedTask;
        }

        if (_voteConfig.DisabledServers.TryGetValue(gameEvent.Owner.Id, out var voteType) && voteType.Contains(VoteType.Map))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.VoteDisabledServer);
            return Task.CompletedTask;
        }

        if (_voteConfig.VoteMapConfiguration.MinimumPlayersRequired > gameEvent.Owner.ConnectedClients.Count)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NotEnoughPlayers);
            return Task.CompletedTask;
        }

        var input = gameEvent.Data.Trim();
        var map = gameEvent.Owner.Maps.FirstOrDefault(m =>
            m.Name.Equals(input, StringComparison.InvariantCultureIgnoreCase) ||
            m.Alias.Equals(input, StringComparison.InvariantCultureIgnoreCase));

        if (map is null)
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.MapNotFound);
            return Task.CompletedTask;
        }

        var vote = new VoteMap
        {
            Initiator = gameEvent.Origin,
            Created = DateTimeOffset.UtcNow,
            Votes = new ConcurrentDictionary<EFClient, Vote>
            {
                [gameEvent.Origin] = Vote.Yes
            },
            Map = map
        };

        var result = _processor.CreateVote(gameEvent.Owner, vote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Owner.Broadcast(_voteConfig.Translations.MapVoteStarted
                    .FormatExt(gameEvent.Origin.CleanedName, map.Alias));
                break;
            case VoteResult.VoteInProgress:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteInProgress);
                break;
            case VoteResult.VoteCooldown:
                gameEvent.Origin.Tell(_voteConfig.Translations.TooRecentVote);
                break;
        }

        return Task.CompletedTask;
    }
}
