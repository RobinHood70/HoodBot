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
		public TemplateParser(string text)
		{
			this.Index = 0;
			this.Text = text;
		}
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
		public int Index { get; private set; }

		public string Text { get; }
		#endregion

		#region Public Methods
		public void ParseIntoTemplate(Template template, bool ignoreWhiteSpaceRules)
		{
			ThrowNull(template, nameof(template));
			this.GetString("|", template.FullName);
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
						key = parameter.FullName.LeadingWhiteSpace + "|" + parameter.FullName.TrailingWhiteSpace;
						if (defaultNames.ContainsKey(key))
						{
							defaultNames[key]++;
						}
						else
						{
							defaultNames.Add(key, 1);
						}
					}

					key = parameter.FullValue.LeadingWhiteSpace + "|" + parameter.FullValue.TrailingWhiteSpace;
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
		private TemplateString GetString(string delimiters)
		{
			var retval = new TemplateString();
			this.GetString(delimiters, retval);
			return retval;
		}

		private void GetString(string delimiters, TemplateString templateString)
		{
			var builder = new StringBuilder(20);
			var foundDelimiter = this.Index >= this.Text.Length;
			while (!foundDelimiter)
			{
				var nextToken = this.GetToken();
				while (this.Index < this.Text.Length && nextToken.IsWhiteSpace)
				{
					builder.Append(nextToken.Text);
					nextToken = this.GetToken();
				}

				if (nextToken.IsWhiteSpace)
				{
					builder.Append(nextToken.Text);
				}

				if (builder.Length > 0)
				{
					templateString.LeadingWhiteSpace = builder.ToString();
					builder.Clear();
				}

				var startOfWhiteSpace = 0;
				foundDelimiter = delimiters.IndexOf(nextToken.Text[0]) != -1;
				while (this.Index < this.Text.Length && !foundDelimiter)
				{
					builder.Append(nextToken.Text);
					if (!nextToken.IsWhiteSpace)
					{
						startOfWhiteSpace = builder.Length;
					}

					nextToken = this.GetToken();
					foundDelimiter = delimiters.IndexOf(nextToken.Text[0]) != -1;
				}

				if (!foundDelimiter)
				{
					builder.Append(nextToken.Text);
					if (!nextToken.IsWhiteSpace)
					{
						startOfWhiteSpace = builder.Length;
					}

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
		}

		private Token GetToken()
		{
			var letter = this.Text.Substring(this.Index, 1);
			if (char.IsWhiteSpace(letter[0]))
			{
				this.Index++;
				return new Token(letter, true);
			}

			// Traditional loop rather than foreach for speed, since GetToken is called repeatedly.
			for (var i = 0; i < searchStrings.Length; i++)
			{
				var pair = searchStrings[i];
				var startLength = pair.Start.Length;
				if (string.Compare(pair.Start, 0, this.Text, this.Index, startLength, StringComparison.Ordinal) == 0)
				{
					var index = this.Index;
					this.Index += startLength;
					var match = false;
					do
					{
						this.GetToken();
						match = string.Compare(pair.Terminator, 0, this.Text, this.Index, pair.Terminator.Length, StringComparison.Ordinal) == 0;
					}
					while (this.Index <= (this.Text.Length - pair.Terminator.Length) && !match);

					if (match)
					{
						this.Index += pair.Terminator.Length;
						return new Token(this.Text.Substring(index, this.Index - index), pair.CountsAsWhiteSpace);
					}
					else
					{
						this.Index = index;
					}
				}
			}

			this.Index++;
			return new Token(letter, false);
		}

		private Parameter ParseParameter(bool ignoreWhiteSpaceRules)
		{
			var valueString = this.GetString("|=");
			if (this.Index > 0 && this.Text[this.Index - 1] == '=')
			{
				var nameString = valueString;
				valueString = this.GetString("|");
				return new Parameter(nameString, valueString);
			}

			if (ignoreWhiteSpaceRules)
			{
				return new Parameter(valueString);
			}

			// Unnamed parameter, and we're not ignoring rules, so stuff all WhiteSpace back into the value.
			return new Parameter(valueString.Build());
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
		}
		#endregion
	}
}