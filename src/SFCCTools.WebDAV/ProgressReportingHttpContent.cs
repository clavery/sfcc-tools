using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SFCCTools.WebDAV
{
    public class TransferProgressReporter
    {
        public delegate void ReportProgressHandler(object sender, long progress, long totalSize);

        public event ReportProgressHandler Handler;

        public void ReportProgress(long progress, long size)
        {
            Handler?.Invoke(this, progress, size);
        }
    }

    /// <summary>
    /// HttpContent implementation that uses a Stream and emits progress
    /// events at intervals determined by the buffer size (default 500kb)
    /// </summary>
    class ProgressReportingHttpContent : HttpContent
    {
        private readonly Stream _content;
        private readonly int _bufferSize;
        private readonly TransferProgressReporter _reporter;
        private const int defaultBufferSize = 1024 * 500;

        public ProgressReportingHttpContent(Stream content, TransferProgressReporter reporter) : this(content,
            defaultBufferSize, reporter)
        {
        }

        public ProgressReportingHttpContent(Stream content, int bufferSize, TransferProgressReporter reporter)
        {
            if (content == null)
            {
                throw new ArgumentNullException("content");
            }

            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException("bufferSize");
            }

            _content = content;
            _bufferSize = bufferSize;
            _reporter = reporter;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            Contract.Assert(stream != null);

            _content.Position = 0;

            var buffer = new byte[_bufferSize];
            var size = _content.Length;
            var transferred = 0;

            _reporter.ReportProgress(transferred, size);
            while (true)
            {
                var length = await _content.ReadAsync(buffer, 0, buffer.Length);
                if (length <= 0) break;
                
                await stream.WriteAsync(buffer, 0, length);
                
                transferred += length;
                _reporter.ReportProgress(transferred, size);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }
        
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}