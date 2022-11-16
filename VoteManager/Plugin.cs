using SharedLibraryCore;
using SharedLibraryCore.Interfaces;

namespace VoteManager;

public class Plugin : IPlugin
{
    private readonly Configuration _configuration;
    private ConfigurationModel? _config;
    public VoteManager VoteManager;

    public const string PluginName = "Vote Manager";
    public string Name => PluginName;
    public float Version => 20221116f;
    public string Author => "Amos";

    public Plugin(IServiceProvider serviceProvider)
    {
        _configuration = new Configuration(serviceProvider);
        VoteManager = new VoteManager(_config!);
    }

    public async Task OnEventAsync(GameEvent gameEvent, Server server)
    {
        if (_config is null || !_config.IsEnabled) return;

        switch (gameEvent.Type)
        {
            case GameEvent.EventType.Disconnect:
                VoteManager.HandleDisconnect(server, gameEvent.Origin);
                break;
            case GameEvent.EventType.Update:
                await VoteManager.OnUpdate();
                break;
        }
    }

    public async Task OnLoadAsync(IManager manager)
    {
        _config = await _configuration.Load();
    }

    public Task OnUnloadAsync()
    {
        return Task.CompletedTask;
    }

    public Task OnTickAsync(Server server)
    {
        return Task.CompletedTask;
    }
}
