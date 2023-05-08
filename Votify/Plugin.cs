using Microsoft.Extensions.DependencyInjection;
using SharedLibraryCore;
using SharedLibraryCore.Events.Management;
using SharedLibraryCore.Interfaces;
using SharedLibraryCore.Interfaces.Events;

namespace Votify;

public class Plugin : IPluginV2
{
    private readonly VoteConfiguration _voteConfig;
    private readonly VoteManager _voteManager;
    public string Name => "Votify";
    public string Version => "2023-05-09";
    public string Author => "Amos";

    public Plugin(VoteConfiguration voteConfig, VoteManager voteManager)
    {
        _voteConfig = voteConfig;
        _voteManager = voteManager;
        if (!_voteConfig.IsEnabled) return;

        IManagementEventSubscriptions.Load += OnLoad;
        IManagementEventSubscriptions.ClientStateDisposed += OnClientStateDisposed;
    }

    public static void RegisterDependencies(IServiceCollection serviceCollection)
    {
        serviceCollection.AddConfiguration("VotifySettings", new VoteConfiguration());
        serviceCollection.AddSingleton<VoteManager>();
    }

    private Task OnClientStateDisposed(ClientStateDisposeEvent clientEvent, CancellationToken token)
    {
        _voteManager.HandleDisconnect(clientEvent.Client.CurrentServer, clientEvent.Client);
        return Task.CompletedTask;
    }

    private Task OnLoad(IManager manager, CancellationToken token)
    {
        Console.WriteLine($"[{Name}] loaded. Version: {Version}");
        Utilities.ExecuteAfterDelay(_voteConfig.TimeBetweenVoteReminders, ExecuteAfterDelayCompleted, token);
        return Task.CompletedTask;
    }

    private async Task ExecuteAfterDelayCompleted(CancellationToken token)
    {
        await _voteManager.OnNotify();
        Utilities.ExecuteAfterDelay(_voteConfig.TimeBetweenVoteReminders, ExecuteAfterDelayCompleted, token);
    }
}
