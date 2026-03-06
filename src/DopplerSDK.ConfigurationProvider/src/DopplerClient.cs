using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using static System.String;

namespace DopplerSDK.ConfigurationProvider;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable ConvertToConstant.Global
using DopplerSecrets = Dictionary<string, string>;

public static class DopplerNameTransformers
{
    public static readonly string DotNet = "dotnet";
    public static readonly string DotNetEnv = "dotnet-env";
    public static readonly string None = "none";
    public static readonly List<string> List = new() { DotNet, DotNetEnv, None };

    public static bool Validate(string nameTransformer)
    {
        return List.Contains(nameTransformer);
    }
}

public class DopplerClientConfiguration
{
    public string? DopplerToken { get; set; } = Empty;
    public string DopplerNameTransformer { get; set; } = DopplerNameTransformers.DotNet;
    public string DopplerApiHost { get; set; } = DopplerClient.DefaultApiHost;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class DopplerClientResponse
{
    public readonly DopplerSecrets Secrets;
    public readonly string StatusMessage;
    public readonly bool IsSuccess;

    public DopplerClientResponse(DopplerSecrets? secrets, string? statusMessage)
        : this(secrets, statusMessage, (secrets?.Count ?? 0) > 0)
    {
    }

    public DopplerClientResponse(DopplerSecrets? secrets, string? statusMessage, bool isSuccess)
    {
        Secrets = secrets ?? new DopplerSecrets();
        StatusMessage = statusMessage ?? Empty;
        IsSuccess = isSuccess;
    }
}

public class DopplerClient : IDisposable
{
    public const string DefaultApiHost = "https://api.doppler.com";
    private const string DefaultApiPath = "/v3/configs/config/secrets/download";

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public string ApiPath = DefaultApiPath;
    public DopplerClientConfiguration DopplerClientConfiguration;

    public DopplerClient(DopplerClientConfiguration dopplerClientConfiguration)
        : this(dopplerClientConfiguration, null)
    {
    }

    internal DopplerClient(DopplerClientConfiguration dopplerClientConfiguration, HttpClient? httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
        _disposeHttpClient = httpClient is null;
        DopplerClientConfiguration = dopplerClientConfiguration;
        _httpClient.Timeout = DopplerClientConfiguration.RequestTimeout;
    }

    private bool TryValidateConfiguration(out string statusMessage)
    {
        statusMessage = Empty;

        if (IsNullOrEmpty(DopplerClientConfiguration.DopplerToken))
        {
            statusMessage = "Doppler Client Error: DopplerClientConfiguration.DopplerToken not set";
            return false;
        }

        if (!DopplerNameTransformers.Validate(DopplerClientConfiguration.DopplerNameTransformer))
        {
            statusMessage =
                $"Doppler Client Error: DopplerClientConfiguration.DopplerNameTransformer must be one of {Join(", ", DopplerNameTransformers.List)}";
            return false;
        }

        if (!Uri.TryCreate(DopplerClientConfiguration.DopplerApiHost, UriKind.Absolute, out var hostUri))
        {
            statusMessage = "Doppler Client Error: DopplerClientConfiguration.DopplerApiHost must be an absolute URI";
            return false;
        }

        if (hostUri.Scheme != Uri.UriSchemeHttps && hostUri.Scheme != Uri.UriSchemeHttp)
        {
            statusMessage = "Doppler Client Error: DopplerClientConfiguration.DopplerApiHost must use http or https";
            return false;
        }

        if (DopplerClientConfiguration.RequestTimeout <= TimeSpan.Zero)
        {
            statusMessage = "Doppler Client Error: DopplerClientConfiguration.RequestTimeout must be greater than zero";
            return false;
        }

        return true;
    }

    private Uri ClientUrl()
    {
        var host = DopplerClientConfiguration.DopplerApiHost.TrimEnd('/');
        var path = ApiPath.TrimStart('/');
        var uriBuilder = new UriBuilder($"{host}/{path}");

        var query = "format=json";
        if (DopplerClientConfiguration.DopplerNameTransformer != DopplerNameTransformers.None)
            query += $"&name_transformer={Uri.EscapeDataString(DopplerClientConfiguration.DopplerNameTransformer)}";
        uriBuilder.Query = query;

        return uriBuilder.Uri;
    }

    private AuthenticationHeaderValue AuthHeader()
    {
        var basicAuthHeader =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(DopplerClientConfiguration.DopplerToken + ":"));
        return new AuthenticationHeaderValue("Basic", basicAuthHeader);
    }

    public async Task<DopplerClientResponse> FetchSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (!TryValidateConfiguration(out var statusMessage))
            return new DopplerClientResponse(null, statusMessage, false);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ClientUrl());
            request.Headers.Authorization = AuthHeader();

            using var httpResponseMessage = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var secrets = await httpResponseMessage.Content.ReadFromJsonAsync<DopplerSecrets>().ConfigureAwait(false);
                return new DopplerClientResponse(secrets, "Ok", true);
            }

            var httpContent = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            var errorMessage = $"Doppler API Error: {httpResponseMessage.StatusCode} {httpContent}";
            return new DopplerClientResponse(null, errorMessage, false);
        }
        catch (HttpRequestException httpRequestException)
        {
            return new DopplerClientResponse(null,
                $"Doppler Client HttpRequestException: {httpRequestException.Message}", false);
        }
        catch (JsonException jsonException)
        {
            return new DopplerClientResponse(null,
                $"Doppler Client JsonException: {jsonException.Message}", false);
        }
        catch (TaskCanceledException taskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DopplerClientResponse(null,
                $"Doppler Client Timeout: {taskCanceledException.Message}", false);
        }
    }

    public void Dispose()
    {
        if (_disposeHttpClient) _httpClient.Dispose();
    }
}
