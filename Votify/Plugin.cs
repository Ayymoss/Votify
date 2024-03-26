using Microsoft.Extensions.DependencyInjection;
using SharedLibraryCore;
using SharedLibraryCore.Events.Management;
using SharedLibraryCore.Interfaces;
using SharedLibraryCore.Interfaces.Events;
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
    public string Name => "Votify";
    public string Version => "2024-03-26";
    public string Author => "Amos";

    public Plugin(ConfigurationBase configuration, VoteState voteState, IServiceProvider serviceProvider)
    {
        _voteState = voteState;
        if (!configuration.IsEnabled) return;

        // Register handler subscriptions
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

    private Task OnLoad(IManager _, CancellationToken __)
    {
        Console.WriteLine($"[{Name}] loaded. Version: {Version}");
        return Task.CompletedTask;
    }

    private Task OnClientDispose(ClientStateDisposeEvent clientEvent, CancellationToken token)
    {
        if (!_voteState.Votes.TryGetValue(clientEvent.Client.CurrentServer, out var voteBase)) return Task.CompletedTask;
        voteBase.Item2.RemoveClient(clientEvent.Client);
        return Task.CompletedTask;
    }
}
