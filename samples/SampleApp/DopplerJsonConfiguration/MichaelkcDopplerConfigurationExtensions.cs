using DopplerSDK.ConfigurationProvider;
using System.Text;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration;

public static class MichaelkcDopplerConfigurationExtensions
{
    public static IConfigurationManager AddMichaelkcDoppler(
        this IConfigurationManager manager,
        string dopplerToken = null,
        IEnumerable<string> keysToLoad = null)
    {
        dopplerToken ??= manager["DopplerToken"];
        if (string.IsNullOrWhiteSpace(dopplerToken))
        {
            throw new InvalidOperationException(
                "DopplerToken is required. Provide it as an argument or via configuration (for example User Secrets).");
        }

        var dopplerNameTransformer = manager["DopplerNameTransformer"] ?? DopplerNameTransformers.DotNet;
        var configuredApiHost = manager["DopplerApiHost"];
        var requestedKeys = keysToLoad?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedKeys != null && requestedKeys.Length > 0)
        {
            var loadedConfiguration = LoadSelectedKeysAsConfiguration(
                dopplerToken,
                dopplerNameTransformer,
                configuredApiHost,
                requestedKeys);
            manager.AddInMemoryCollection(loadedConfiguration);
            return manager;
        }

        manager.AddDoppler(doppler =>
        {
            doppler.DopplerToken = dopplerToken;
            doppler.DopplerNameTransformer = dopplerNameTransformer;

            if (!string.IsNullOrWhiteSpace(configuredApiHost))
            {
                doppler.DopplerApiHost = configuredApiHost;
            }
        });

        return manager;
    }

    private static Dictionary<string, string> LoadSelectedKeysAsConfiguration(
        string dopplerToken,
        string dopplerNameTransformer,
        string dopplerApiHost,
        string[] requestedKeys)
    {
        var clientConfiguration = new DopplerClientConfiguration
        {
            DopplerToken = dopplerToken,
            DopplerNameTransformer = dopplerNameTransformer
        };
        if (!string.IsNullOrWhiteSpace(dopplerApiHost))
        {
            clientConfiguration.DopplerApiHost = dopplerApiHost;
        }

        using var dopplerClient = new DopplerClient(clientConfiguration);
        var response = dopplerClient.FetchSecretsAsync(CancellationToken.None).GetAwaiter().GetResult();
        if (!response.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to load Doppler secrets. {response.StatusMessage}");
        }

        var requestedKeySet = new HashSet<string>(requestedKeys, StringComparer.OrdinalIgnoreCase);
        var selectedSecrets = response.Secrets
            .Where(secret => requestedKeySet.Contains(secret.Key))
            .ToArray();

        if (selectedSecrets.Length != requestedKeys.Length)
        {
            var foundKeys = new HashSet<string>(selectedSecrets.Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var missing = requestedKeys.Where(x => !foundKeys.Contains(x)).ToArray();
            throw new InvalidOperationException(
                $"Could not find requested Doppler keys: {string.Join(", ", missing)}, found keys {string.Join(", ", foundKeys)}");
        }

        var configurationValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var secret in selectedSecrets)
        {
            if (!TryAddSecretAsJsonConfiguration(secret.Key, secret.Value, configurationValues))
            {
                if (!configurationValues.TryAdd(secret.Key, secret.Value))
                {
                    throw new InvalidOperationException(
                        $"Duplicate configuration key '{secret.Key}' while loading selected Doppler keys.");
                }
            }
        }

        return configurationValues;
    }

    private static bool TryAddSecretAsJsonConfiguration(
        string secretName,
        string secretValue,
        Dictionary<string, string> configurationValues)
    {
        try
        {
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(secretValue));
            var jsonConfiguration = new ConfigurationBuilder()
                .AddJsonStream(jsonStream)
                .Build();

            var parsedItems = jsonConfiguration.AsEnumerable()
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .ToArray();

            if (parsedItems.Length == 0)
            {
                return false;
            }

            foreach (var item in parsedItems)
            {
                if (!configurationValues.TryAdd(item.Key, item.Value))
                {
                    throw new InvalidOperationException(
                        $"Duplicate configuration key '{item.Key}' while expanding Doppler JSON secret '{secretName}'.");
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
