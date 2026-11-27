using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TornDiscordBot.Torn;

namespace TornDiscordBot.Tests.Torn;

/// <summary>Tests for <see cref="TornClient"/> using a fake HTTP handler, so Torn is never called.</summary>
public class TornClientTests
{
    private const string Key = "AbCdEfGh12345678";
    private const string CooldownsJson = """{"cooldowns":{"drug":0,"medical":0,"booster":0}}""";

    [Fact]
    public async Task ValidateKeyAsync_SendsKeyInHeaderOnly()
    {
        var handler = new FakeHandler(CooldownsJson);

        await CreateClient(handler).ValidateKeyAsync(Key);

        var request = Assert.IsType<HttpRequestMessage>(handler.Request);
        Assert.Equal("https://api.torn.com/v2/user/cooldowns", request.RequestUri?.ToString());
        Assert.Equal("ApiKey", request.Headers.Authorization?.Scheme);
        Assert.Equal(Key, request.Headers.Authorization?.Parameter);
    }

    [Theory]
    [InlineData(CooldownsJson, KeyValidationResult.Valid)]
    [InlineData("""{"error":{"code":2,"error":"Incorrect key"}}""", KeyValidationResult.Invalid)]
    [InlineData("""{"error":{"code":18,"error":"Key paused"}}""", KeyValidationResult.Invalid)]
    [InlineData("""{"error":{"code":16,"error":"Access level too low"}}""", KeyValidationResult.AccessLevelTooLow)]
    [InlineData("""{"error":{"code":10,"error":"Federal jail"}}""", KeyValidationResult.Valid)]
    [InlineData("""{"error":{"code":5,"error":"Too many requests"}}""", KeyValidationResult.Unavailable)]
    [InlineData("""{"unexpected":true}""", KeyValidationResult.Unavailable)]
    [InlineData("<html>Bad gateway</html>", KeyValidationResult.Unavailable)]
    public async Task ValidateKeyAsync_MapsResponse(string body, KeyValidationResult expected)
    {
        var result = await CreateClient(new FakeHandler(body)).ValidateKeyAsync(Key);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ValidateKeyAsync_ReturnsUnavailable_WhenRequestFails()
    {
        var handler = new FakeHandler(new HttpRequestException("offline"));

        var result = await CreateClient(handler).ValidateKeyAsync(Key);

        Assert.Equal(KeyValidationResult.Unavailable, result);
    }

    private static TornClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.torn.com/v2/") },
            NullLogger<TornClient>.Instance);

    /// <summary>Returns a canned response, or throws, and records the request.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string? _body;
        private readonly Exception? _exception;

        public FakeHandler(string body) => _body = body;

        public FakeHandler(Exception exception) => _exception = exception;

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;

            return _exception is not null
                ? Task.FromException<HttpResponseMessage>(_exception)
                : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_body!) });
        }
    }
}
