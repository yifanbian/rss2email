using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RssToEmail
{
    public sealed class MicrosoftGraphAuthenticationProvider : IAuthenticationProvider, IDisposable
    {
        private static readonly string[] s_scopes = { "https://graph.microsoft.com/.default" };

        private readonly IConfidentialClientApplication _cca;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private AuthenticationResult? _authenticationResult;

        public MicrosoftGraphAuthenticationProvider(string tenantId, string clientId, string clientSecret)
        {
            _cca = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}/v2.0"))
                .Build();
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", await GetAccessTokenAsync());
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            try
            {
                await _semaphore.WaitAsync();
                if (_authenticationResult == null ||
                    _authenticationResult.ExpiresOn.UtcDateTime < DateTime.UtcNow.AddMinutes(-1))
                {
                    _authenticationResult = await _cca.AcquireTokenForClient(s_scopes).ExecuteAsync();
                }

                return _authenticationResult.AccessToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
