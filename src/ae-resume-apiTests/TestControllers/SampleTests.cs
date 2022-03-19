﻿using ae_resume_api.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using ae_resume_api.Tests;
using Microsoft.Extensions.Configuration;
using IdentityModel.Client;
using System.IO;

namespace ae_resume_api.Controllers.Tests
{

    public class SampleTests : IClassFixture<WebApplicationFactory<ae_resume_api.Startup>>
    {
        private readonly IConfigurationRoot _config;
        private readonly HttpClient _client;
        private readonly ApiTokenInMemoryClient _tokenService;

        public SampleTests(WebApplicationFactory<ae_resume_api.Startup> application)
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            _client = application.CreateClient(new WebApplicationFactoryClientOptions()
                {
                    BaseAddress = new Uri(_config["Tests:API"])
                });

            _tokenService = new ApiTokenInMemoryClient(_config);
        }

        [Fact]
        public async Task IdentityTest()
        {
            var token = await _tokenService.GetSAAccessToken();
            _client.SetBearerToken(token);

            // Act
            var response = await _client.GetAsync("identity");
            response.EnsureSuccessStatusCode();

            var responseString = Encoding.UTF8.GetString(
                await response.Content.ReadAsByteArrayAsync()
            );

            Console.WriteLine(responseString);
        }

    }
}