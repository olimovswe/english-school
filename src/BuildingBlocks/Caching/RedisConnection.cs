using StackExchange.Redis;

namespace EnglishSchool.BuildingBlocks.Caching;

/// <summary>
/// Valkey/Redis bilan bitta umumiy ulanish: L2 kesh ham, health check ham shundan foydalanadi. Server ishlamasa
/// ham ilova ishga tushadi (<c>abortConnect=false</c>): ulanish fonda qayta tiklanadi, kesh esa shu vaqt faqat
/// jarayon xotirasida (L1) ishlaydi. Uzilish paytidagi buyruqlar navbatda kutmaydi (<c>FailFast</c>), aks holda har
/// bir keshlangan oʻqish timeout'gacha (5 s) osilib qolardi.
/// </summary>
internal sealed class RedisConnection(string configuration) : IAsyncDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer>> _connection = new(() => ConnectAsync(configuration));

    public Task<IConnectionMultiplexer> GetAsync() => _connection.Value;

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated && _connection.Value.IsCompletedSuccessfully)
        {
            await _connection.Value.Result.DisposeAsync();
        }
    }

    private static async Task<IConnectionMultiplexer> ConnectAsync(string configuration)
    {
        var options = ConfigurationOptions.Parse(configuration);
        options.AbortOnConnectFail = false;
        options.BacklogPolicy = BacklogPolicy.FailFast;
        return await ConnectionMultiplexer.ConnectAsync(options);
    }
}
