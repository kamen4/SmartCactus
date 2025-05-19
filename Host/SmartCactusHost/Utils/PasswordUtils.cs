using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Utils;

public static class PasswordUtils
{
    public static byte[] Hash(string password, RandomNumberGenerator rng)
    {
        const KeyDerivationPrf Pbkdf2Prf = KeyDerivationPrf.HMACSHA1;
        const int Pbkdf2IterCount = 1000;
        const int Pbkdf2SubkeyLength = 256 / 8;
        const int SaltSize = 128 / 8;

        byte[] salt = new byte[SaltSize];
        rng.GetBytes(salt);
        byte[] subkey = KeyDerivation.Pbkdf2(password, salt, Pbkdf2Prf, Pbkdf2IterCount, Pbkdf2SubkeyLength);

        var outputBytes = new byte[1 + SaltSize + Pbkdf2SubkeyLength];
        outputBytes[0] = 0x00;
        Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
        Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, Pbkdf2SubkeyLength);
        return outputBytes;
    }

    public static bool Verify(byte[] hashedPassword, string password)
    {
        const KeyDerivationPrf Pbkdf2Prf = KeyDerivationPrf.HMACSHA1;
        const int Pbkdf2IterCount = 1000;
        const int Pbkdf2SubkeyLength = 256 / 8;
        const int SaltSize = 128 / 8;

        if (hashedPassword.Length != 1 + SaltSize + Pbkdf2SubkeyLength)
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        Buffer.BlockCopy(hashedPassword, 1, salt, 0, salt.Length);

        byte[] expectedSubkey = new byte[Pbkdf2SubkeyLength];
        Buffer.BlockCopy(hashedPassword, 1 + salt.Length, expectedSubkey, 0, expectedSubkey.Length);

        byte[] actualSubkey = KeyDerivation.Pbkdf2(password, salt, Pbkdf2Prf, Pbkdf2IterCount, Pbkdf2SubkeyLength);
        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }

    public static string GeneratePassword(int length = 8,
        bool includeLowercase = true,
        bool includeUppercase = true,
        bool includeDigits = true,
        bool includeSpecialChars = true)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+[]{};:,.<>?/";

        StringBuilder charPool = new();
        if (includeLowercase) charPool.Append(lowercase);
        if (includeUppercase) charPool.Append(uppercase);
        if (includeDigits) charPool.Append(digits);
        if (includeSpecialChars) charPool.Append(special);

        if (charPool.Length == 0)
            throw new ArgumentException("At least one symbol type must be included.");

        Random rng = new((int)DateTime.Now.ToFileTime());
        StringBuilder password = new();

        for (int i = 0; i < length; i++)
        {
            int index = rng.Next(charPool.Length);
            password.Append(charPool[index]);
        }

        return password.ToString();
    }
}
