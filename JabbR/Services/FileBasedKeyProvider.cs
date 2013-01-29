using System;
using System.IO;
using System.Security.Cryptography;
using JabbR.Infrastructure;

namespace JabbR.Services
{
    /// <summary>
    /// Default keys are generate on the fly and persisted to a specified folder
    /// </summary>
    public class FileBasedKeyProvider : IKeyProvider
    {
        private readonly Lazy<KeyCache> _keyCache;

        public FileBasedKeyProvider()
            : this(GetDefaultPath())
        {
        }

        public FileBasedKeyProvider(string path)
        {
            _keyCache = new Lazy<KeyCache>(() => new KeyCache(path));
        }

        public byte[] EncryptionKey
        {
            get { return _keyCache.Value.EncryptionKey; }
        }

        public byte[] VerificationKey
        {
            get { return _keyCache.Value.ValidationKey; }
        }

        private static string GetDefaultPath()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(path, "JabbR");

            Directory.CreateDirectory(path);

            return path;
        }

        private class KeyCache
        {
            public KeyCache(string path)
            {
                string keyFile = Path.Combine(path, "keyfile");

                if (File.Exists(keyFile))
                {
                    string[] lines = File.ReadAllLines(keyFile);

                    if (lines.Length == 2 && 
                        !String.IsNullOrEmpty(lines[0]) && 
                        !String.IsNullOrEmpty(lines[1]))
                    {
                        try
                        {
                            EncryptionKey = CryptoHelper.FromHex(lines[0]);
                            ValidationKey = CryptoHelper.FromHex(lines[1]);
                        }
                        catch
                        {
                            // If we failed to read the file for some reason just swallow the exception
                        }
                    }
                }

                if (EncryptionKey == null || ValidationKey == null)
                {
                    EncryptionKey = GenerateRandomKey();
                    ValidationKey = GenerateRandomKey();

                    File.WriteAllLines(keyFile, new[] { 
                        CryptoHelper.ToHex(EncryptionKey),
                        CryptoHelper.ToHex(ValidationKey)
                    });
                }
            }

            public byte[] EncryptionKey { get; set; }
            public byte[] ValidationKey { get; set; }

            /// <summary>
            /// Generates a 256 bit random key
            /// </summary>
            private static byte[] GenerateRandomKey()
            {
                using (var crypto = new RNGCryptoServiceProvider())
                {
                    var buffer = new byte[32];
                    crypto.GetBytes(buffer);
                    return buffer;
                }
            }
        }
    }
}