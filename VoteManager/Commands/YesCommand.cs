﻿using Data.Models.Client;
using SharedLibraryCore;
using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;

namespace VoteManager.Commands;

public class YesCommand : Command
{
    public YesCommand(CommandConfiguration config, ITranslationLookup translationLookup) : base(config,
        translationLookup)
    {
        Name = "yes";
        Description = "vote yes on the current vote";
        Alias = "y";
        Permission = EFClient.Permission.User;
        RequiresTarget = false;
    }

    public override async Task ExecuteAsync(GameEvent gameEvent)
    {
        if (Plugin.VoteManager.InProgressVote(gameEvent.Owner))
        {
            var result = Plugin.VoteManager.CastVote(gameEvent.Owner, gameEvent.Origin, Vote.Yes);
            switch (result)
            {
                case VoteResult.Success:
                    gameEvent.Origin.Tell(Plugin.Configuration.Translations.VoteSuccess);
                    break;
                case VoteResult.NoVoteInProgress:
                    gameEvent.Origin.Tell(Plugin.Configuration.Translations.NoVoteInProgress);
                    break;
                case VoteResult.AlreadyVoted:
                    gameEvent.Origin.Tell(Plugin.Configuration.Translations.AlreadyVoted);
                    break;
            }
        }
        else
        {
            gameEvent.Origin.Tell(Plugin.Configuration.Translations.NoVoteInProgress);
        }
    }
}
