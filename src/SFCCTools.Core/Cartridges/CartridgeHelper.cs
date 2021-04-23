using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace SFCCTools.Core.Cartridges
{
    public class CartridgeHelper
    {
        /// <summary>
        /// Finds cartridges recursively in given directory
        /// Cartridge must contain a .project file
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static IList<Cartridge> FindAllInDirectory(string directory)
        {
            var projectFiles = Directory.GetFiles(directory, ".project", SearchOption.AllDirectories);
            List<Cartridge> cartridges = new List<Cartridge>();

            foreach (var projectFile in projectFiles)
            {
                var location = Path.GetFullPath(Path.GetDirectoryName(projectFile));
                if (location.Contains("node_modules"))
                {
                    continue;
                }
                var cartridgeName = Path.GetFileName(location);
                cartridges.Add(new Cartridge()
                {
                    Path = location,
                    Name = cartridgeName
                });
            }

            return cartridges;
        }

        /// <summary>
        /// Creates a zip archive of all cartridges and contents in the given location
        /// written to the given stream
        /// </summary>
        /// <param name="location"></param>
        /// <param name="outputStream"></param>
        public static void CartridgesToZipFile(IList<Cartridge> cartridges, string archiveRoot, Stream outputStream)
        {
            var zipFile = new ZipArchive(outputStream, ZipArchiveMode.Create, true);

            foreach (var cartridge in cartridges)
            {
                var cartridgeFiles = Directory.GetFiles(cartridge.Path, "*.*", SearchOption.AllDirectories);
                var parentDir = Path.GetDirectoryName(cartridge.Path);
                foreach (var cartridgeFile in cartridgeFiles)
                {
                    var normalizedEntryName = cartridgeFile.Substring(parentDir.Length);
                    if (normalizedEntryName.StartsWith(Path.DirectorySeparatorChar))
                    {
                        normalizedEntryName = normalizedEntryName.Substring(1);
                    }

                    normalizedEntryName = Path.Join(archiveRoot, normalizedEntryName);
                    zipFile.CreateEntryFromFile(cartridgeFile, normalizedEntryName, CompressionLevel.Optimal);
                }
            }

            zipFile.Dispose();
        }
    }
}