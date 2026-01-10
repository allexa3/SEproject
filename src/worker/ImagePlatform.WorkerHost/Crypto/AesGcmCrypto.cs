using System;
using System.Security.Cryptography;

namespace ImagePlatform.WorkerHost.Crypto
{
    public static class AesGcmCrypto
    {
        private const byte Version = 1;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        public static byte[] Decrypt(byte[] input, byte[] key)
        {
            if (key is null || key.Length != 32) throw new ArgumentException("AES-256 key must be 32 bytes.");
            if (input is null || input.Length < 1 + NonceSize + TagSize) throw new ArgumentException("Ciphertext too short.");
            if (input[0] != Version) throw new CryptographicException("Unsupported encryption version.");

            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            int ctLen = input.Length - (1 + NonceSize + TagSize);

            byte[] ciphertext = new byte[ctLen];
            byte[] plaintext = new byte[ctLen];

            Buffer.BlockCopy(input, 1, nonce, 0, NonceSize);
            Buffer.BlockCopy(input, 1 + NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(input, 1 + NonceSize + TagSize, ciphertext, 0, ctLen);

            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
    }
}
