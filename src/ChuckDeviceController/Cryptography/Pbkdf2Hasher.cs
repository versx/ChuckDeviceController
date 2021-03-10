namespace ChuckDeviceController.Cryptography
{
    using System;
    using System.Security.Cryptography;

    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    public static class Pbkdf2Hasher
    {
        public static string ComputeHash(string password, byte[] salt, int iterations = 10000)
        {
            return Convert.ToBase64String
            (
                KeyDerivation.Pbkdf2
                (
                    password: password,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA1,
                    iterationCount: iterations,
                    numBytesRequested: 256 / 8
                )
            );
        }

        public static byte[] GenerateRandomSalt()
        {
            var salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            return salt;
        }

        public static string GeneratePassword(int length = 32)
        {
            var cryptRNG = new RNGCryptoServiceProvider();
            var tokenBuffer = new byte[length];
            cryptRNG.GetBytes(tokenBuffer);
            return Convert.ToBase64String(tokenBuffer).Remove(length);
        }
    }
}