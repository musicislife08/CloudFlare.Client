﻿using System.Linq;
using System.Threading.Tasks;
using CloudFlare.Client.Api.Accounts;
using CloudFlare.Client.Api.Parameters.Endpoints;
using CloudFlare.Client.Contexts;
using CloudFlare.Client.Test.Helpers;
using CloudFlare.Client.Test.TestData;
using FluentAssertions;
using Force.DeepCloner;
using Newtonsoft.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CloudFlare.Client.Test.Accounts
{
    public class AccountsUnitTests
    {
        private readonly WireMockServer _wireMockServer;
        private readonly ConnectionInfo _connectionInfo;

        public AccountsUnitTests()
        {
            _wireMockServer = WireMockServer.Start();
            _connectionInfo = new WireMockConnection(_wireMockServer.Urls.First()).ConnectionInfo;
        }

        [Fact]
        public async Task TestGetAccountsAsync()
        {
            _wireMockServer
                .Given(Request.Create().WithPath($"/{AccountEndpoints.Base}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(AccountTestData.Accounts)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var accounts = await client.Accounts.GetAsync();

            accounts.Result.Should().BeEquivalentTo(AccountTestData.Accounts);
        }

        [Fact]
        public async Task TestGetAccountDetailsAsync()
        {
            var account = AccountTestData.Accounts.First();

            _wireMockServer
                .Given(Request.Create().WithPath($"/{AccountEndpoints.Base}/{account.Id}").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithBody(WireMockResponseHelper.CreateTestResponse(account)));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var accountDetails = await client.Accounts.GetDetailsAsync(account.Id);

            accountDetails.Result.Should().BeEquivalentTo(account);
        }

        [Fact]
        public async Task UpdateAccountAsync()
        {
            var account = AccountTestData.Accounts.First();

            _wireMockServer
                .Given(Request.Create().WithPath($"/{AccountEndpoints.Base}/{account.Id}").UsingPut())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(x =>
                {
                    var body = JsonConvert.DeserializeObject<Account>(x.Body);
                    var acc = AccountTestData.Accounts.First(y => y.Id == body.Id).DeepClone();

                    acc.Id = body.Id;
                    acc.Name = body.Name;
                    acc.Settings = body.Settings;

                    return WireMockResponseHelper.CreateTestResponse(acc);
                }));

            using var client = new CloudFlareClient(WireMockConnection.ApiKeyAuthentication, _connectionInfo);

            var updatedAccount = await client.Accounts.UpdateAsync(account.Id, "New Name", new AdditionalAccountSettings
            {
                EnforceTwoFactorAuthentication = true
            });

            updatedAccount.Result.Name.Should().Be("New Name");
            updatedAccount.Result.Settings.EnforceTwoFactorAuthentication.Should().BeTrue();
            updatedAccount.Result.Should().BeEquivalentTo(account, opt => opt.Excluding(x => x.Name).Excluding(x => x.Settings.EnforceTwoFactorAuthentication));
        }
    }
}
