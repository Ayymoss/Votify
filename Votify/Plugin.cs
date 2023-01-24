using SharedLibraryCore;
using SharedLibraryCore.Interfaces;

namespace Votify;

public class Plugin : IPlugin
{
    private readonly IConfigurationHandler<ConfigurationModel> _configurationHandler;
    public static ConfigurationModel Configuration = null!;
    public static readonly Votify Votify = new();

    private const string PluginName = "Votify";
    public string Name => PluginName;
    public float Version => 20221124f;
    public string Author => "Amos";

    public Plugin(IConfigurationHandlerFactory configurationHandlerFactory)
    {
        _configurationHandler = configurationHandlerFactory.GetConfigurationHandler<ConfigurationModel>("VotifySettings");
    }

    public async Task OnEventAsync(GameEvent gameEvent, Server server)
    {
        if (!Configuration.IsEnabled) return;

        switch (gameEvent.Type)
        {
            case GameEvent.EventType.Disconnect:
                Votify.HandleDisconnect(server, gameEvent.Origin);
                break;
            case GameEvent.EventType.Update:
                await Votify.OnUpdate();
                break;
        }
    }

    public async Task OnLoadAsync(IManager manager)
    {
        await _configurationHandler.BuildAsync();
        if (_configurationHandler.Configuration() == null)
        {
            Console.WriteLine($"[{PluginName}] Configuration not found, creating.");
            _configurationHandler.Set(new ConfigurationModel());
        }

        await _configurationHandler.Save();

        Configuration = _configurationHandler.Configuration();
        Console.WriteLine($"[{PluginName}] loaded. Version: {Version}");
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
