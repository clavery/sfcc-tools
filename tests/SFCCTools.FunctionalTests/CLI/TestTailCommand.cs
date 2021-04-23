using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Serilog.Core;
using SFCCTools.CLI;
using SFCCTools.CLI.Commands;
using SFCCTools.WebDAV;
using Xunit;
using Xunit.Abstractions;

namespace SFCCTools.FunctionalTests
{
    public class TestTailCommand
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestTailCommand(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        // TODO: refactor this out to a shared fixture
        private Mock<IWebDAVClient> GetMockClient()
        {
            var mockWebDavClient = new Mock<IWebDAVClient>();

            mockWebDavClient.Setup(x => x.ListDirectory(WebDAVLocation.Logs, It.IsAny<string>())).ReturnsAsync(
                new List<WebDAVFile>()
                {
                    new WebDAVFile()
                    {
                        Filename = "customerror-foobar"
                    }
                });

            var firstCall = true;
            mockWebDavClient.Setup(
                    (x => x.UpdateFile(It.Is<WebDAVFile>(file => file.Filename == "customerror-foobar"),
                        true)))
                .ReturnsAsync(new Func<WebDAVFile, bool, bool>((file, useRange) =>
                {
                    var contents = Encoding.UTF8.GetBytes(
                        "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  Error 1\n" +
                        "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  ReferenceError: config is not defined\n");

                    if (!firstCall)
                    {
                        // send 2 more logs on the send update call
                        contents = Encoding.UTF8.GetBytes(
                            "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  Error 1\n" +
                            "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  ReferenceError: config is not defined\n" +
                            "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  Error 2\n" +
                            "[2020-02-14 19:31:01.605 GMT] ERROR PipelineCallServlet|6394638|Sites-Acme-Site|COBilling-Start|PipelineCall|4I69HLdLHj custom.isml []  Error 3\n");
                    }

                    file.Contents = contents;
                    file.Length = contents.Length;
                    firstCall = false;
                    return true;
                }));
            return mockWebDavClient;
        }

        private Mock<IConsoleOutput> GetMockConsole()
        {
            var mockConsole = new Mock<IConsoleOutput>();
            mockConsole.Setup(x => x.WriteLine(It.IsAny<string>())).Verifiable();
            return mockConsole;
        }

        [Fact]
        public async void TestEnumeratesLogsAndOutputsLastLine()
        {
            var mockConsole = GetMockConsole();
            var mockWebDavClient = GetMockClient();
            var log = new Logger<TailCommand>(new NullLoggerFactory());

            mockConsole.Setup(x => x.WriteLine(It.IsAny<string>())).Verifiable();

            var tailCommand = new TailCommand(log, mockConsole.Object, mockWebDavClient.Object);
            var result = tailCommand.RunCommand(new CancellationToken(), new List<string>());

            // TODO refactor to send cancellation after verifying
            await Task.Delay(100);
            mockWebDavClient.Verify(x => x.ListDirectory(WebDAVLocation.Logs, ""), Times.Once);
            mockWebDavClient.Verify(x => x.UpdateFile(It.IsAny<WebDAVFile>(), true), Times.Once);
            // The first log should never be output as it is never the latest
            mockConsole.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("Error 1"))), Times.Never);
            mockConsole.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("config is not defined"))), Times.Once);
        }

        [Fact]
        public async void TestTailsLogsAfterUpdate()
        {
            var mockConsole = GetMockConsole();
            var mockWebDavClient = GetMockClient();
            var log = new Logger<TailCommand>(new NullLoggerFactory());

            var tailCommand = new TailCommand(log, mockConsole.Object, mockWebDavClient.Object);
            var result = tailCommand.RunCommand(new CancellationToken(), new List<string>(), interval: 300);

            // after the delay an update round should have occurred
            await Task.Delay(400);

            mockWebDavClient.Verify(x => x.UpdateFile(It.IsAny<WebDAVFile>(), true), Times.Exactly(2));
            mockConsole.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("config is not defined"))),
                Times.Once);
            mockConsole.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("Error 2"))), Times.Once);
            mockConsole.Verify(x => x.WriteLine(It.Is<string>(s => s.Contains("Error 3"))), Times.Once);
        }
    }
}