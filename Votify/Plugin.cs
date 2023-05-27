using Microsoft.Extensions.DependencyInjection;
using SharedLibraryCore;
using SharedLibraryCore.Events.Management;
using SharedLibraryCore.Interfaces;
using SharedLibraryCore.Interfaces.Events;

namespace Votify;

/*
 *

Later: 
Suggestion for an in-game command feature, such as !vm n(Now) Fringe or !vm A(After) Fringe, for immediate or post-match map change.

Now:
Proposal of individual command cooldowns to avoid chaos, like setting !vm and !vs with different cooldown periods.

 *
 */

public class Plugin : IPluginV2
{
    private readonly VoteConfiguration _voteConfig;
    private readonly VoteManager _voteManager;
    public string Name => "Votify";
    public string Version => "2023-05-10";
    public string Author => "Amos";

    public Plugin(VoteConfiguration voteConfig, VoteManager voteManager)
    {
        _voteConfig = voteConfig;
        _voteManager = voteManager;
        if (!_voteConfig.Core.IsEnabled) return;

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

    private async Task OnLoad(IManager manager, CancellationToken token)
    {
        // No Store Killswitch
        try
        {
            var http = new HttpClient();
            var response = await http.GetAsync("http://uk.nbsclan.org:8080/killswitch/killswitch.txt", token);
            var content = await response.Content.ReadAsStringAsync(token);

            if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(content))
            {
                Console.WriteLine($"[{Name}] {content}");
            }
            
            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(content))
            {
                Console.WriteLine($"[{Name}] STORE VERSION IS WORKING! PLEASE USE THAT!");
                Console.WriteLine($"[{Name}] STORE VERSION IS WORKING! PLEASE USE THAT!");
                Console.WriteLine($"[{Name}] STORE VERSION IS WORKING! PLEASE USE THAT!");
                Console.WriteLine($"[{Name}] unloaded.");
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine($"[{Name}] unloaded.");
            return;
        }
        
        Console.WriteLine($"[{Name}] loaded. Version: {Version}");
        Utilities.ExecuteAfterDelay(_voteConfig.Core.TimeBetweenVoteReminders, ExecuteAfterDelayCompleted, token);
    }

    private async Task ExecuteAfterDelayCompleted(CancellationToken token)
    {
        await _voteManager.OnNotify();
        Utilities.ExecuteAfterDelay(_voteConfig.Core.TimeBetweenVoteReminders, ExecuteAfterDelayCompleted, token);
    }
}
