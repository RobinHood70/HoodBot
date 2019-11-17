namespace RobinHood70.HoodBot
{
	// This class taken from https://thomasfreudenberg.com/archive/2017/02/11/encrypting-values-when-serializing-with-json-net/. It has been modified to use my built-in functions, where available, and to follow my own formatting and naming conventions.
	using System;
	using System.IO;
	using System.Security.Cryptography;
	using System.Text;
	using Newtonsoft.Json;
	using static RobinHood70.WikiCommon.Globals;

	public class EncryptingJsonConverter : JsonConverter
	{
		private readonly byte[] encryptionKeyBytes;

		public EncryptingJsonConverter(string encryptionKey)
		{
			// Hash the key to ensure it is exactly 256 bits long, as required by AES-256
			ThrowNull(encryptionKey, nameof(encryptionKey));
			using var sha = new SHA256Managed();
			this.encryptionKeyBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			ThrowNull(writer, nameof(writer));
			var stringValue = value as string;
			if (string.IsNullOrEmpty(stringValue))
			{
				writer.WriteNull();
				return;
			}

			using var outputStream = new MemoryStream();
			using var aes = new AesManaged
			{
				Key = this.encryptionKeyBytes
			};
			var iv = aes.IV; // first access generates a new IV
			outputStream.Write(iv, 0, iv.Length);
			outputStream.Flush();

			var buffer = Encoding.UTF8.GetBytes(stringValue);
			using (var inputStream = new MemoryStream(buffer, false))
			using (var encryptor = aes.CreateEncryptor(this.encryptionKeyBytes, iv))
			using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
			{
				inputStream.CopyTo(cryptoStream);
			}

			writer.WriteValue(Convert.ToBase64String(outputStream.ToArray()));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Too many possible errors and handling is the same for all. Also, not my code.")]
		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			ThrowNull(reader, nameof(reader));
			var value = reader.Value as string;
			if (string.IsNullOrEmpty(value))
			{
				return reader.Value;
			}

			var decryptedValue = string.Empty;
			try
			{
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

				decryptedValue = Encoding.UTF8.GetString(outputStream.ToArray());
			}
			catch
			{
			}

			return decryptedValue;
		}

		/// <inheritdoc />
		public override bool CanConvert(Type objectType) => objectType == typeof(string);
	}
}