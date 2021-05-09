using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
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
        private readonly TokenCredential _credential;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private AccessToken? _authenticationResult;

        public MicrosoftGraphAuthenticationProvider()
        {
            _credential = new DefaultAzureCredential();
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            string accessToken = await GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
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
                    _authenticationResult.Value.ExpiresOn.UtcDateTime < DateTime.UtcNow.AddMinutes(-1))
                {
                    _authenticationResult = await _credential.GetTokenAsync(new TokenRequestContext(s_scopes), default);
                }

                return _authenticationResult.Value.Token;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
