using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using RiftLens.Api.Security;
using RiftLens.Contracts;

namespace RiftLens.Api.Tests;

public sealed class AccessKeyTests(AccessKeyTests.ApiFactory factory) : IClassFixture<AccessKeyTests.ApiFactory>
{
    private const string ValidKey = "test-key-0123456789-0123456789-abcdef";

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:RiftLens", "Server=invalid;Database=none");
            builder.UseSetting("Riot:ApiKey", "RGAPI-test");
            builder.UseSetting("Access:Keys:0", ValidKey);
        }
    }

    [Fact]
    public async Task Sans_cle_l_api_repond_401()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/meta", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Mauvaise_cle_l_api_repond_401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, "nope");
        using var response = await client.GetAsync(new Uri("/api/meta", UriKind.Relative));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bonne_cle_l_api_repond_200()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, ValidKey);
        using var response = await client.GetAsync(new Uri("/api/meta", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("abd", false)]
    [InlineData("", false)]
    public void IsValid_compare_les_cles(string provided, bool expected) =>
        Assert.Equal(expected, AccessKeyMiddleware.IsValid(provided, ["xyz", "abc", ""]));
}
