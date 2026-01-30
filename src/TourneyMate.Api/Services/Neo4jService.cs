using Neo4jClient;
using TourneyMate.Api.Configurations;

namespace TourneyMate.Api.Services;

public sealed class Neo4jService
{
    private readonly IGraphClient _client;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _connected;

    public Neo4jService(Neo4jConfiguration cfg)
    {
        cfg.Validate();
        _client = new BoltGraphClient(new Uri(cfg.Uri!), cfg.Username, cfg.Password);
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connected) return;

        await _lock.WaitAsync();
        try
        {
            if (_connected) return;
            await _client.ConnectAsync();
            _connected = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IGraphClient> ClientAsync()
    {
        await EnsureConnectedAsync();
        return _client;
    }
}
