using System;
using System.IO;
using System.Text;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using SFCCTools.WebDAV;
using Xunit.Abstractions;

namespace SFCCTools.FunctionalTests.WebDAV
{
    public class TestSFCCWebDAVClient : IClassFixture<SFCCEnvironmentFixture>
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly IWebDAVClient _client;

        public TestSFCCWebDAVClient(SFCCEnvironmentFixture fixture, ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _client = fixture.ServiceProvider.GetService<IWebDAVClient>();
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestMakeDirectory()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            var directory = await _client.GET(WebDAVLocation.Impex, "src/newdirectory");
            Assert.True(directory.Exists);
            Assert.True(directory.IsDirectory);
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory"));
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestCreateFileViaString()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json", "This is a test"));
            var file = await _client.GET(WebDAVLocation.Impex, "src/newdirectory/test.json");
            Assert.True(file.Exists);
            Assert.True(file.IsFile);
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory/test.json"));
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory"));
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestCreateFileViaBytes()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json",
                Encoding.UTF8.GetBytes("This is a test")));
            var file = await _client.GET(WebDAVLocation.Impex, "src/newdirectory/test.json");
            Assert.True(file.Exists);
            Assert.True(file.IsFile);
            Assert.Equal("This is a test", file.ContentsAsString());
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory/test.json"));
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory"));
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestListDirectoryContents()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json",
                Encoding.UTF8.GetBytes("This is a test")));
            var file = await _client.GET(WebDAVLocation.Impex, "src/newdirectory/test.json");
            var directory = await _client.ListDirectory(WebDAVLocation.Impex, "src/newdirectory");
            Assert.Equal(1, directory.Count);
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory/test.json"));
            Assert.True(await _client.DELETE(WebDAVLocation.Impex, "src/newdirectory"));
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestUpdatingFile()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json",
                Encoding.UTF8.GetBytes("This is a test")));
            var file = await _client.GET(WebDAVLocation.Impex, "src/newdirectory/test.json");

            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json",
                Encoding.UTF8.GetBytes("This is a test\nthis is another test")));

            var updated = await _client.UpdateFile(file);
            Assert.True(updated);
            Assert.Equal("This is a test\nthis is another test", file.ContentsAsString());
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestUnchangedFileShouldNotUpdate()
        {
            Assert.True(await _client.MakeDirectory(WebDAVLocation.Impex, "src/newdirectory"));
            Assert.True(await _client.PUT(WebDAVLocation.Impex, "src/newdirectory/test.json",
                Encoding.UTF8.GetBytes("This is a test")));

            var file = await _client.GET(WebDAVLocation.Impex, "src/newdirectory/test.json");
            var updated = await _client.UpdateFile(file);
            Assert.False(updated);
            Assert.Equal("This is a test", file.ContentsAsString());
        }

        [Trait("Category", "RequiresInstance")]
        [Fact]
        public async void TestStreamingOfContent()
        {
            var random = new Random();
            var buffer = new byte[1_000_000];
            using (var ms = new MemoryStream())
            {
                // fill a large buffer with random data
                for (var i = 0; i < 50; i++)
                {
                    random.NextBytes(buffer);
                    ms.Write(buffer);
                }
                
                var startTime = DateTime.Now;

                var putTask = await _client.PUT(WebDAVLocation.Cartridges, "testdata", ms, (sender, progress, size) =>
                {
                    double percent = (double)progress / (double)size * 100;
                });
                
                _outputHelper.WriteLine($"Total seconds to stream 50million bytes: {DateTime.Now.Subtract(startTime).TotalSeconds}");
            }
        }
    }
}