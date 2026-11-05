using System;
using System.Security.Cryptography;
using System.Text;

namespace CarParkManager
{
    internal class PasswordHelper
    {
        public static string getHashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

                return Convert.ToBase64String(hash);
            }
        }

        public static bool Verify(string password, string expectedHash)
        {
            return getHashPassword(password) == expectedHash;
        }
    }
}
