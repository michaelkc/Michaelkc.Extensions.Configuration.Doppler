using Microsoft.Extensions.Configuration;
using ConfigurationProviderBase = Microsoft.Extensions.Configuration.ConfigurationProvider;

namespace DopplerSDK.ConfigurationProvider;

public sealed class DopplerConfigurationProvider : ConfigurationProviderBase, IDisposable
{
    private readonly DopplerConfigurationSource _source;
    private readonly DopplerClient _dopplerClient;
    private readonly bool _disposeDopplerClient;

    private Timer? _reloadTimer;
    private int _isReloading;
    private bool _disposed;

    internal DopplerConfigurationProvider(
        DopplerConfigurationSource source,
        DopplerClient dopplerClient,
        bool disposeDopplerClient)
    {
        _source = source;
        _dopplerClient = dopplerClient;
        _disposeDopplerClient = disposeDopplerClient;
    }

    public override void Load()
    {
        var loadedData = LoadData(throwOnFailure: _source.FailFast);
        if (loadedData is not null) Data = loadedData;

        StartReloadTimerIfNeeded();
    }

    private Dictionary<string, string?>? LoadData(bool throwOnFailure)
    {
        var response = _dopplerClient.FetchSecretsAsync().GetAwaiter().GetResult();
        if (response.IsSuccess)
        {
            return response.Secrets.ToDictionary(
                static kvp => kvp.Key,
                static kvp => (string?)kvp.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        if (throwOnFailure)
            throw new InvalidOperationException($"Doppler configuration load failed: {response.StatusMessage}");

        return null;
    }

    private void StartReloadTimerIfNeeded()
    {
        if (_reloadTimer is not null || _source.ReloadInterval is null) return;

        var reloadInterval = _source.ReloadInterval.Value;
        _reloadTimer = new Timer(
            ReloadFromTimer,
            state: null,
            dueTime: reloadInterval,
            period: reloadInterval);
    }

    private void ReloadFromTimer(object? state)
    {
        if (Interlocked.Exchange(ref _isReloading, 1) == 1) return;

        try
        {
            var loadedData = LoadData(throwOnFailure: false);
            if (loadedData is null) return;

            Data = loadedData;
            OnReload();
        }
        finally
        {
            Interlocked.Exchange(ref _isReloading, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _reloadTimer?.Dispose();
        if (_disposeDopplerClient) _dopplerClient.Dispose();

        _disposed = true;
    }
}
