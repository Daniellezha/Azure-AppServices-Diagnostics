﻿using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics.DataProviders
{
    public class KustoTokenService
    {
        private AuthenticationContext _authContext;
        private ClientCredential _clientCredential;
        private KustoDataProviderConfiguration _configuration;
        private static readonly Lazy<KustoTokenService> _instance = new Lazy<KustoTokenService>(() => new KustoTokenService());
        private string _authorizationToken;
        private bool _tokenAcquiredAtleastOnce;
        private Task<AuthenticationResult> _acquireTokenTask;

        public static KustoTokenService Instance => _instance.Value;

        public string AuthorizationToken => _authorizationToken;

        private KustoTokenService() : base()
        {   
        }

        public void Initialize(KustoDataProviderConfiguration configuration)
        {
            _configuration = configuration;
            _authContext = new AuthenticationContext(DataProviderConstants.MicrosoftTenantAuthorityUrl);
            _clientCredential = new ClientCredential(_configuration.ClientId, _configuration.AppKey);
            _tokenAcquiredAtleastOnce = false;
            StartTokenRefresh();
        }

        private async Task StartTokenRefresh()
        {
            while (true)
            {
                _acquireTokenTask = _authContext.AcquireTokenAsync(DataProviderConstants.DefaultKustoEndpoint, _clientCredential);
                AuthenticationResult authResult = await _acquireTokenTask;
                _authorizationToken = GetAuthTokenFromAuthenticationResult(authResult);
                _tokenAcquiredAtleastOnce = true;

                await Task.Delay(DataProviderConstants.TokenRefreshIntervalInMs);
            }
        }

        private string GetAuthTokenFromAuthenticationResult(AuthenticationResult authenticationResult)
        {
            return $"{authenticationResult.AccessTokenType} {authenticationResult.AccessToken}";
        }

        public async Task<string> GetAuthorizationTokenAsync()
        {
            if (!_tokenAcquiredAtleastOnce)
            {
                var authResult = await _acquireTokenTask;
                return GetAuthTokenFromAuthenticationResult(authResult);
            }

            return _authorizationToken;
        }
    }
}
