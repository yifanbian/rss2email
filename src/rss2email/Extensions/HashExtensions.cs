using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace RssToEmail
{
    public static class HashExtensions
    {
        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "Usage not related to security")]
        public static string GetBase64EncodedSha1(this string s)
        {
            using var hasher = new SHA1Managed();
            return Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(s)));
        }
    }
}
