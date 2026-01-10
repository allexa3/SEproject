using System;
using System.Security.Cryptography;

namespace HelloWorldMVC.Crypto
{
    public static class AesGcmCrypto
    {
        // Format: [1 byte version][12 bytes nonce][16 bytes tag][ciphertext]
        private const byte Version = 1;

        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("AES-256 key must be 32 bytes.");

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[plaintext.Length];

            using var aes = new AesGcm(key,16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var output = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
            output[0] = Version;
            Buffer.BlockCopy(nonce, 0, output, 1, nonce.Length);
            Buffer.BlockCopy(tag, 0, output, 1 + nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, output, 1 + nonce.Length + tag.Length, ciphertext.Length);

            return output;
        }

        public static byte[] Decrypt(byte[] input, byte[] key)
        {
            if (key == null || key.Length != 32)
                throw new ArgumentException("AES-256 key must be 32 bytes.");

            if (input == null || input.Length < 1 + 12 + 16)
                throw new ArgumentException("Invalid encrypted data.");

            if (input[0] != Version)
                throw new CryptographicException("Unsupported encryption version.");

            byte[] nonce = new byte[12];
            byte[] tag = new byte[16];
            int cipherLen = input.Length - (1 + nonce.Length + tag.Length);

            byte[] ciphertext = new byte[cipherLen];
            byte[] plaintext = new byte[cipherLen];

            Buffer.BlockCopy(input, 1, nonce, 0, nonce.Length);
            Buffer.BlockCopy(input, 1 + nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(input, 1 + nonce.Length + tag.Length, ciphertext, 0, cipherLen);

            using var aes = new AesGcm(key);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
