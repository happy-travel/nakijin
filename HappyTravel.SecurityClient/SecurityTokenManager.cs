using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.SecurityClient
{
    public class SecurityTokenManager : ISecurityTokenManager
    {
        public SecurityTokenManager(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory, IOptions<TokenRequestOptions> options)
        {
            _clientFactory = clientFactory;
            _logger = loggerFactory.CreateLogger<SecurityTokenManager>();
            _tokenRequestOptions = options.Value;
        }


        public async Task<string> Get()
        {
            try
            {
                await _getTokenSemaphore.WaitAsync();
                var now = DateTime.UtcNow;

                // Refreshing token if it's empty or will expire soon.
                if (_tokenInfo.Equals(default) || _tokenInfo.ExpiryDate <= now)
                    await Refresh();

                return _tokenInfo.Token;
            }
            finally
            {
                _getTokenSemaphore.Release();
            }
        }


        public async Task Refresh()
        {
            try
            {
                // If someone refreshes token right now, there is no need to refresh it again.
                var isTokenRefreshAlreadyStarted = _refreshTokenSemaphore.CurrentCount == 0;
                // Anyway, will wait until other refresh finishes. This is indicated by released semaphore.
                await _refreshTokenSemaphore.WaitAsync();
                if (isTokenRefreshAlreadyStarted)
                    return;
                
                var now = DateTime.UtcNow;
                using var client = _clientFactory.CreateClient();

                var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = _tokenRequestOptions.Address,
                    Scope = _tokenRequestOptions.Scope,
                    ClientId = _tokenRequestOptions.ClientId,
                    ClientSecret = _tokenRequestOptions.ClientSecret,
                    GrantType = _tokenRequestOptions.GrantType
                });

                if (tokenResponse.IsError)
                {
                    var errorMessage = $"Something went wrong while requesting the access token. Error: {tokenResponse.Error}. " +
                        $"Using existing token: '{_tokenInfo.Token}' with expiry date '{_tokenInfo.ExpiryDate}'";

                    _logger.LogGetTokenForConnectorError(errorMessage);
                }
                else
                {
                    _tokenInfo = (tokenResponse.AccessToken, now.AddSeconds(tokenResponse.ExpiresIn));
                }
            }
            finally
            {
                _refreshTokenSemaphore.Release();
            }
        }

        
        private (string Token, DateTime ExpiryDate) _tokenInfo;

        private readonly SemaphoreSlim _getTokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _refreshTokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<SecurityTokenManager> _logger;
        private readonly TokenRequestOptions _tokenRequestOptions;
    }
}
