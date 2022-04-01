namespace RobinHood70.HoodBot
{
	// This class is based on https://thomasfreudenberg.com/archive/2017/02/11/encrypting-values-when-serializing-with-json-net/.
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using System.Text;
	using RobinHood70.CommonCode;

	internal sealed class TextEncrypter
	{
		private readonly byte[] encryptionKeyBytes;

		public TextEncrypter(string encryptionKey)
		{
			// Hash the key to ensure it is exactly 256 bits long, as required by AES-256
			using SHA256 sha = SHA256.Create();
			this.encryptionKeyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey.NotNull()));
		}

		public string Encrypt(string value)
		{
			using MemoryStream outputStream = new();
			using Aes aes = Aes.Create();
			aes.Key = this.encryptionKeyBytes;
			var iv = aes.IV; // first access generates a new IV
			outputStream.Write(iv, 0, iv.Length);
			outputStream.Flush();

			var buffer = Encoding.UTF8.GetBytes(value);
			using (MemoryStream inputStream = new(buffer, false))
			using (var encryptor = aes.CreateEncryptor())
			using (CryptoStream cryptoStream = new(outputStream, encryptor, CryptoStreamMode.Write))
			{
				inputStream.CopyTo(cryptoStream);
			}

			return Convert.ToBase64String(outputStream.ToArray());
		}

		public string Decrypt(string value)
		{
			var buffer = Convert.FromBase64String(value.NotNull());
			using MemoryStream inputStream = new(buffer, false);
			var iv = new byte[16];
			var bytesRead = inputStream.Read(iv, 0, 16);
			if (bytesRead < 16)
			{
				throw new CryptographicException("IV is missing or invalid.");
			}

			using MemoryStream outputStream = new();
			using Aes aes = Aes.Create();
			aes.Key = this.encryptionKeyBytes;
			using (var decryptor = aes.CreateDecryptor(this.encryptionKeyBytes, iv))
			using (CryptoStream cryptoStream = new(inputStream, decryptor, CryptoStreamMode.Read))
			{
				cryptoStream.CopyTo(outputStream);
			}

			return Encoding.UTF8.GetString(outputStream.ToArray());
		}
	}
}