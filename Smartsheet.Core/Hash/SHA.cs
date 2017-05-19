using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings;

namespace Smartsheet.Core
{
	public static class SHA
	{
		public static string GenerateSHA256String(string inputString)
		{
			SHA256 sha256 = SHA256.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(inputString);
			byte[] hash = sha256.ComputeHash(bytes);
			return GetStringFromHash(hash);
		}

		private static string GetStringFromHash(byte[] hash)
		{
			StringBuilder result = new StringBuilder();

			for (int i = 0; i < hash.Length; i++)
			{
				result.Append(hash[i].ToString("X2"));
			}

			return result.ToString();
		}

	}
}
