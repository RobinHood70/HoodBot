namespace RobinHood70.WallE.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	internal class RequestVisitorMultipart : IParameterVisitor
	{
		#region Constants
		private const int RandomBoundaryStartLength = 4;
		private const bool ScanBoundaryConflicts = true;
		#endregion

		#region Static Fields
		private static Encoding encoding = Encoding.UTF8;
		private static char[] boundaryChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'()+_,-./:=? ".ToCharArray();
		#endregion

		#region Fields
		private bool badBoundary;
		private string boundary;
		private bool supportsUnitSeparator;
		private MemoryStream stream;
		#endregion

		#region Constructors
		private RequestVisitorMultipart()
		{
		}
		#endregion

		#region Public Static Methods
		public static MultipartResult Build(Request request)
		{
			var visitor = new RequestVisitorMultipart
			{
				supportsUnitSeparator = request.SupportsUnitSeparator,
			};

			// Boundary is kept short at the expense of scanning data and possibly having to regenerate boundary multiple times until a valid one is found. Wiki uploads are generally < 1MB, however, so this is not expected to be a major concern. Each time one is rejected, boundary length is increased to reduce chance of another rejection.
			var formData = new byte[0];
			string contentType;
			var boundaryLength = RandomBoundaryStartLength;
			do
			{
				visitor.badBoundary = false;
				visitor.boundary = RandomBoundary(boundaryLength);
				contentType = "multipart/form-data; boundary=\"" + visitor.boundary + "\"";

				using (var memoryStream = new MemoryStream())
				{
					visitor.stream = memoryStream;
					var first = false;
					foreach (var parameter in request)
					{
						if (first)
						{
							memoryStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));
						}

						first = true;
						parameter.Accept(visitor);
						if (visitor.badBoundary)
						{
							break;
						}
					}

					if (!visitor.badBoundary)
					{
						var footer = "\r\n--" + visitor.boundary + "--\r\n";
						memoryStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));
						formData = new byte[memoryStream.Length];
						memoryStream.Position = 0;
						memoryStream.Read(formData, 0, formData.Length);
					}
					else
					{
						if (boundaryLength < 70)
						{
							boundaryLength++;
						}
					}
				}
			}
			while (visitor.badBoundary);

			return new MultipartResult(contentType, formData);
		}

		public void Visit(FileParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			if (ScanBoundaryConflicts && parameter.Value.LongLength > 0 && encoding.GetString(parameter.Value).Contains(this.boundary))
			{
				this.badBoundary = true;
				return;
			}

			var data = Invariant((FormattableString)$"--{this.boundary}\r\nContent-Disposition: form-data; name=\"{parameter.Name}\"; filename=\"{parameter.FileName}\";\r\nContent-Type: application/octet-stream\r\n\r\n");
			this.stream.Write(encoding.GetBytes(data), 0, encoding.GetByteCount(data));
			this.stream.Write(parameter.Value, 0, parameter.Value.Length);
		}

		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}

		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}

		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = BuildPipedValue(parameter, this.supportsUnitSeparator);
			this.TextMultipart(parameter.Name, value);
		}

		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}
		#endregion

		#region Private Methods
		private static string RandomBoundary(int boundaryLength)
		{
			var builder = new StringBuilder();
			var rand = new Random();

			// Sanity check
			if (boundaryLength > 70)
			{
				boundaryLength = 70;
			}

			for (var i = 0; i < boundaryLength; i++)
			{
				var scanLength = i == 69 ? boundaryChars.Length - 1 : boundaryChars.Length;
				builder.Append(boundaryChars[rand.Next(scanLength)]);
			}

			return builder.ToString();
		}

		private void TextMultipart(string name, string value)
		{
			if (ScanBoundaryConflicts && (value?.Contains(this.boundary) ?? false))
			{
				this.badBoundary = true;
				return;
			}

			var postData = Invariant((FormattableString)$"--{this.boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}");
			this.stream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
		}
		#endregion
	}
}