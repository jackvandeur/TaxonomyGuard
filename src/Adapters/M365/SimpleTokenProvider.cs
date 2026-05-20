using System.Net.Http.Headers;
using PnP.Core.Services;

namespace EnterpriseGovernance.Adapters.M365;

public class SimpleTokenProvider : IAuthenticationProvider
{
    private readonly string _accessToken;

    public SimpleTokenProvider(string accessToken)
    {
        _accessToken = accessToken;
    }

    // Voldoet aan de HTTP-request injectie
    public Task AuthenticateRequestAsync(Uri resource, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        return Task.CompletedTask;
    }

    // Voldoe aan de overload met resource en scopes
    public Task<string> GetAccessTokenAsync(Uri resource, string[] scopes)
    {
        return Task.FromResult(_accessToken);
    }

    // Gecorrigeerd: De ontbrekende overload die de compiler zocht (alleen resource)
    public Task<string> GetAccessTokenAsync(Uri resource)
    {
        return Task.FromResult(_accessToken);
    }
}