using System.Net;
using System.Security.Cryptography;
using System.Text;
using clean.Domain.Exceptions;
using clean.Infrastructure.Identity;
namespace clean.Tests;
public sealed class PasswordScreeningTests
{
    private const string Password = "test-only long password phrase";
    [Fact] public async Task SendsOnlyHashPrefixAndIgnoresPadding()
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(Password)));
        var handler = new StubHandler(request => {
            Assert.Equal("/range/" + hash[..5], request.RequestUri!.AbsolutePath);
            Assert.Equal("true", Assert.Single(request.Headers.GetValues("Add-Padding")));
            return new(HttpStatusCode.OK) { Content = new StringContent(hash[5..] + ":0") };
        });
        var service = new PwnedPasswordScreening(new HttpClient(handler) { BaseAddress = new("https://example.test/") });
        Assert.False(await service.IsCompromisedAsync(Password, default));
    }
    [Theory] [InlineData("malformed", 200)] [InlineData("", 503)]
    public async Task FailsClosedForInvalidOrUnavailableResponse(string body, int status)
    {
        var service = new PwnedPasswordScreening(new HttpClient(new StubHandler(_ =>
            new((HttpStatusCode)status) { Content = new StringContent(body) })) { BaseAddress = new("https://example.test/") });
        var error = await Assert.ThrowsAsync<RuleException>(() => service.IsCompromisedAsync(Password, default));
        Assert.Equal("PASSWORD_SCREENING_UNAVAILABLE", error.Code);
    }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
