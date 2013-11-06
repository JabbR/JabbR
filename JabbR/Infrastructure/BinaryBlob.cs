using System;
using System.IO;

namespace JabbR.Infrastructure
{
    public class BinaryBlob
    {
        public string ContentType { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// Parses a data uri
        /// </summary>
        public static BinaryBlob Parse(string dataUri)
        {
            var blob = new BinaryBlob();

            var reader = new StringReader(dataUri);

            // data:[<MIME-type>][;charset=<encoding>][;base64],<data>
            reader.Expect("data:");
            blob.ContentType = reader.ReadUntil(ch => ch == ';').Trim();
            reader.Read();
            reader.Expect("base64,");
            blob.Data = Convert.FromBase64String(reader.ReadToEnd());

            return blob;
        }
    }
}
