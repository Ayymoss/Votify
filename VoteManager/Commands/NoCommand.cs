﻿using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class NoCommand : Command
{
    private readonly ConfigurationModel _configuration;

    public NoCommand(CommandConfiguration config, ITranslationLookup translationLookup,
        IConfigurationHandler<ConfigurationModel> configurationHandler) : base(config,
        translationLookup)
    {
        Name = "no";
        Description = "vote no on the current vote";
        Alias = "n";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
        _configuration = configurationHandler.Configuration();
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (Plugin.VoteManager.InProgressVote(gameEvent.Owner))
        {
            var result = Plugin.VoteManager.CastVote(gameEvent.Owner, gameEvent.Origin, Vote.No);
            switch (result)
            {
                case VoteResult.Success:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.VoteSuccess);
                    break;
                case VoteResult.NoVoteInProgress:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.NoVoteInProgress);
                    break;
                case VoteResult.AlreadyVoted:
                    gameEvent.Origin.Tell(_configuration.VoteMessages.AlreadyVoted);
                    break;
            }
        }
        else
        {
            gameEvent.Origin.Tell("There is no vote in progress");
        }
    }
}