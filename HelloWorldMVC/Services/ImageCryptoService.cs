using System;
using Microsoft.Extensions.Configuration;
using HelloWorldMVC.Crypto;

namespace HelloWorldMVC.Services
{
    public class ImageCryptoService
    {
        private readonly byte[] _aesKey;

        // 👇 THIS is the constructor you were asking about
        public ImageCryptoService(IConfiguration configuration)
        {
            var keyBase64 = configuration["Crypto:AesKeyBase64"];

            if (string.IsNullOrWhiteSpace(keyBase64))
                throw new InvalidOperationException("Encryption key not found in Key Vault.");

            _aesKey = Convert.FromBase64String(keyBase64);

            if (_aesKey.Length != 32)
                throw new InvalidOperationException("Invalid AES-256 key length.");
        }

        public byte[] Encrypt(byte[] imageBytes)
        {
            return AesGcmCrypto.Encrypt(imageBytes, _aesKey);
        }

        public byte[] Decrypt(byte[] encryptedBytes)
        {
            return AesGcmCrypto.Decrypt(encryptedBytes, _aesKey);
        }
    }
}
