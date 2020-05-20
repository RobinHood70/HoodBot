namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Formats a Request object as multipart (<see cref="MultipartResult"/>) data.</summary>
	public class RequestVisitorMultipart : IParameterVisitor
	{
		#region Constants
		private const int RandomBoundaryStartLength = 4;
		private const bool ScanBoundaryConflicts = true;
		#endregion

		#region Static Fields
		private static readonly Encoding CurrentEncoding = Encoding.UTF8;
		private static readonly char[] BoundaryChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'()+_,-./:=? ".ToCharArray();
		#endregion

		#region Fields
		private bool badBoundary;
		private string boundary;
		private bool supportsUnitSeparator;
		private MemoryStream? stream;
		#endregion

		#region Constructors
		private RequestVisitorMultipart() => this.boundary = RandomBoundary(RandomBoundaryStartLength);
		#endregion

		#region Public Static Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, as they would be used in a URL or POST data.</returns>
		public static MultipartResult Build(Request request)
		{
			// TODO: Rewrite to use request.Build().
			ThrowNull(request, nameof(request));
			var visitor = new RequestVisitorMultipart
			{
				supportsUnitSeparator = request.SupportsUnitSeparator,
			};

			// Boundary is kept short at the expense of scanning data and possibly having to regenerate boundary multiple times until a valid one is found. Wiki uploads are generally < 1MB, however, so this is not expected to be a major concern. Each time one is rejected, boundary length is increased to reduce chance of another rejection.
			var formData = Array.Empty<byte>();
			string contentType;
			var boundaryLength = RandomBoundaryStartLength;
			do
			{
				visitor.badBoundary = false;
				contentType = "multipart/form-data; boundary=\"" + visitor.boundary + "\"";

				using var memoryStream = new MemoryStream();
				visitor.stream = memoryStream;
				var first = true;
				foreach (var parameter in request)
				{
					if (!first)
					{
						memoryStream.Write(CurrentEncoding.GetBytes("\r\n"), 0, CurrentEncoding.GetByteCount("\r\n"));
					}

					first = false;
					parameter.Accept(visitor);
					if (visitor.badBoundary)
					{
						break;
					}
				}

				if (!visitor.badBoundary)
				{
					var footer = "\r\n--" + visitor.boundary + "--\r\n";
					memoryStream.Write(CurrentEncoding.GetBytes(footer), 0, CurrentEncoding.GetByteCount(footer));
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

					visitor.SetRandomBoundary(boundaryLength);
				}
			}
			while (visitor.badBoundary);

			return new MultipartResult(contentType, formData);
		}

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		public void Visit(FileParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			if (ScanBoundaryConflicts && parameter.Value.Data.LongLength > 0 && CurrentEncoding.GetString(parameter.Value.Data).Contains(this.boundary, StringComparison.Ordinal))
			{
				this.badBoundary = true;
				return;
			}

			ThrowNull(this.stream, nameof(RequestVisitorMultipart), nameof(this.stream));
			var data = Invariant($"--{this.boundary}\r\nContent-Disposition: form-data; name=\"{parameter.Name}\"; filename=\"{parameter.Value.FileName}\";\r\nContent-Type: application/octet-stream\r\n\r\n");
			this.stream!.Write(CurrentEncoding.GetBytes(data), 0, CurrentEncoding.GetByteCount(data));
			this.stream.Write(parameter.Value.Data, 0, parameter.Value.Data.Length);
		}

		/// <summary>Visits the specified FormatParameter object.</summary>
		/// <param name="parameter">The FormatParameter object.</param>
		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}

		/// <summary>Visits the specified HiddenParameter object.</summary>
		/// <param name="parameter">The HiddenParameter object.</param>
		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <typeparam name="T">An enumerable string collection.</typeparam>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.TextMultipart(parameter.Name, value);
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.TextMultipart(parameter.Name, parameter.Value);
		}
		#endregion

		#region Public Methods

		/// <summary>Sets the boundary to a random string of characters of the given length.</summary>
		/// <param name="boundaryLength">Length of the boundary.</param>
		public void SetRandomBoundary(int boundaryLength) => this.boundary = RandomBoundary(boundaryLength);
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
				var scanLength = i == 69 ? BoundaryChars.Length - 1 : BoundaryChars.Length;
				builder.Append(BoundaryChars[rand.Next(scanLength)]);
			}

			return builder.ToString();
		}

		private void TextMultipart(string name, string value)
		{
			if (ScanBoundaryConflicts && (value?.Contains(this.boundary, StringComparison.Ordinal) ?? false))
			{
				this.badBoundary = true;
				return;
			}

			ThrowNull(this.stream, nameof(RequestVisitorMultipart), nameof(this.stream));
			var postData = Invariant($"--{this.boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}");
			this.stream!.Write(CurrentEncoding.GetBytes(postData), 0, CurrentEncoding.GetByteCount(postData));
		}
		#endregion
	}
}