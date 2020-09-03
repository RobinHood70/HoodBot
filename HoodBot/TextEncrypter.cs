namespace RobinHood70.HoodBot
{
	// This class is based on https://thomasfreudenberg.com/archive/2017/02/11/encrypting-values-when-serializing-with-json-net/.
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	internal class TextEncrypter
	{
		private readonly byte[] encryptionKeyBytes;

		public TextEncrypter(string encryptionKey)
		{
			// Hash the key to ensure it is exactly 256 bits long, as required by AES-256
			ThrowNull(encryptionKey, nameof(encryptionKey));
			using var sha = new SHA256Managed();
			this.encryptionKeyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
		}

		public string Encrypt(string value)
		{
			using var outputStream = new MemoryStream();
			using var aes = new AesManaged
			{
				Key = this.encryptionKeyBytes
			};
			var iv = aes.IV; // first access generates a new IV
			outputStream.Write(iv, 0, iv.Length);
			outputStream.Flush();

			var buffer = Encoding.UTF8.GetBytes(value);
			using (var inputStream = new MemoryStream(buffer, false))
			using (var encryptor = aes.CreateEncryptor(this.encryptionKeyBytes, iv))
			using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
			{
				inputStream.CopyTo(cryptoStream);
			}

			return Convert.ToBase64String(outputStream.ToArray());
		}

		public string Decrypt(string value)
		{
			ThrowNull(value, nameof(value));
			var buffer = Convert.FromBase64String(value);
			using var inputStream = new MemoryStream(buffer, false);
			var iv = new byte[16];
			var bytesRead = inputStream.Read(iv, 0, 16);
			if (bytesRead < 16)
			{
				throw new CryptographicException("IV is missing or invalid.");
			}

			using var outputStream = new MemoryStream();
			using var aes = new AesManaged
			{
				Key = this.encryptionKeyBytes
			};
			using (var decryptor = aes.CreateDecryptor(this.encryptionKeyBytes, iv))
			using (var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
			{
				cryptoStream.CopyTo(outputStream);
			}

			return Encoding.UTF8.GetString(outputStream.ToArray());
		}
	}
}