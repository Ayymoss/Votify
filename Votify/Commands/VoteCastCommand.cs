using SharedLibraryCore;
using SharedLibraryCore.Commands;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models;
using Votify.Services;

namespace Votify.Commands;

public abstract class VoteCastCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteState _voteState;
    private readonly Vote _voteDirection;
    private readonly string _voteTranslation;

    protected VoteCastCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        ConfigurationBase voteConfig, VoteState voteState,
        string name, string description, string alias, Vote voteDirection, string voteTranslation)
        : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _voteState = voteState;
        _voteDirection = voteDirection;
        _voteTranslation = voteTranslation;
        Name = name;
        Description = description;
        Alias = alias;
        Permission = Data.Models.Client.EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        var userVote = new UserVote
        {
            Server = gameEvent.Owner,
            Client = gameEvent.Origin,
            Vote = _voteDirection
        };

        if (!_voteState.Votes.TryGetValue(gameEvent.Owner, out var voteBase))
        {
            gameEvent.Origin.Tell(_voteConfig.Translations.NoVoteInProgress);
            return Task.CompletedTask;
        }

        var result = voteBase.Item2.RegisterUserVote(userVote);

        switch (result)
        {
            case VoteResult.Success:
                gameEvent.Origin.Tell(_voteConfig.Translations.VoteSuccess
                    .FormatExt(_voteTranslation));
                break;
            case VoteResult.AlreadyVoted:
                gameEvent.Origin.Tell(_voteConfig.Translations.AlreadyVoted);
                break;
        }

        return Task.CompletedTask;
    }
}
