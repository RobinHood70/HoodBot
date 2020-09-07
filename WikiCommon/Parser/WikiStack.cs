namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.InteropServices;
	using System.Text.RegularExpressions;
	using RobinHood70.WikiCommon.Parser.StackElements;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;

	// Not a .NET Stack<T> mostly for closer parity with the original PHP version, plus it significantly outperforms the built-in one. Top, being a property, also provides a significant debugging advantage over Peek().
	internal class WikiStack
	{
		#region Internal Constants
		internal const string CommentWhiteSpace = " \t";
		#endregion

		#region Private Constants
		private const int StartSize = 4;
		private const string IncludeOnlyTag = "includeonly";
		private const string NoIncludeTag = "noinclude";
		private const string OnlyIncludeTag = "onlyinclude";
		private const string OnlyIncludeTagClose = "</" + OnlyIncludeTag + ">";
		private const string OnlyIncludeTagOpen = "<" + OnlyIncludeTag + ">";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> AllowMissingEndTag = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { IncludeOnlyTag, NoIncludeTag, OnlyIncludeTag };
		#endregion

		#region Fields
		private readonly bool enableOnlyInclude;
		private readonly HashSet<string> ignoredElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> ignoredTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly bool includeIgnores;
		private readonly HashSet<string> noMoreClosingTag = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly int textLength;
		private readonly Regex tagsRegex;
		private StackElement[] array;
		private int count;
		private bool findOnlyinclude;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiStack"/> class.</summary>
		/// <param name="text">The text to work with.</param>
		/// <param name="tagList">A list of tags whose contents should not be parsed.</param>
		/// <param name="include">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public WikiStack([Localizable(false)] string text, ICollection<string> tagList, bool? include, bool strictInclusion)
		{
			// Not using Push both so that nullable reference check succeeds on .Top and for a micro-optimization.
			this.array = new StackElement[StartSize];
			this.Top = new RootElement(this);
			this.array[0] = this.Top;
			this.count = 1;

			this.Text = text;
			this.textLength = text.Length;

			var allTags = new HashSet<string>(tagList, StringComparer.Ordinal);
			switch (include)
			{
				case true:
					this.includeIgnores = !strictInclusion;
					this.enableOnlyInclude = text.Contains(OnlyIncludeTagOpen, StringComparison.OrdinalIgnoreCase);
					this.findOnlyinclude = this.enableOnlyInclude;
					this.ignoredTags.UnionWith(new[] { IncludeOnlyTag, "/" + IncludeOnlyTag });
					this.ignoredElements.Add(NoIncludeTag);
					allTags.Add(NoIncludeTag);
					break;
				case false:
					this.includeIgnores = !strictInclusion;
					this.ignoredTags.UnionWith(new[] { NoIncludeTag, "/" + NoIncludeTag, OnlyIncludeTag, "/" + OnlyIncludeTag });
					this.ignoredElements.Add(IncludeOnlyTag);
					allTags.Add(IncludeOnlyTag);
					break;
				default:
					this.includeIgnores = true;
					this.ignoredTags.UnionWith(new[] { NoIncludeTag, "/" + NoIncludeTag, OnlyIncludeTag, "/" + OnlyIncludeTag, IncludeOnlyTag, "/" + IncludeOnlyTag });
					break;
			}

			allTags.UnionWith(this.ignoredTags);
			var regexTags = new List<string>(allTags.Count);
			foreach (var tag in allTags)
			{
				regexTags.Add(Regex.Escape(tag));
			}

			regexTags.Sort();
			this.tagsRegex = new Regex(@"\G(" + string.Join("|", regexTags) + @")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, DefaultRegexTimeout);

			this.Preprocess();
		}
		#endregion

		#region Internal Properties
		internal int HeadingIndex { get; set; } = 1;

		internal int Index { get; set; }

		internal string Text { get; }

		internal StackElement Top { get; private set; }
		#endregion

		#region Private Properties
		private char CurrentCharacter => this.Text[this.Index];
		#endregion

		#region Public Methods
		public ElementNodeCollection GetFinalNodes()
		{
			// We don't need to check that array.Length > 0 because the root node cannot be popped without throwing an error.
			var nodes = this.array[0].CurrentPiece;
			for (var i = 1; i < this.count; i++)
			{
				nodes.Merge(this.array[i].BreakSyntax());
			}

			for (var i = 0; i < nodes.Count; i++)
			{
				if (nodes[i] is HeaderNode hNode && !hNode.Confirmed)
				{
					hNode.Confirmed = true;
				}
			}

			return nodes;
		}
		#endregion

		#region Internal Methods
		internal void Parse(char found)
		{
			switch (found)
			{
				case '\n':
					this.Top.CurrentPiece.AddLiteral("\n");
					this.Index++;
					this.ParseLineStart();
					break;
				case '<':
					if (this.enableOnlyInclude && string.Compare(this.Text, this.Index, OnlyIncludeTagClose, 0, OnlyIncludeTagClose.Length, StringComparison.OrdinalIgnoreCase) == 0)
					{
						this.findOnlyinclude = true;
					}
					else if (string.Compare(this.Text, this.Index + 1, "!--", 0, 3, StringComparison.Ordinal) == 0)
					{
						if (this.FoundComment())
						{
							this.ParseLineStart();
						}
					}
					else
					{
						var tagMatch = this.tagsRegex.Match(this.Text, this.Index + 1);
						if (!tagMatch.Success || !this.FoundTag(tagMatch.Groups[1].Value))
						{
							this.Top.CurrentPiece.AddLiteral("<");
							this.Index++;
						}
					}

					break;
				case '{':
				case '[':
					var countFound = this.Text.Span(found, this.Index);
					if (countFound >= 2)
					{
						var element = found == '['
							? new LinkElement(this, countFound)
							: new TemplateElement(this, countFound/*, this.Index > 0 && this.Text[this.Index - 1] == '\n'*/) as StackElement;
						this.Push(element);
					}
					else
					{
						this.Top.CurrentPiece.AddLiteral(new string(found, countFound));
					}

					this.Index += countFound;
					break;
				default:
					var curChar = this.Index < this.Text.Length ? this.Text[this.Index] : '\uffff';
					throw new InvalidOperationException(Invariant($"Found unexpected character '{curChar}' at position {this.Index}."));
			}
		}

		internal void Pop()
		{
			this.count--;
			if (this.count == 0)
			{
				throw new InvalidOperationException(Resources.PoppedRoot);
			}

			// this.array[this.count] = null;
			this.Top = this.array[this.count - 1];
		}

		internal void Push(StackElement item)
		{
			if (this.count == this.array.Length)
			{
				var newArray = new StackElement[this.count << 1];
				Array.Copy(this.array, newArray, this.count);
				this.array = newArray;
			}

			this.array[this.count] = item;
			this.count++;
			this.Top = item;
		}
		#endregion

		#region Private Methods

		// Returns true if comment(s) are surrounded by NewLines, so caller knows whether to check for a possible header.
		private bool FoundComment()
		{
			var piece = this.Top.CurrentPiece;
			var endPos = this.Text.IndexOf("-->", this.Index + 4, StringComparison.Ordinal) + 3;
			if (endPos == 2)
			{
				piece.Add(new CommentNode(this.Text.Substring(this.Index)));
				this.Index = this.textLength;
				return false;
			}

			var comments = new List<Comment>();
			var wsStart = this.Index - this.Text.SpanReverse(CommentWhiteSpace, this.Index);
			var closing = endPos;
			var wsEnd = wsStart;
			do
			{
				var length = this.Text.Span(CommentWhiteSpace, closing);
				comments.Add(new Comment(wsEnd, closing, length));
				wsEnd = closing + length;
				closing = string.Compare(this.Text, wsEnd, "<!--", 0, 4, StringComparison.Ordinal) == 0
					? this.Text.IndexOf("-->", wsEnd + 4, StringComparison.Ordinal) + 3
					: 2;
			}
			while (closing != 2);

			var retval = false;
			int startPos;
			Comment cmt;
			if (wsStart > 0 && wsEnd < this.textLength && this.Text[wsStart - 1] == '\n' && this.Text[wsEnd] == '\n')
			{
				var wsLength = this.Index - wsStart;
				if (wsLength > 0)
				{
					if (piece[^1] is TextNode last)
					{
						var lastValue = last.Text;
						if (lastValue.SpanReverse(CommentWhiteSpace, lastValue.Length) == wsLength)
						{
							last.Text = lastValue.Substring(0, lastValue.Length - wsLength);
						}
					}
				}

				var lastComment = comments.Count - 1;
				for (var j = 0; j < lastComment; j++)
				{
					cmt = comments[j];
					piece.Add(new CommentNode(this.Text.Substring(cmt.Start, cmt.End - cmt.Start + cmt.WhiteSpaceLength)));
				}

				cmt = comments[lastComment];
				startPos = cmt.Start;
				endPos = cmt.End + cmt.WhiteSpaceLength + 1; // Grab the trailing \n

				retval = true;
			}
			else
			{
				// Since we have the comments all gathered up and everything between them is known to be text, don't backtrack, add them all here.
				var lastComment = comments.Count - 1;
				for (var j = 0; j < lastComment; j++)
				{
					cmt = comments[j];
					var start = j == 0 ? this.Index : cmt.Start;
					piece.Add(new CommentNode(this.Text[start..cmt.End]));
					piece.Add(new TextNode(this.Text.Substring(cmt.End, cmt.WhiteSpaceLength)));
				}

				cmt = comments[lastComment];
				startPos = lastComment == 0 ? this.Index : cmt.Start;
				endPos = cmt.End;
			}

			if (piece.CommentEnd != wsStart - 1)
			{
				piece.VisualEnd = wsStart;
			}

			piece.CommentEnd = endPos - 1;
			piece.Add(new CommentNode(this.Text[startPos..endPos]));
			this.Index = endPos;

			return retval;
		}

		// Returns true if a valid tag was found.
		private bool FoundTag(string tagOpen)
		{
			var piece = this.Top.CurrentPiece;
			var attrStart = this.Index + tagOpen.Length + 1;
			var tagEndPos = this.Text.IndexOf('>', attrStart);
			if (tagEndPos == -1)
			{
				piece.AddLiteral("<");
				this.Index++;
				return false;
			}

			if (this.ignoredTags.Contains(tagOpen))
			{
				if (this.includeIgnores)
				{
					piece.Add(new IgnoreNode(this.Text.Substring(this.Index, tagEndPos - this.Index + 1)));
				}

				this.Index = tagEndPos + 1;
				return true;
			}

			var tagStartPos = this.Index;
			int attrEnd;
			string? tagClose;
			string? inner;
			if (this.Text[tagEndPos - 1] == '/')
			{
				inner = null;
				tagClose = null;
				attrEnd = tagEndPos - 1;
				this.Index = tagEndPos + 1;
			}
			else
			{
				attrEnd = tagEndPos;
				var findClosing = new Regex(@"</" + tagOpen + @"\s*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
				Match match;
				if (!this.noMoreClosingTag.Contains(tagOpen) && (match = findClosing.Match(this.Text, tagEndPos + 1)).Success)
				{
					inner = this.Text.Substring(tagEndPos + 1, match.Index - tagEndPos - 1);
					tagClose = match.Value;
					this.Index = match.Index + match.Length;
				}
				else if (AllowMissingEndTag.Contains(tagOpen))
				{
					inner = this.Text.Substring(tagEndPos + 1);
					tagClose = string.Empty;
					this.Index = this.textLength;
				}
				else
				{
					this.Index = tagEndPos + 1;
					piece.AddLiteral(this.Text[tagStartPos..this.Index]);
					this.noMoreClosingTag.Add(tagOpen);
					return true;
				}
			}

			if (this.ignoredElements.Contains(tagOpen))
			{
				if (this.includeIgnores)
				{
					piece.Add(new IgnoreNode(this.Text[tagStartPos..this.Index]));
				}
			}
			else
			{
				var attr = attrEnd > attrStart ? this.Text[attrStart..attrEnd] : null;
				piece.Add(new TagNode(tagOpen, attr, inner, tagClose));
			}

			return true;
		}

		private void ParseLineStart()
		{
			var equalsCount = this.Text.Span('=', this.Index, 6);

			// Using LastIndexOf instead of Contains as a very minor optimization, since we know the = is added to the SearchString last for a PairedElement. Even if that changes in the future, this won't break, it just won't be quite as optimal.
			if (equalsCount > 1 || (equalsCount == 1 && this.Top.SearchString.LastIndexOf('=') == -1))
			{
				this.Push(new HeaderElement(this, equalsCount));
				this.Index += equalsCount;
			}
		}

		private void Preprocess()
		{
			if (this.textLength == 0)
			{
				return;
			}

			if (this.textLength > 0 && this.Text[0] == '=')
			{
				this.ParseLineStart();
			}

			do
			{
				if (this.findOnlyinclude)
				{
					var startPos = this.Text.IndexOf(OnlyIncludeTagOpen, this.Index, StringComparison.OrdinalIgnoreCase);
					if (startPos == -1)
					{
						if (this.includeIgnores)
						{
							this.Top.CurrentPiece.Add(new IgnoreNode(this.Text.Substring(this.Index)));
						}

						break;
					}

					var tagEndPos = startPos + OnlyIncludeTagOpen.Length; // past-the-end
					if (this.includeIgnores)
					{
						this.Top.CurrentPiece.Add(new IgnoreNode(this.Text[this.Index..tagEndPos]));
					}

					this.Index = tagEndPos;
					this.findOnlyinclude = false;
				}

				var search = this.Top.SearchString;
				var literalOffset = this.Text.IndexOfAny(search.ToCharArray(), this.Index);
				if (literalOffset == -1)
				{
					literalOffset = this.textLength;
				}

				if (literalOffset != this.Index)
				{
					this.Top.CurrentPiece.AddLiteral(this.Text[this.Index..literalOffset]);
					this.Index = literalOffset;
					if (this.Index >= this.textLength)
					{
						break;
					}
				}

				this.Top.Parse(this.CurrentCharacter);
			}
			while (this.Index < this.textLength);

			var lastHeader = this.Top as HeaderElement;
			lastHeader?.Parse('\n');
		}
		#endregion

		#region Private Structures
		[StructLayout(LayoutKind.Auto)]
		private struct Comment
		{
			public Comment(int start, int end, int wsLength)
			{
				this.Start = start;
				this.End = end;
				this.WhiteSpaceLength = wsLength;
			}

			public int End { get; set; }

			public int Start { get; set; }

			public int WhiteSpaceLength { get; set; }
		}
		#endregion
	}
}