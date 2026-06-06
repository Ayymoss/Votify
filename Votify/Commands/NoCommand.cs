using SharedLibraryCore.Configuration;
using SharedLibraryCore.Interfaces;
using Votify.Configuration;
using Votify.Enums;
using Votify.Services;

namespace Votify.Commands;

public class NoCommand(CommandConfiguration config, ITranslationLookup translationLookup,
    ConfigurationBase voteConfig, VoteState voteState)
    : VoteCastCommand(config, translationLookup, voteConfig, voteState,
        "no", "vote no on the current vote", "n", Vote.No, voteConfig.Translations.VoteNo);
