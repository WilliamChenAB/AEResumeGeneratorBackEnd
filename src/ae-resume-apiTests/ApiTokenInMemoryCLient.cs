﻿using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ae_resume_api.Tests
{
    public class ApiTokenInMemoryClient
    {
        private IConfigurationRoot _configuration;

        public ApiTokenInMemoryClient(IConfigurationRoot config)
        {
            _configuration = config;
        }

        public async Task<string> GetUserAccessToken()
        {
            Parameters p = new Parameters()
                {
                    { "username", "user"},
                    { "password", "Abcd1234"},
                    { "scope", _configuration["Tests:Scope"] }
                };

            return await GetAccessToken(p);
        }

        //public static async Task<string> GetPAAccessToken()
        //{
        //    Parameters p = new Parameters()
        //        {
        //            { "username", "admin"},
        //            { "password", "Abcd1234"},
        //            { "scope", _configuration["Tests:Scope"] }
        //        };

        //    return await GetAccessToken(p);
        //}

        public async Task<string> GetSAAccessToken()
        {
            Parameters p = new Parameters()
                {
                    { "username", "admin"},
                    { "password", "Abcd1234"},
                    { "scope", _configuration["Tests:Scope"] }
                };

            return await GetAccessToken(p);
        }

        private async Task<string> GetAccessToken(Parameters parameters)
        {
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(_configuration["Authority"]);
            if (!String.IsNullOrEmpty(disco.Error))
            {
                throw new Exception(disco.Error);
            }

            var tr = new TokenRequest
            {
                Address = disco.TokenEndpoint,
                GrantType = IdentityModel.OidcConstants.GrantTypes.Password,
                ClientId = _configuration["Tests:Client"],
                ClientSecret = _configuration["Tests:Secret"],

                Parameters = parameters
            };

            var response = await client.RequestTokenAsync(tr);

            return response.AccessToken;
        }

    }
}