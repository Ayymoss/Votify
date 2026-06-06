using Microsoft.Extensions.DependencyInjection;
using SharedLibraryCore;
using SharedLibraryCore.Events.Management;
using SharedLibraryCore.Helpers;
using SharedLibraryCore.Interfaces;
using SharedLibraryCore.Interfaces.Events;
using Votify.Commands;
using Votify.Configuration;
using Votify.Handlers;
using Votify.Models.VoteModel;
using Votify.Processors;
using Votify.Services;

namespace Votify;

/*
 Later:
    // sv_mapRotationCurrent - for "after" map change fr
    Suggestion for an in-game command feature, such as !vm n(Now) Fringe or !vm A(After) Fringe, for immediate or post-match map change.
*/

public class Plugin : IPluginV2
{
    private readonly VoteState _voteState;
    private readonly IInteractionRegistration _interactionRegistration;
    private readonly MetaManager _metaManager;
    private readonly IRemoteCommandService _remoteCommandService;

    private const string VoteInteraction = "Webfront::Profile::VoteBlock";
    public const string BannedVoterKey = "VotifyBanState";

    public string Name => "Votify";
    public string Version => "2026-06-06";
    public string Author => "Amos";

    public Plugin(ConfigurationBase configuration, VoteState voteState, IServiceProvider serviceProvider,
        IInteractionRegistration interactionRegistration, MetaManager metaManager, IRemoteCommandService remoteCommandService)
    {
        _voteState = voteState;
        _interactionRegistration = interactionRegistration;
        _metaManager = metaManager;
        _remoteCommandService = remoteCommandService;
        if (!configuration.IsEnabled) return;

        // Call constructors
        serviceProvider.GetRequiredService<VoteHandler<VoteKick>>();
        serviceProvider.GetRequiredService<VoteHandler<VoteBan>>();
        serviceProvider.GetRequiredService<VoteHandler<VoteSkip>>();
        serviceProvider.GetRequiredService<VoteHandler<VoteMap>>();
        IManagementEventSubscriptions.Load += OnLoad;
        IManagementEventSubscriptions.ClientStateDisposed += OnClientDispose;
    }

    public static void RegisterDependencies(IServiceCollection serviceCollection)
    {
        serviceCollection.AddConfiguration("VotifySettingsV2", new ConfigurationBase());

        serviceCollection.AddSingleton<MetaManager>();
        serviceCollection.AddSingleton<VoteState>();

        serviceCollection.AddSingleton<VoteKickProcessor>();
        serviceCollection.AddSingleton<VoteBanProcessor>();
        serviceCollection.AddSingleton<VoteSkipProcessor>();
        serviceCollection.AddSingleton<VoteMapProcessor>();

        serviceCollection.AddSingleton<VoteHandler<VoteKick>, VoteKickHandler>();
        serviceCollection.AddSingleton<VoteHandler<VoteBan>, VoteBanHandler>();
        serviceCollection.AddSingleton<VoteHandler<VoteSkip>, VoteSkipHandler>();
        serviceCollection.AddSingleton<VoteHandler<VoteMap>, VoteMapHandler>();
    }

    private Task OnLoad(IManager manager, CancellationToken __)
    {
        _interactionRegistration.RegisterInteraction(VoteInteraction, async (targetClientId, game, _) =>
        {
            if (!targetClientId.HasValue)
            {
                return null;
            }

            var isUserVoteBlocked = await _metaManager.IsUserVoteBlockedAsync(targetClientId.Value);
            var server = manager.GetServers().First();

            return isUserVoteBlocked
                ? CreateVoteInteraction(targetClientId.Value, server, GetCommandName,
                    "Unblock from Voting", "ph-play", "Unblock", "Allow Access To Voting", typeof(VoteUnblockCommand))
                : CreateVoteInteraction(targetClientId.Value, server, GetCommandName,
                    "Block from Voting", "ph-pause", "Block", "Deny Access To Voting", typeof(VoteBlockCommand));

            string GetCommandName(Type commandType) =>
                manager.Commands.FirstOrDefault(command => command.GetType() == commandType)?.Name ?? string.Empty;
        });

        Console.WriteLine($"[{Name}] loaded. Version: {Version}");

        return Task.CompletedTask;
    }

    private Task OnClientDispose(ClientStateDisposeEvent clientEvent, CancellationToken token)
    {
        if (!_voteState.Votes.TryGetValue(clientEvent.Client.CurrentServer, out var voteBase)) return Task.CompletedTask;
        voteBase.Item2.RemoveClient(clientEvent.Client);
        return Task.CompletedTask;
    }

    private InteractionData CreateVoteInteraction(int targetClientId, Server server, Func<Type, string> getCommandNameFunc,
        string name, string icon, string buttonLabel, string displayName, Type commandType)
    {
        return new InteractionData
        {
            EntityId = targetClientId,
            Name = name,
            DisplayMeta = icon,
            ActionPath = "DynamicAction",
            ActionMeta = new Dictionary<string, string>
            {
                { "InteractionId", VoteInteraction },
                { "ActionButtonLabel", buttonLabel },
                { "Name", displayName },
                { "ShouldRefresh", true.ToString() }
            },
            MinimumPermission = Data.Models.Client.EFClient.Permission.Administrator,
            Source = Name,
            Action = async (originId, targetId, gameName, meta, cancellationToken) =>
            {
                if (!targetId.HasValue) return "No target client id specified";

                var commandResponse = await _remoteCommandService
                    .Execute(originId, targetId, getCommandNameFunc(commandType), null, server);
                return string.Join(".", commandResponse.Select(result => result.Response));
            }
        };
    }
}
