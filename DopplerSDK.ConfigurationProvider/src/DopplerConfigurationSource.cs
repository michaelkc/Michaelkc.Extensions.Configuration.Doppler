using Microsoft.Extensions.Configuration;

namespace DopplerSDK.ConfigurationProvider;

public sealed class DopplerConfigurationSource : IConfigurationSource
{
    public DopplerClientConfiguration DopplerClientConfiguration { get; } = new();
    public bool FailFast { get; set; } = true;
    public TimeSpan? ReloadInterval { get; set; }

    internal Func<DopplerClientConfiguration, DopplerClient>? ClientFactory { get; set; }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        Validate();

        var clientConfiguration = CreateClientConfiguration();
        var client = ClientFactory?.Invoke(clientConfiguration) ?? new DopplerClient(clientConfiguration);
        return new DopplerConfigurationProvider(this, client, disposeDopplerClient: true);
    }

    internal DopplerClientConfiguration CreateClientConfiguration()
    {
        return new DopplerClientConfiguration
        {
            DopplerToken = DopplerClientConfiguration.DopplerToken,
            DopplerNameTransformer = DopplerClientConfiguration.DopplerNameTransformer,
            DopplerApiHost = DopplerClientConfiguration.DopplerApiHost,
            RequestTimeout = DopplerClientConfiguration.RequestTimeout
        };
    }

    private void Validate()
    {
        if (ReloadInterval is { } reloadInterval && reloadInterval <= TimeSpan.Zero)
            throw new InvalidOperationException("Doppler configuration error: ReloadInterval must be greater than zero.");

        if (DopplerClientConfiguration.RequestTimeout <= TimeSpan.Zero)
            throw new InvalidOperationException(
                "Doppler configuration error: DopplerClientConfiguration.RequestTimeout must be greater than zero.");
    }
}
