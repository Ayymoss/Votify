using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Services;

namespace Votify.Commands;

public class YesCommand(CommandConfiguration config, ITranslationLookup translationLookup,
    ConfigurationBase voteConfig, VoteState voteState)
    : VoteCastCommand(config, translationLookup, voteConfig, voteState,
        "yes", "vote yes on the current vote", "y", Vote.Yes, voteConfig.Translations.VoteYes);
