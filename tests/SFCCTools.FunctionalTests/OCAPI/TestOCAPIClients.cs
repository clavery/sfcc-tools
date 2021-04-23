using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using SFCCTools.AccountManager;
using SFCCTools.FunctionalTests.WebDAV;
using SFCCTools.OCAPI.DataAPI.Resources;
using SFCCTools.OCAPI.DataAPI.Types;
using SFCCTools.OCAPI.SharedTypes;
using SFCCTools.OCAPI.ShopAPI.Resources;
using SFCCTools.WebDAV;
using Xunit;

namespace SFCCTools.FunctionalTests.OCAPI
{
    public class TestOCAPIClients : IClassFixture<SFCCEnvironmentFixture>
    {
        private readonly ISites _sitesClient;
        private IJobs _jobsClient;
        private IGlobalPreferences _globalPreferencesClient;
        private IOrderSearch _orderSearchClient;
        private IOrders _ordersClient;
        private IJobExecutionSearch _jobSearchClient;
        private ICodeVersions _codeVersionsClient;

        public TestOCAPIClients(SFCCEnvironmentFixture fixture)
        {
            _sitesClient = fixture.ServiceProvider.GetService<ISites>();
            _jobsClient = fixture.ServiceProvider.GetService<IJobs>();
            _globalPreferencesClient = fixture.ServiceProvider.GetService<IGlobalPreferences>();
            _orderSearchClient = fixture.ServiceProvider.GetService<IOrderSearch>();
            _ordersClient = fixture.ServiceProvider.GetService<IOrders>();
            _jobSearchClient = fixture.ServiceProvider.GetService<IJobExecutionSearch>();
            _codeVersionsClient = fixture.ServiceProvider.GetService<ICodeVersions>();
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestGetAllSites()
        {
            var sites = await _sitesClient.GetAll();
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestExportMetadata()
        {
            var configuration = new SiteArchiveExportConfiguration
            {
                ExportFile = "TestExportFile.zip",
                OverwriteExportFile = true,
                DataUnits = {GlobalData = {MetaData = true}}
            };
            try
            {
                var jobExecution = await _jobsClient.SiteArchiveExport(configuration);
            }
            catch (ApiException exception)
            {
                var fault = await exception.GetContentAsAsync<OCAPIError>();
            }
        }


        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestReusingAccessTokenForMultipleRequests()
        {
            try
            {
                var jobExecution = await _jobsClient.SiteArchiveExport(new SiteArchiveExportConfiguration()
                {
                    ExportFile = "TestExportFile2.zip",
                    OverwriteExportFile = true,
                    DataUnits =
                    {
                        GlobalData =
                        {
                            MetaData = true
                        }
                    }
                });
            }
            catch (ApiException exception)
            {
                var fault = await exception.GetContentAsAsync<OCAPIError>();
            }

            var sites = await _sitesClient.GetAll();
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestGlobalPreferences()
        {
            var migrationPrefs = await _globalPreferencesClient.Get("dwreMigrate", "current");

            migrationPrefs = await _globalPreferencesClient.Update("dwreMigrate", "development",
                new OrganizationPreferences()
                {
                    MigrateToolVersion = "12"
                });

            migrationPrefs.MigrateHotfixes.Add("thisisatest");

            var newPrefs =
                await _globalPreferencesClient.Update("dwreMigrate", "development", migrationPrefs);

            Assert.Contains("thisisatest", newPrefs.MigrateHotfixes);
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestOrderSearch()
        {
            // test query for order with term query
            var result = await _orderSearchClient.SearchOrders(new SearchRequest()
            {
                Query = new TermQuery()
                {
                    QueryBody = new TermQueryBody()
                    {
                        Fields = new List<string>() {"order_no"},
                        Operator = Operator.Is,
                        Values = new List<string>() {"00080511"}
                    }
                }
            });

            // test helper method
            var result2 = await _orderSearchClient.SearchOrderByOrderNo("00080511");

            Assert.Equal(result2.OrderNo, result.Hits[0].Data.OrderNo);
        }


        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestGetOrderById()
        {
            var result = await _ordersClient.GetOrder("00080610");
        }


        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestOrderSearchByDate()
        {
            var result = await _orderSearchClient.SearchOrdersBetween(DateTime.Now.Subtract(TimeSpan.FromDays(14)), DateTime.Now);
            var total = result.Total;
            var count = 0;
            await foreach (var order in result)
            {
                count++;
            }
            Assert.Equal(total, count);
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestJobSearch()
        {
            var result = await _jobSearchClient.SearchJobsBetween(DateTime.Now.Subtract(TimeSpan.FromDays(1)), DateTime.Now);
            var total = result.Total;
            var count = 0;
            List<JobExecution> executions = new List<JobExecution>();
            await foreach (var job in result)
            {
                executions.Add(job);
                count++;
            }
            Assert.Equal(total, count);
        }
        
        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestCodeVersions()
        {
            var codeVersions = await _codeVersionsClient.GetAll();
        }
    }
}