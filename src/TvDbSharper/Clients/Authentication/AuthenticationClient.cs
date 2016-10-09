﻿namespace TvDbSharper.Clients.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    using TvDbSharper.Clients.Authentication.Json;
    using TvDbSharper.Errors;
    using TvDbSharper.JsonClient;

    public class AuthenticationClient : IAuthenticationClient
    {
        public AuthenticationClient(IJsonClient jsonClient, IErrorMessages errorMessages)
        {
            this.JsonClient = jsonClient;
            this.ErrorMessages = errorMessages;
        }

        private IErrorMessages ErrorMessages { get; }

        private IJsonClient JsonClient { get; }

        public async Task AuthenticateAsync(AuthenticationRequest authenticationRequest, CancellationToken cancellationToken)
        {
            if (authenticationRequest == null)
            {
                throw new ArgumentNullException(nameof(authenticationRequest));
            }

            try
            {
                var response =
                    await this.JsonClient.PostJsonAsync<AuthenticationResponse>("/login", authenticationRequest, cancellationToken);

                this.UpdateAuthenticationHeader(response.Token);
            }
            catch (TvDbServerException ex)
            {
                string message = this.GetMessage(ex.StatusCode, this.ErrorMessages.Authentication.AuthenticateAsync);

                if (message == null)
                {
                    throw;
                }

                throw new TvDbServerException(message, ex.StatusCode, ex);
            }
        }

        public async Task AuthenticateAsync(string apiKey, string username, string userKey, CancellationToken cancellationToken)
        {
            if (apiKey == null)
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("The ApiKey cannot be an empty string or white space.");
            }

            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The Username cannot be an empty string or white space.");
            }

            if (userKey == null)
            {
                throw new ArgumentNullException(nameof(userKey));
            }

            if (string.IsNullOrWhiteSpace(userKey))
            {
                throw new ArgumentException("The UserKey cannot be an empty string or white space.");
            }

            await this.AuthenticateAsync(new AuthenticationRequest(apiKey, username, userKey), cancellationToken);
        }

        public async Task AuthenticateAsync(string apiKey, string username, string value)
        {
            await this.AuthenticateAsync(apiKey, username, value, CancellationToken.None);
        }

        public async Task AuthenticateAsync(AuthenticationRequest authenticationRequest)
        {
            await this.AuthenticateAsync(authenticationRequest, CancellationToken.None);
        }

        public async Task RefreshTokenAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.JsonClient.GetJsonAsync<AuthenticationResponse>("/refresh_token", cancellationToken);

                this.UpdateAuthenticationHeader(response.Token);
            }
            catch (TvDbServerException ex)
            {
                string message = this.GetMessage(ex.StatusCode, this.ErrorMessages.Authentication.RefreshTokenAsync);

                if (message == null)
                {
                    throw;
                }

                throw new TvDbServerException(message, ex.StatusCode, ex);
            }
        }

        public async Task RefreshTokenAsync()
        {
            await this.RefreshTokenAsync(CancellationToken.None);
        }

        private string GetMessage(HttpStatusCode statusCode, IDictionary<int, string> messagesDictionary)
        {
            if (messagesDictionary.ContainsKey((int)statusCode))
            {
                return messagesDictionary[(int)statusCode];
            }

            return null;
        }

        private void UpdateAuthenticationHeader(string token)
        {
            this.JsonClient.AuthorizationHeader = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}