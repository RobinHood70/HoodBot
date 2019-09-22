#pragma warning disable CA1303 // Do not pass literals as localized parameters
// Class will soon be deprecated, so ignore the warnings.
namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.WikiCommon;

	/// <summary>This is a helper class to parse parameter text into either a single parameter or a collection of <see cref="Parameter"/>s.</summary>
	/// <remarks>Both the single parameter and collection will be populated after Parse is called, regardless of the number of parameters. If multiple parameters are present, the value in <see cref="SingleParameter"/> will treat everything after the name as a single value, regardless of the presence of pipes or equals signs (as is done in MediaWiki for links outside File space).</remarks>
	public class ParameterParser
	{
		#region Static Fields
		private static readonly Delimiter[] AllDelimiters =
		{
				new Delimiter("<!--", "-->", TokenType.WhiteSpace, true),
				new Delimiter("<nowiki>", "</nowiki>", TokenType.Value, true),
				new Delimiter("{{{", "}}}", TokenType.Value, false),
				new Delimiter("{{", "}}", TokenType.Value, false),
				new Delimiter("[[", "]]", TokenType.Value, false),
		};
		#endregion

		#region Fields
		private readonly int paramEndToken;
		private readonly int paramStartToken;
		private readonly string text;
		private readonly List<Token> tokens = new List<Token>();
		private readonly Stack<FoundDelimiter> delimiterStack = new Stack<FoundDelimiter>();
		private int tokenIndex;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterParser"/> class.</summary>
		/// <param name="textToParse">The text to parse.</param>
		/// <param name="isLink">Whether to parse the text as a link (<see langword="true"/>) or a template (<see langword="false"/>).</param>
		/// <param name="parseAllAsValues">if set to <c>true</c> [parse all as values].</param>
		/// <param name="ignoreWhiteSpaceRules">if set to <c>true</c> [ignore white space rules].</param>
		public ParameterParser(string textToParse, bool isLink, bool parseAllAsValues, bool ignoreWhiteSpaceRules)
		{
			this.text = textToParse ?? string.Empty;
			this.Tokenize(isLink ? "[[" : "{{");
			if (this.tokens.Count == 0)
			{
				this.Name = new PaddedString();
				return;
			}

			this.tokenIndex = 0;
			var name = this.GetParameter(true, false);
			if (name.Value.Length > 0 && name.Value[0] == ':')
			{
				this.LeadingColon = true;
				name.Value = name.Value.Substring(1);
			}

			this.Name = name.FullValue;
			if ((this.tokenIndex + 1) < this.tokens.Count)
			{
				this.paramStartToken = this.tokenIndex + 1;
				this.paramEndToken = this.tokens.Count - 1;
			}

			while (this.tokenIndex < this.tokens.Count)
			{
				this.Parameters.Add(this.GetParameter(parseAllAsValues, ignoreWhiteSpaceRules));
			}
		}
		#endregion

		#region Private Enumerations
		private enum TokenType
		{
			Unknown,
			Equals,
			DelimiterMarker,
			DelimiterTerminator,
			Pipe,
			Value,
			WhiteSpace,
		}
		#endregion

		#region Properties

		/// <summary>Gets a value indicating whether the link or template name started with a colon.</summary>
		/// <value><c>true</c> if there was a leading colon; otherwise, <c>false</c>.</value>
		public bool LeadingColon { get; private set; }

		/// <summary>Gets the link or template page name.</summary>
		/// <value>The name.</value>
		public PaddedString Name { get; private set; }

		/// <summary>Gets the parameters parsed from the text provided in the constructor.</summary>
		/// <value>The parameters.</value>
		public IList<Parameter> Parameters { get; } = new List<Parameter>();

		/// <summary>Gets the parsed text from the constructor as a single parameter, as would be used in a normal link.</summary>
		/// <value>The parameter as a single value (everything from the first pipe to the closing delimiter).</value>
		public PaddedString SingleParameter => this.paramEndToken == 0 ? null : this.GetParameterString(this.paramStartToken, this.paramEndToken, true);
		#endregion

		#region Public Methods

		/// <summary>Guesses the default format from the existing parameters.</summary>
		/// <param name="names">if <c>true,</c> returns a format based on the parameter names; otherwise, the format is based on the parameter values.</param>
		/// <returns>A ParameterString with the most common formatting in use, based on the existing named parameters. If there are no named parameters, returns an empty ParameterString.</returns>
		public PaddedString DefaultFormat(bool names)
		{
			var counts = new Dictionary<string, int>();
			var highest = 0;
			var highestKey = string.Empty;
			foreach (var parameter in this.Parameters)
			{
				if (!parameter.Anonymous)
				{
					var key = names
						? parameter.FullName.LeadingWhiteSpace + '|' + parameter.FullName.TrailingWhiteSpace
						: parameter.FullValue.LeadingWhiteSpace + '|' + parameter.FullValue.TrailingWhiteSpace;
					counts.TryGetValue(key, out var lastCount);
					counts[key] = ++lastCount;
					if (lastCount > highest)
					{
						highest = lastCount;
						highestKey = key;
					}
				}
			}

			var retval = new PaddedString();
			if (highest > 0)
			{
				var whiteSpace = highestKey.Split(TextArrays.Pipe);
				retval.LeadingWhiteSpace = whiteSpace[0];
				retval.TrailingWhiteSpace = whiteSpace[1];
			}

			return retval;
		}
		#endregion

		#region Private Methods
		private (Delimiter delimiter, bool isTerminator) CheckForDelimiter(int index)
		{
			if (this.delimiterStack.Count > 0)
			{
				var sought = this.delimiterStack.Peek();
				if (string.Compare(sought.Delimiter.Terminator, 0, this.text, index, sought.Delimiter.Terminator.Length, StringComparison.Ordinal) == 0)
				{
					return (sought.Delimiter, true);
				}
			}

			foreach (var delimiter in AllDelimiters)
			{
				if (string.Compare(delimiter.Marker, 0, this.text, index, delimiter.Marker.Length, StringComparison.Ordinal) == 0)
				{
					return (delimiter, false);
				}
			}

			return (null, false);
		}

		private Parameter GetParameter(bool forceValue, bool ignoreWhiteSpaceRules)
		{
			if (this.tokenIndex >= this.tokens.Count)
			{
				throw new InvalidOperationException("Out of bounds.");
			}

			if (this.tokenIndex > 0)
			{
				if (this.tokens[this.tokenIndex].Type != TokenType.Pipe)
				{
					throw new InvalidOperationException("We're supposed to be sitting on a pipe. Why are we not sitting on a pipe!?!");
				}

				this.tokenIndex++;
			}

			PaddedString name = null;
			var start = this.tokenIndex;
			var firstEquals = true;
			while (this.tokenIndex < this.tokens.Count)
			{
				var token = this.tokens[this.tokenIndex];
				if (token.Type == TokenType.Equals && firstEquals && !forceValue)
				{
					name = this.GetParameterString(start, this.tokenIndex - 1, true);
					start = this.tokenIndex + 1;
					firstEquals = false;
				}
				else if (token.Type == TokenType.Pipe)
				{
					break;
				}

				this.tokenIndex++;
			}

			var value = this.GetParameterString(start, this.tokenIndex - 1, name == null || !ignoreWhiteSpaceRules);

			return new Parameter(name, value);
		}

		private PaddedString GetParameterString(int start, int end, bool parseWhiteSpace)
		{
			var retval = new PaddedString();
			var textStart = start;
			var textEnd = end;
			if (parseWhiteSpace)
			{
				// Do end before start so that if all we have is whitespace, it's assumed to be trailing (e.g., a blank line) rather than leading.
				while (textEnd >= textStart && this.tokens[textEnd].Type == TokenType.WhiteSpace)
				{
					textEnd--;
				}

				if (textEnd < end)
				{
					retval.TrailingWhiteSpace = this.GetString(textEnd + 1, end);
				}

				while (textStart <= textEnd && this.tokens[textStart].Type == TokenType.WhiteSpace)
				{
					textStart++;
				}

				if (textStart > start)
				{
					retval.LeadingWhiteSpace = this.GetString(start, textStart - 1);
				}
			}

			if (textStart <= textEnd)
			{
				retval.Value = this.GetString(textStart, textEnd);
			}

			return retval;
		}

		private string GetString(int start, int end) => start == end ? this.text.Substring(this.tokens[start].Start, this.tokens[start].Length) : this.text.Substring(this.tokens[start].Start, this.tokens[end].Start + this.tokens[end].Length - this.tokens[start].Start);

		private Token GetToken(int index)
		{
			var start = index;
			var tokenType = TokenType.Unknown;
			while (index < this.text.Length)
			{
				if (this.text[index] == '=')
				{
					if (tokenType == TokenType.Unknown)
					{
						tokenType = TokenType.Equals;
						index++;
					}

					break;
				}

				if (this.text[index] == '|')
				{
					if (tokenType == TokenType.Unknown)
					{
						tokenType = TokenType.Pipe;
						index++;
					}

					break;
				}

				var (delimiter, isTerminator) = this.CheckForDelimiter(index);
				if (delimiter != null)
				{
					if (tokenType != TokenType.Unknown)
					{
						break;
					}

					if (isTerminator)
					{
						this.delimiterStack.Pop();
						tokenType = TokenType.DelimiterTerminator;
						index += delimiter.Terminator.Length;
					}
					else
					{
						if (delimiter.NoParse)
						{
							// Special cases - no other delimiters count until we find a terminator.
							var end = this.text.IndexOf(delimiter.Terminator, index + delimiter.Marker.Length, StringComparison.Ordinal);
							if (end != -1)
							{
								index = end + delimiter.Terminator.Length;
								tokenType = delimiter.TokenType;
							}
						}
						else
						{
							this.delimiterStack.Push(new FoundDelimiter(delimiter, index));
							var delimiterToken = FindMatchingDelimiter(delimiter);
							if (delimiterToken == null)
							{
								if (this.delimiterStack.Count > 0)
								{
									index = this.delimiterStack.Pop().Index;
								}
								else
								{
									Debug.WriteLine("I don't think this can ever happen. Prove me wrong!");
								}
							}
							else
							{
								return delimiterToken;
							}
						}
					}

					if (tokenType != TokenType.Unknown)
					{
						// Found a valid delimiter, so break out of further processing; otherwise, fall through and parse as a normal character.
						break;
					}
				}

				if (char.IsWhiteSpace(this.text[index]))
				{
					if (tokenType == TokenType.Unknown)
					{
						tokenType = TokenType.WhiteSpace;
					}
					else if (tokenType != TokenType.WhiteSpace)
					{
						break;
					}
				}
				else
				{
					if (tokenType == TokenType.Unknown)
					{
						tokenType = TokenType.Value;
					}
					else if (tokenType != TokenType.Value)
					{
						break;
					}
				}

				index++;
			}

			return tokenType == TokenType.Unknown ? null : new Token(start, index - start, tokenType);

			Token FindMatchingDelimiter(Delimiter delimiter)
			{
				var markerIndex = index;
				var terminatorIndex = index + delimiter.Marker.Length;
				Token nextToken;
				do
				{
					nextToken = this.GetToken(terminatorIndex);
					terminatorIndex += nextToken?.Length ?? 0;
				}
				while (nextToken != null && (nextToken.Type != TokenType.DelimiterTerminator || this.TokenText(nextToken) != delimiter.Terminator));

				return nextToken != null ? new Token(markerIndex, terminatorIndex - markerIndex, delimiter.TokenType) : null;
			}
		}

		private void Tokenize(string marker)
		{
			if (this.text.Length > 0)
			{
				var (delimiter, isTerminator) = this.CheckForDelimiter(0);
				if (isTerminator || delimiter == null || delimiter.Marker != marker)
				{
					throw new InvalidOperationException("Text does not start with correct opening characters.");
				}

				this.delimiterStack.Push(new FoundDelimiter(delimiter, 0));
				var terminator = delimiter.Terminator;
				var index = delimiter.Marker.Length;
				var token = this.GetToken(index);
				while (token != null && (token.Type != TokenType.DelimiterTerminator || this.TokenText(token) != terminator))
				{
					this.tokens.Add(token);
					index += token.Length;
					token = this.GetToken(index);
				}

				if (this.delimiterStack.Count > 0)
				{
					Debug.WriteLine("Uh oh!");
				}
			}
		}

		private string TokenText(Token token) => this.text.Substring(token.Start, token.Length);
		#endregion

		#region Private Structs
		private struct FoundDelimiter
		{
			public FoundDelimiter(Delimiter delimiter, int index)
			{
				this.Delimiter = delimiter;
				this.Index = index;
			}

			public Delimiter Delimiter { get; }

			public int Index { get; }
		}
		#endregion

		#region Private Classes
		private class Delimiter
		{
			public Delimiter(string marker, string terminator, TokenType tokenType, bool noparse)
			{
				this.Marker = marker;
				this.Terminator = terminator;
				this.TokenType = tokenType;
				this.NoParse = noparse;
			}

			public string Marker { get; }

			public bool NoParse { get; }

			public string Terminator { get; }

			public TokenType TokenType { get; }

			public override string ToString() => this.Marker + " " + this.Terminator;
		}

		private class Token
		{
			public Token(int start, int length, TokenType type)
			{
				this.Start = start;
				this.Length = length;
				this.Type = type;
			}

			public int Length { get; }

			public int Start { get; }

			public TokenType Type { get; set; }

			public override string ToString() => FormattableString.Invariant($"{this.Type}: {this.Start}, {this.Length}");
		}
		#endregion
	}
}
#pragma warning restore CA1303 // Do not pass literals as localized parameters