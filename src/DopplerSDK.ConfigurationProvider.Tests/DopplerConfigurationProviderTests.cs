using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DopplerSDK.ConfigurationProvider.Tests;

public class DopplerConfigurationProviderTests
{
    [Fact]
    public void Load_Throws_WhenFailFastEnabledAndFetchFails()
    {
        var source = CreateSource(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"nope\"}", Encoding.UTF8, "application/json")
            },
            failFast: true);
        using var provider = (DopplerConfigurationProvider)source.Build(new ConfigurationBuilder());

        var exception = Assert.Throws<InvalidOperationException>(provider.Load);
        Assert.Contains("Doppler configuration load failed", exception.Message);
    }

    [Fact]
    public void Load_DoesNotThrow_WhenFailFastDisabledAndFetchFails()
    {
        var source = CreateSource(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"still nope\"}", Encoding.UTF8, "application/json")
            },
            failFast: false);
        using var provider = (DopplerConfigurationProvider)source.Build(new ConfigurationBuilder());

        provider.Load();

        Assert.False(provider.TryGet("Debug", out _));
    }

    [Fact]
    public void Load_PopulatesCaseInsensitiveData()
    {
        var source = CreateSource(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Smtp:Server\":\"smtp.jedi-academy.com\"}", Encoding.UTF8,
                    "application/json")
            },
            failFast: true);
        using var provider = (DopplerConfigurationProvider)source.Build(new ConfigurationBuilder());

        provider.Load();

        Assert.True(provider.TryGet("smtp:server", out var server));
        Assert.Equal("smtp.jedi-academy.com", server);
    }

    [Fact]
    public async Task Load_ReloadsData_WhenReloadIntervalIsConfigured()
    {
        var attempt = 0;
        var source = CreateSource(
            _ =>
            {
                var body = Interlocked.Increment(ref attempt) == 1
                    ? "{\"Debug\":\"false\"}"
                    : "{\"Debug\":\"true\"}";

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            },
            failFast: true,
            reloadInterval: TimeSpan.FromMilliseconds(50));
        using var provider = (DopplerConfigurationProvider)source.Build(new ConfigurationBuilder());

        provider.Load();
        Assert.True(provider.TryGet("Debug", out var initialValue));
        Assert.Equal("false", initialValue);

        var reloaded = await WaitForAsync(() =>
        {
            var hasValue = provider.TryGet("Debug", out var value);
            return hasValue && value == "true";
        }, TimeSpan.FromSeconds(2));

        Assert.True(reloaded);
    }

    private static DopplerConfigurationSource CreateSource(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory,
        bool failFast,
        TimeSpan? reloadInterval = null)
    {
        var source = new DopplerConfigurationSource
        {
            FailFast = failFast,
            ReloadInterval = reloadInterval,
            ClientFactory = config => new DopplerClient(config, new HttpClient(new TestHttpMessageHandler(responseFactory)))
        };

        source.DopplerClientConfiguration.DopplerToken = "dp.st.test";
        return source;
    }

    private static async Task<bool> WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            if (condition()) return true;
            await Task.Delay(25);
        }

        return false;
    }
}
