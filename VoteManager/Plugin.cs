﻿using SharedLibraryCore;
using SharedLibraryCore.Interfaces;

namespace VoteManager;

public class Plugin : IPlugin
{
    private readonly IConfigurationHandler<ConfigurationModel> _configurationHandler;
    public static ConfigurationModel Configuration = null!;
    public static readonly VoteManager VoteManager = new();

    private const string PluginName = "Vote Manager";
    public string Name => PluginName;
    public float Version => 20221116f;
    public string Author => "Amos";

    public Plugin(IConfigurationHandler<ConfigurationModel> configurationHandler)
    {
        _configurationHandler = configurationHandler;
    }

    public async Task OnEventAsync(GameEvent gameEvent, Server server)
    {
        if (!Configuration.IsEnabled) return;

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
        await _configurationHandler.BuildAsync();
        if (_configurationHandler.Configuration() == null)
        {
            Console.WriteLine($"[{PluginName}] Configuration not found, creating.");
            _configurationHandler.Set(new ConfigurationModel());
            await _configurationHandler.Save();
            await _configurationHandler.BuildAsync();
        }
        else
        {
            await _configurationHandler.Save();
        }

        Configuration = _configurationHandler.Configuration();
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
