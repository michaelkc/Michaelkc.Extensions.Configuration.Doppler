using System.Net;
using System.Text;

namespace DopplerSDK.ConfigurationProvider.Tests;

public class DopplerClientTests
{
    [Fact]
    public async Task FetchSecretsAsync_ReturnsFailure_WhenTokenMissing()
    {
        using var client = new DopplerClient(new DopplerClientConfiguration
        {
            DopplerToken = string.Empty
        });

        var response = await client.FetchSecretsAsync();

        Assert.False(response.IsSuccess);
        Assert.Contains("DopplerToken not set", response.StatusMessage);
    }

    [Fact]
    public async Task FetchSecretsAsync_ReturnsSecrets_WhenApiReturnsSuccess()
    {
        var handler = new TestHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"Debug\":\"true\",\"Smtp:Server\":\"smtp.jedi-academy.com\"}", Encoding.UTF8,
                    "application/json")
            });

        using var httpClient = new HttpClient(handler);
        using var client = new DopplerClient(new DopplerClientConfiguration
        {
            DopplerToken = "dp.st.test",
            DopplerNameTransformer = DopplerNameTransformers.DotNet
        }, httpClient);

        var response = await client.FetchSecretsAsync();

        Assert.True(response.IsSuccess);
        Assert.Equal("true", response.Secrets["Debug"]);
        Assert.Equal("smtp.jedi-academy.com", response.Secrets["Smtp:Server"]);
    }

    [Fact]
    public async Task FetchSecretsAsync_OmitsNameTransformerQuery_WhenTransformerIsNone()
    {
        Uri? requestUri = null;
        var handler = new TestHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        using var client = new DopplerClient(new DopplerClientConfiguration
        {
            DopplerToken = "dp.st.test",
            DopplerNameTransformer = DopplerNameTransformers.None
        }, httpClient);

        var response = await client.FetchSecretsAsync();

        Assert.True(response.IsSuccess);
        Assert.NotNull(requestUri);
        Assert.DoesNotContain("name_transformer", requestUri.Query, StringComparison.OrdinalIgnoreCase);
    }
}
