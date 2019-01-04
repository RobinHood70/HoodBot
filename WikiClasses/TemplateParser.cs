namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	// This is a "dumb" parser class that is rather convoluted at times, but is smaller and (from a certain POV) simpler than building a full-fledged parse tree.
	public class TemplateParser
	{
		#region Private Constants
		private const string ParameterSeparators = "|=";
		private const string PipeString = "|";
		#endregion

		#region Fields
		private static Pair[] searchStrings =
		{
				new Pair("<!--", "-->", true),
				new Pair("<nowiki>", "</nowiki>", true),
				new Pair("{{{", "}}}", false),
				new Pair("{{", "}}", false),
				new Pair("[[", "]]", false),
		};
		#endregion

		#region Constructors
		public TemplateParser(string text) => this.Text = text;
		#endregion

		#region Private Enumerations
		private enum ValueType
		{
			Unknown,
			Value,
			Pipe,
			Equals,
			WhiteSpace
		}
		#endregion

		#region Public Properties
		public int Index { get; private set; } = 0;

		public string Text { get; }
		#endregion

		#region Public Methods
		public void ParseIntoTemplate(Template template, bool ignoreWhiteSpaceRules)
		{
			ThrowNull(template, nameof(template));
			this.GetString(PipeString, template.FullName);
			while (this.Index < this.Text.Length)
			{
				template.Add(this.ParseParameter(ignoreWhiteSpaceRules));
			}

			if (template.Count > 0)
			{
				var defaultNames = new Dictionary<string, int>();
				var defaultValues = new Dictionary<string, int>();
				foreach (var parameter in template)
				{
					string key;
					if (!parameter.Anonymous)
					{
						key = parameter.FullName.LeadingWhiteSpace + PipeString + parameter.FullName.TrailingWhiteSpace;
						if (defaultNames.ContainsKey(key))
						{
							defaultNames[key]++;
						}
						else
						{
							defaultNames.Add(key, 1);
						}
					}

					key = parameter.FullValue.LeadingWhiteSpace + PipeString + parameter.FullValue.TrailingWhiteSpace;
					if (defaultValues.ContainsKey(key))
					{
						defaultValues[key]++;
					}
					else
					{
						defaultValues.Add(key, 1);
					}
				}

				if (defaultNames.Count > 0)
				{
					var list = new List<KeyValuePair<string, int>>(defaultNames);
					list.Sort((x, y) => y.Value.CompareTo(x.Value));
					var whiteSpace = list[0].Key.Split(TextArrays.Pipe);
					template.DefaultNameFormat.LeadingWhiteSpace = whiteSpace[0];
					template.DefaultNameFormat.TrailingWhiteSpace = whiteSpace[1];
				}

				if (defaultValues.Count > 0)
				{
					var list = new List<KeyValuePair<string, int>>(defaultValues);
					list.Sort((x, y) => y.Value.CompareTo(x.Value));
					var whiteSpace = list[0].Key.Split(TextArrays.Pipe);
					template.DefaultValueFormat.LeadingWhiteSpace = whiteSpace[0];
					template.DefaultValueFormat.TrailingWhiteSpace = whiteSpace[1];
				}
			}
		}
		#endregion

		#region Private Methods
		private TemplateString GetString(string delimiters, TemplateString templateString)
		{
			// TODO: Re-examine - this got kludgey after fixing empty last parameter bug.
			var builder = new StringBuilder(20);
			var foundDelimiter = !(this.Index < this.Text.Length);
			while (!foundDelimiter)
			{
				var nextToken = this.PeekToken(this.Index);
				while (nextToken.IsWhiteSpace && this.Index < this.Text.Length)
				{
					this.Index += nextToken.Text.Length;
					builder.Append(nextToken.Text);
					if (this.Index < this.Text.Length)
					{
						nextToken = this.PeekToken(this.Index);
					}
				}

				if (builder.Length > 0)
				{
					templateString.LeadingWhiteSpace = builder.ToString();
					builder.Clear();
				}

				var startOfWhiteSpace = 0;
				if (this.Index < this.Text.Length)
				{
					foundDelimiter = delimiters.IndexOf(nextToken.Text[0]) != -1;
					while (!foundDelimiter)
					{
						builder.Append(nextToken.Text);
						if (!nextToken.IsWhiteSpace)
						{
							startOfWhiteSpace = builder.Length;
						}

						this.Index += nextToken.Text.Length;
						if (this.Index < this.Text.Length)
						{
							nextToken = this.PeekToken(this.Index);
							foundDelimiter = delimiters.IndexOf(nextToken.Text[0]) != -1;
						}
						else
						{
							foundDelimiter = true;
						}
					}
				}
				else
				{
					foundDelimiter = true;
				}

				if (startOfWhiteSpace > 0)
				{
					templateString.Value = builder.ToString(0, startOfWhiteSpace);
					templateString.TrailingWhiteSpace = builder.ToString(startOfWhiteSpace, builder.Length - startOfWhiteSpace);
				}
				else
				{
					// If all WhiteSpace, assume that anything from \n or \r onwards is trailing WhiteSpace.
					templateString.Value = string.Empty;
					var newLine = templateString.LeadingWhiteSpace.IndexOfAny(TextArrays.NewLineChars);
					if (newLine >= 0)
					{
						templateString.TrailingWhiteSpace = templateString.LeadingWhiteSpace.Substring(newLine);
						templateString.LeadingWhiteSpace = templateString.LeadingWhiteSpace.Substring(0, newLine);
					}
				}
			}

			return templateString;
		}

		private Parameter ParseParameter(bool ignoreWhiteSpaceRules)
		{
			this.Index++; // We are always sitting on a pipe when this is called, so skip past it.
			var valueString = this.GetString(ParameterSeparators, new TemplateString());
			if (this.Index < this.Text.Length && this.Text[this.Index] == '=')
			{
				var nameString = valueString;
				this.Index++; // We are now guaranteed to be sitting on an equals sign, so skip past that.
				valueString = this.GetString(PipeString, new TemplateString());
				return new Parameter(nameString, valueString);
			}

			if (ignoreWhiteSpaceRules)
			{
				return new Parameter(valueString);
			}

			// Unnamed parameter, and we're not ignoring rules, so stuff all WhiteSpace back into the value.
			return new Parameter(valueString.Build());
		}

		private Token PeekToken(int index)
		{
			var letter = this.Text[index];
			if (char.IsWhiteSpace(letter))
			{
				return new Token(letter.ToString(), true);
			}

			// Traditional loop rather than foreach for speed, since GetToken is called repeatedly.
			for (var i = 0; i < searchStrings.Length; i++)
			{
				var pair = searchStrings[i];
				var startLength = pair.Start.Length;
				if (string.Compare(pair.Start, 0, this.Text, index, startLength, StringComparison.Ordinal) == 0)
				{
					var indexEnd = index + startLength;
					if (pair.CountsAsWhiteSpace)
					{
						indexEnd = this.Text.IndexOf(pair.Terminator, indexEnd, StringComparison.Ordinal);
						return indexEnd == -1 ? new Token(this.Text.Substring(index), false) : new Token(this.Text.Substring(index, indexEnd - index + pair.Terminator.Length), pair.CountsAsWhiteSpace);
					}

					var match = false;
					while (indexEnd < (this.Text.Length - pair.Terminator.Length) && !match)
					{
						var nextToken = this.PeekToken(indexEnd);
						indexEnd += nextToken.Text.Length;
						match = string.Compare(pair.Terminator, 0, this.Text, indexEnd, pair.Terminator.Length, StringComparison.Ordinal) == 0;
					}

					if (match)
					{
						indexEnd += pair.Terminator.Length;
						return new Token(this.Text.Substring(index, indexEnd - index), pair.CountsAsWhiteSpace);
					}
				}
			}

			return new Token(letter.ToString(), false);
		}
		#endregion

		#region Private Structs
		private struct Pair
		{
			public Pair(string start, string terminator, bool countsAsWhiteSpace)
			{
				this.Start = start;
				this.Terminator = terminator;
				this.CountsAsWhiteSpace = countsAsWhiteSpace;
			}

			public bool CountsAsWhiteSpace { get; }

			public string Start { get; }

			public string Terminator { get; }

			public override string ToString() => this.Start + " " + this.Terminator;
		}

		private struct Token
		{
			public Token(string letters, bool isWhiteSpace)
			{
				this.Text = letters;
				this.IsWhiteSpace = isWhiteSpace;
			}

			public string Text { get; }

			public bool IsWhiteSpace { get; }

			public override string ToString() => this.Text;
		}
		#endregion
	}
}