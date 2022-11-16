using Microsoft.Extensions.DependencyInjection;
using SharedLibraryCore.Interfaces;

namespace VoteManager;

public class Configuration
{
    private readonly IConfigurationHandler<ConfigurationModel> _configurationHandler;

    public Configuration(IServiceProvider serviceProvider)
    {
        _configurationHandler = serviceProvider.GetRequiredService<IConfigurationHandlerFactory>()
            .GetConfigurationHandler<ConfigurationModel>("VoteManagerSettings");
    }

    public async Task<ConfigurationModel> Load()
    {
        await _configurationHandler.BuildAsync();
        if (_configurationHandler.Configuration() == null)
        {
            Console.WriteLine($"[{Plugin.PluginName}] Configuration not found, creating.");
            _configurationHandler.Set(new ConfigurationModel());
        }
        
        await _configurationHandler.Save();

        return _configurationHandler.Configuration();
    }
}

public class ConfigurationModel : IBaseConfiguration
{
    public bool IsEnabled { get; set; } = true;
    public int PercentageVotePassed { get; set; } = 50;
    public int MinimumPlayersRequired { get; set; } = 4;
    public int MinimumPlayersRequiredForSuccessfulVote { get; set; } = 2;
    public bool IsVoteReasonRequired { get; set; } = true;
    public int VoteDuration { get; set; } = 30;
    public int VoteCooldown { get; set; } = 60;
    public int TimeBetweenVoteReminders { get; set; } = 10;
    public VoteTypeConfiguration IsVoteTypeEnabled { get; set; } = new();
    public VoteMessages VoteMessages { get; set; } = new();
    
    public string Name() => "VoteManager";
    public IBaseConfiguration Generate() => new ConfigurationModel();
}

public class VoteTypeConfiguration
{
    public bool VoteBan { get; set; } = true;
    public bool VoteKick { get; set; } = true;
    public bool VoteMap { get; set; } = true;
    public bool VoteSkip { get; set; } = true;
}

public class VoteMessages
{
    public string? NotEnoughVotes { get; set; } = "VOTE: {type} on {target} failed, not enough votes";
    public string? NotEnoughYesVotes { get; set; } = "VOTE: {type} on {target} failed, not enough yes votes";
    public string? OpenVoteAutoMessage { get; set; } = "VOTE: There is an ongoing {type} vote. Type (Color::Green)!y (Color::White)or (Color::Red)!n (Color::White)to vote";
}
