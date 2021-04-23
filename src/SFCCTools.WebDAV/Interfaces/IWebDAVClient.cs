
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SFCCTools.WebDAV
{
    public class WebDAVLocation
    {
        public WebDAVLocation(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public static WebDAVLocation Impex = new WebDAVLocation("Impex");
        public static WebDAVLocation Logs = new WebDAVLocation("Logs");
        public static WebDAVLocation Cartridges = new WebDAVLocation("Cartridges");
        public static WebDAVLocation SecurityLogs = new WebDAVLocation("Securitylogs");
        public static WebDAVLocation Temp = new WebDAVLocation("Temp");
        public static WebDAVLocation RealmData = new WebDAVLocation("Realmdata");

        public override string ToString()
        {
            return Value;
        }
    }

    public interface IWebDAVClient
    {
        Task<WebDAVFile> GETInfo(string path);
        Task<WebDAVFile> GETInfo(WebDAVLocation location, string path);
        Task<WebDAVFile> GET(string filename);
        Task<WebDAVFile> GET(WebDAVLocation location, string filename);
        Task<Stream> GETStream(string path);
        Task<Stream> GETStream(WebDAVLocation location, string path);
        Task<IList<WebDAVFile>> ListDirectory(WebDAVLocation location, string directory);
        Task<bool> PUT(WebDAVLocation location, string filename, string contents);
        Task<bool> PUT(WebDAVLocation location, string filename, byte[] contents);
        Task<bool> DELETE(WebDAVLocation location, string filename);
        Task<bool> MakeDirectory(WebDAVLocation location, string directory);
        Task<bool> UpdateFile(WebDAVFile file, bool useRange = true);
        Task<bool> UNZIP(WebDAVLocation location, string filename);
        Task<bool> PUT(WebDAVLocation location, string filename, Stream contents, TransferProgressReporter.ReportProgressHandler progressHandler = null);
    }
}