using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Models;
using Votify.Services;

namespace Votify.Commands;

public class NoCommand : Command
{
    private readonly ConfigurationBase _voteConfig;
    private readonly VoteState _voteState;

    public NoCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        ConfigurationBase voteConfig, VoteState voteState) : base(config, translationLookup)
    {
        _voteConfig = voteConfig;
        _voteState = voteState;
        Name = "no";
        Description = "vote no on the current vote";
        Alias = "n";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override Task ExecuteAsync(GameEvent gameEvent)
    {
        var userVote = new UserVote
        {
            Server = gameEvent.Owner,
            Client = gameEvent.Origin,
            Vote = Vote.No
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
                    .FormatExt(_voteConfig.Translations.VoteNo));
                break;
            case VoteResult.AlreadyVoted:
                gameEvent.Origin.Tell(_voteConfig.Translations.AlreadyVoted);
                break;
        }

        return Task.CompletedTask;
    }
}
