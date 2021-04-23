using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using SFCCTools.Core.Cartridges;
using Xunit;

namespace SFCCTools.FunctionalTests.Cartridges
{
    public class TestCartridgeHelper
    {
        /// <summary>
        /// Find cartridges in testdata/cartridges test harness
        /// </summary>
        [Fact]
        public void TestFindsCartridges()
        {
            var cartridges = CartridgeHelper.FindAllInDirectory("testdata/cartridges");
            Assert.Equal(1, cartridges.Count);
            Assert.Equal("app_something", cartridges[0].Name);
        }

        [Fact]
        public void TestCreatesZipFile()
        {
            using (var memory = new MemoryStream())
            {
                var cartridges = CartridgeHelper.FindAllInDirectory("testdata/cartridges");
                CartridgeHelper.CartridgesToZipFile(cartridges, "testing", memory);
                using (var outputFile = new StreamWriter("testfile.zip"))
                {
                    memory.WriteTo(outputFile.BaseStream);
                }
            }
            using (var inputFile = new StreamReader("testfile.zip"))
            {
                var zipFile = new ZipArchive(inputFile.BaseStream, ZipArchiveMode.Read);
                var entry = zipFile.GetEntry("testing/app_something/.project");
                Assert.NotNull(entry);
                entry = zipFile.GetEntry("foo");
                Assert.Null(entry);
                entry = zipFile.GetEntry("testing/app_something/cartridge/controllers/TestController.js");
                Assert.NotNull(entry);
                using (var entryStream = new StreamReader(entry.Open()))
                {
                    var contents = entryStream.ReadToEnd();
                    Assert.Equal("\"hello world\";", contents.TrimEnd());
                }
            }
        }
    }
}