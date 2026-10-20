using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Application.WebApi.Helpers.Lazada
{
    public static class LazadaSignatureHelper
    {
        public static string GenerateSignature(string appSecret, string apiPath, Dictionary<string, string> parameters)
        {
            var sortedKeys = parameters.Keys.OrderBy(k => k).ToList();

            var sb = new StringBuilder();
            sb.Append(apiPath);
            foreach (var key in sortedKeys)
            {
                sb.Append(key).Append(parameters[key]);
            }

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").ToUpper();
            }
        }

        public static string GetTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        }
    }
}