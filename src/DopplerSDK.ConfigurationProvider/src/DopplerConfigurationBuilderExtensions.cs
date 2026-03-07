using Microsoft.Extensions.Configuration;

namespace DopplerSDK.ConfigurationProvider;

public static class DopplerConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDoppler(
        this IConfigurationBuilder builder,
        Action<DopplerConfigurationSource> configureSource)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (configureSource is null) throw new ArgumentNullException(nameof(configureSource));

        var source = new DopplerConfigurationSource();
        configureSource(source);
        return builder.Add(source);
    }

    public static IConfigurationBuilder AddDoppler(
        this IConfigurationBuilder builder,
        Action<DopplerClientConfiguration> configureClient,
        bool failFast = true,
        TimeSpan? reloadInterval = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (configureClient is null) throw new ArgumentNullException(nameof(configureClient));

        var source = new DopplerConfigurationSource
        {
            FailFast = failFast,
            ReloadInterval = reloadInterval
        };

        configureClient(source.DopplerClientConfiguration);
        return builder.Add(source);
    }

    public static IConfigurationBuilder AddDoppler(
        this IConfigurationBuilder builder,
        DopplerClientConfiguration configuration,
        bool failFast = true,
        TimeSpan? reloadInterval = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        return builder.AddDoppler(source =>
        {
            source.FailFast = failFast;
            source.ReloadInterval = reloadInterval;

            source.DopplerClientConfiguration.DopplerToken = configuration.DopplerToken;
            source.DopplerClientConfiguration.DopplerNameTransformer = configuration.DopplerNameTransformer;
            source.DopplerClientConfiguration.DopplerApiHost = configuration.DopplerApiHost;
            source.DopplerClientConfiguration.RequestTimeout = configuration.RequestTimeout;
        });
    }
}
