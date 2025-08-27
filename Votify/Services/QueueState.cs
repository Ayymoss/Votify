using System.Collections.Concurrent;
using SharedLibraryCore;
using SharedLibraryCore.RCon;

namespace Votify.Services;

public class QueueState
{
    private readonly ConcurrentDictionary<Server, Map> _queuedMaps = new();

    public void SetQueuedMap(Server server, Map map)
    {
        _queuedMaps[server] = map;
    }

    public Map? GetQueuedMap(Server server)
    {
        _queuedMaps.TryGetValue(server, out var map);
        return map;
    }

    public void ClearQueuedMap(Server server)
    {
        _queuedMaps.TryRemove(server, out _);
    }
}
