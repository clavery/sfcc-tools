using System;
using System.Text;
using System.Xml.Serialization;

namespace SFCCTools.WebDAV
{
    // Represents a File or Directory on WebDAV
    // May not exist
    public class WebDAVFile
    {
        public string URI { get; set; }
        public string Filename { get; set; }
        public bool Exists { get; set; }

        public bool HasContents => Contents != null;

        public byte[] Contents { get; set; }

        /// <summary>
        /// Read the contents of this file as a string
        /// </summary>
        /// <param name="offset">offset within the byte stream (defaults to the start)</param>
        /// <param name="encoding">defaults to UTF8</param>
        /// <returns></returns>
        public string ContentsAsString(int offset=0, Encoding encoding = null)
        {
            if (!HasContents) return null;
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(Contents[new Range(offset, Length)]);
        }

        public bool IsFile { get; set; }

        public bool IsDirectory => !IsFile; // This isn't perfect but works well enough

        public DateTime CreationDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        // TODO: change this to support content length and known length??
        public int Length { get; set; }
        public string ContentType { get; set; }
        public string ETag { get; set; }
    }
}