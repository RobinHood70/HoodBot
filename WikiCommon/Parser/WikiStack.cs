namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser.StackElements;
	using RobinHood70.WikiCommon.Properties;

	/// <summary>What to include when parsing.</summary>
	public enum InclusionType
	{
		/// <summary>Parse text as if it were transcluded to another page. Ignored text and tags will be put into <see cref="IIgnoreNode"/>s unless using strict inclusion.</summary>
		Transcluded,

		/// <summary>Parse text as it would appear on the current page. Ignored text and tags will be put into <see cref="IIgnoreNode"/>s unless using strict inclusion.</summary>
		CurrentPage,

		/// <summary>Parse all text. Only inclusion tags themselves will be put into <see cref="IIgnoreNode"/>s; all remaining text will be parsed.</summary>
		Raw,
	}

	/// <summary>This class does the core work to parse text into a list of wiki nodes. It provides factory methods for each node type, allowing implementers to override the way nodes are created.</summary>
	public sealed class WikiStack
	{
		#region Private Constants
		private const int StartSize = 4;
		private const string IncludeOnlyTag = "includeonly";
		private const string NoIncludeTag = "noinclude";
		private const string OnlyIncludeTag = "onlyinclude";
		private const string OnlyIncludeTagClose = "</" + OnlyIncludeTag + ">";
		private const string OnlyIncludeTagOpen = "<" + OnlyIncludeTag + ">";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> AllowMissingEndTag = new(StringComparer.OrdinalIgnoreCase) { IncludeOnlyTag, NoIncludeTag, OnlyIncludeTag };
		#endregion

		#region Fields
		private readonly bool enableOnlyInclude;
		private readonly HashSet<string> ignoredElements = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> ignoredTags = new(StringComparer.OrdinalIgnoreCase);
		private readonly bool includeIgnores;
		private readonly HashSet<string> noMoreClosingTag = new(StringComparer.OrdinalIgnoreCase);
		private readonly int textLength;
		private readonly Regex tagsRegex;

		private StackElement[] array;
		private int count;
		private bool findOnlyinclude;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiStack"/> class.</summary>
		/// <param name="factory">The <see cref="IWikiNodeFactory">factory</see> to use for creating nodes.</param>
		/// <param name="text">The text to work with. Null values will be treated as empty strings.</param>
		/// <param name="inclusionType">The inclusion type for the text. Set to <see cref="InclusionType.Transcluded"/> to return text as if transcluded to another page; <see cref="InclusionType.CurrentPage"/> to return text as it would appear on the current page; <see cref="InclusionType.Raw"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public WikiStack(IWikiNodeFactory factory, [Localizable(false)] string? text, InclusionType inclusionType, bool strictInclusion)
		{
			ArgumentNullException.ThrowIfNull(factory);
			this.NodeFactory = factory;

			// Not using Push both so that nullable reference check succeeds on .Top and for a micro-optimization.
			this.array = new StackElement[StartSize];
			this.Top = new RootElement(this);
			this.array[0] = this.Top;
			this.count = 1;

			text ??= string.Empty;
			this.Text = text;
			this.textLength = text.Length;

			this.ignoredTags.UnionWith(ParsedTags);
			HashSet<string> allTags = new(UnparsedTags, StringComparer.Ordinal);
			switch (inclusionType)
			{
				case InclusionType.Transcluded:
					this.includeIgnores = !strictInclusion;
					this.enableOnlyInclude = text.Contains(OnlyIncludeTagOpen, StringComparison.OrdinalIgnoreCase);
					this.findOnlyinclude = this.enableOnlyInclude;
					this.ignoredTags.UnionWith([IncludeOnlyTag, "/" + IncludeOnlyTag]);
					this.ignoredElements.Add(NoIncludeTag);
					allTags.Add(NoIncludeTag);
					break;
				case InclusionType.CurrentPage:
					this.includeIgnores = !strictInclusion;
					this.ignoredTags.UnionWith([NoIncludeTag, "/" + NoIncludeTag, OnlyIncludeTag, "/" + OnlyIncludeTag]);
					this.ignoredElements.Add(IncludeOnlyTag);
					allTags.Add(IncludeOnlyTag);
					break;
				default:
					this.includeIgnores = true;
					this.ignoredTags.UnionWith([NoIncludeTag, "/" + NoIncludeTag, OnlyIncludeTag, "/" + OnlyIncludeTag, IncludeOnlyTag, "/" + IncludeOnlyTag]);
					break;
			}

			allTags.UnionWith(this.ignoredTags);
			List<string> regexTags = new(allTags.Count);
			foreach (var tag in allTags)
			{
				regexTags.Add(Regex.Escape(tag));
			}

			regexTags.Sort(StringComparer.Ordinal);
			this.tagsRegex = new Regex(@"\G(" + string.Join('|', regexTags) + ")", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			this.Preprocess();
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the list of tags which should be parsed as ignored ITagNodes (i.e., where there's valid wikitext inside of them).</summary>
		/// <value>The tags.</value>
		public static IList<string> ParsedTags { get; } = [];

		/// <summary>Gets the list of tags which are not parsed into wikitext.</summary>
		/// <value>The unparsed tags.</value>
		public static IList<string> UnparsedTags { get; } = ["pre", "nowiki", "gallery", "indicator"];
		#endregion

		#region Internal Properties
		internal int Index { get; set; }

		internal IWikiNodeFactory NodeFactory { get; }

		internal string Text { get; }

		internal StackElement Top { get; private set; }
		#endregion

		#region Private Properties
		private char CurrentCharacter => this.Text[this.Index];
		#endregion

		#region Public Methods

		/// <summary>Does final processing before returning the node collection.</summary>
		/// <returns>The <see cref="NodeCollection"/> representing the text provided in the constructor.</returns>
		public IEnumerable<IWikiNode> GetNodes()
		{
			var finalNodes = this.array[0].CurrentPiece;
			for (var i = 1; i < this.count; i++)
			{
				finalNodes.MergeText(this.array[i].Backtrack());
			}

			foreach (var node in finalNodes.Nodes)
			{
				if (node is IHeaderNode hNode && !hNode.Confirmed)
				{
					hNode.Confirmed = true;
				}
			}

			return finalNodes.Nodes;
		}
		#endregion

		#region Internal Methods
		internal void ParseCharacter(char found)
		{
			switch (found)
			{
				case '\n':
					this.Top.CurrentPiece.AddLiteral(this.NodeFactory, "\n");
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
						if (this.ParseComment())
						{
							this.ParseLineStart();
						}
					}
					else
					{
						var tagMatch = this.tagsRegex.Match(this.Text, this.Index + 1);
						if (!tagMatch.Success || !this.FoundTag(tagMatch.Value))
						{
							this.Top.CurrentPiece.AddLiteral(this.NodeFactory, "<");
							this.Index++;
						}
					}

					break;
				case '{':
				case '[':
					var countFound = this.Text.Span(found, this.Index);
					if (countFound >= 2)
					{
						StackElement element = found == '['
							? new LinkElement(this, countFound)
							: new TemplateElement(this, countFound/*, this.Index > 0 && this.Text[this.Index - 1] == '\n'*/);
						this.Push(element);
					}
					else
					{
						this.Top.CurrentPiece.AddLiteral(this.NodeFactory, new string(found, countFound));
					}

					this.Index += countFound;
					break;
				default:
					var curChar = this.Index < this.Text.Length ? this.Text[this.Index] : '\uffff';
					throw new InvalidOperationException(string.Create(
						CultureInfo.InvariantCulture,
						$"Found unexpected character '{curChar}' at position {this.Index}."));
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
		private bool ParseComment()
		{
			var piece = this.Top.CurrentPiece;
			var endPos = this.Text.IndexOf("-->", this.Index + 4, StringComparison.Ordinal);
			if (endPos == -1)
			{
				piece.Nodes.Add(this.NodeFactory.CommentNode(this.Text[this.Index..]));
				this.Index = this.textLength;
				return false;
			}

			var wsStart = this.Index - this.Text.SpanReverse(HeaderElement.CommentWhiteSpace, this.Index);
			var wsEnd = wsStart;
			(wsEnd, var comments) = this.GetComments(endPos + 3, wsEnd);

			var retval = false;
			int startPos;
			Comment cmt;
			if (wsStart > 0 && wsEnd < this.textLength && this.Text[wsStart - 1] == '\n' && this.Text[wsEnd] == '\n')
			{
				var wsLength = this.Index - wsStart;
				if (wsLength > 0 && piece.Nodes[^1] is ITextNode last)
				{
					var lastValue = last.Text;
					if (lastValue.SpanReverse(HeaderElement.CommentWhiteSpace, lastValue.Length) == wsLength)
					{
						last.Text = lastValue[..^wsLength];
					}
				}

				var lastComment = comments.Count - 1;
				for (var j = 0; j < lastComment; j++)
				{
					cmt = comments[j];
					piece.Nodes.Add(this.NodeFactory.CommentNode(this.Text.Substring(cmt.Start, cmt.End - cmt.Start + cmt.WhiteSpaceLength)));
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
					piece.Nodes.Add(this.NodeFactory.CommentNode(this.Text[start..cmt.End]));
					piece.Nodes.Add(this.NodeFactory.TextNode(this.Text.Substring(cmt.End, cmt.WhiteSpaceLength)));
				}

				cmt = comments[lastComment];
				startPos = lastComment == 0 ? this.Index : cmt.Start;
				endPos = cmt.End;
			}

			if (piece is HeaderPiece header)
			{
				if (header.CommentEnd != wsStart - 1)
				{
					header.VisualEnd = wsStart;
				}

				header.CommentEnd = endPos - 1;
			}

			piece.Nodes.Add(this.NodeFactory.CommentNode(this.Text[startPos..endPos]));
			this.Index = endPos;

			return retval;
		}

		private (int NewEnd, List<Comment> Comments) GetComments(int closing, int wsEnd)
		{
			List<Comment> comments = [];
			do
			{
				var length = this.Text.Span(HeaderElement.CommentWhiteSpace, closing);
				comments.Add(new Comment(wsEnd, closing, length));
				wsEnd = closing + length;
				closing = string.Compare(this.Text, wsEnd, "<!--", 0, 4, StringComparison.Ordinal) == 0
					? this.Text.IndexOf("-->", wsEnd + 4, StringComparison.Ordinal) + 3
					: 2;
			}
			while (closing != 2);

			return (wsEnd, comments);
		}

		// Returns true if a valid tag was found.
		private bool FoundTag(string tagOpen)
		{
			var piece = this.Top.CurrentPiece;
			var attrStart = this.Index + tagOpen.Length + 1;
			var tagEndPos = this.Text.IndexOf('>', attrStart);
			if (tagEndPos == -1)
			{
				piece.AddLiteral(this.NodeFactory, "<");
				this.Index++;
				return false;
			}

			if (this.ignoredTags.Contains(tagOpen))
			{
				if (this.includeIgnores)
				{
					piece.Nodes.Add(this.NodeFactory.IgnoreNode(this.Text.Substring(this.Index, tagEndPos - this.Index + 1)));
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
				Regex findClosing = new("</" + tagOpen + @"\s*>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
				var noClose = this.noMoreClosingTag.Contains(tagOpen);
				var match = findClosing.Match(this.Text, tagEndPos + 1);
				if (match.Success && !noClose)
				{
					inner = this.Text.Substring(tagEndPos + 1, match.Index - tagEndPos - 1);
					tagClose = match.Value;
					this.Index = match.Index + match.Length;
				}
				else if (AllowMissingEndTag.Contains(tagOpen))
				{
					inner = this.Text[(tagEndPos + 1)..];
					tagClose = string.Empty;
					this.Index = this.textLength;
				}
				else
				{
					this.Index = tagEndPos + 1;
					piece.AddLiteral(this.NodeFactory, this.Text[tagStartPos..this.Index]);
					this.noMoreClosingTag.Add(tagOpen);
					return true;
				}
			}

			if (this.ignoredElements.Contains(tagOpen))
			{
				if (this.includeIgnores)
				{
					piece.Nodes.Add(this.NodeFactory.IgnoreNode(this.Text[tagStartPos..this.Index]));
				}
			}
			else
			{
				var attr = attrEnd > attrStart ? this.Text[attrStart..attrEnd] : null;
				piece.Nodes.Add(this.NodeFactory.TagNode(tagOpen, attr, inner, tagClose));
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

			if (this.Text[0] == '=')
			{
				this.ParseLineStart();
			}

			while (this.Index < this.textLength)
			{
				if (this.findOnlyinclude)
				{
					var startPos = this.Text.IndexOf(OnlyIncludeTagOpen, this.Index, StringComparison.OrdinalIgnoreCase);
					if (startPos == -1)
					{
						if (this.includeIgnores)
						{
							this.Top.CurrentPiece.Nodes.Add(this.NodeFactory.IgnoreNode(this.Text[this.Index..]));
						}

						break;
					}

					var tagEndPos = startPos + OnlyIncludeTagOpen.Length; // past-the-end
					if (this.includeIgnores)
					{
						this.Top.CurrentPiece.Nodes.Add(this.NodeFactory.IgnoreNode(this.Text[this.Index..tagEndPos]));
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
					this.Top.CurrentPiece.AddLiteral(this.NodeFactory, this.Text[this.Index..literalOffset]);
					this.Index = literalOffset;
					if (this.Index >= this.textLength)
					{
						break;
					}
				}

				this.Top.Parse(this.CurrentCharacter);
			}

			var lastHeader = this.Top as HeaderElement;
			lastHeader?.Parse('\n');
		}
		#endregion

		#region Private Structures
		[StructLayout(LayoutKind.Auto)]
		private readonly struct Comment(int start, int end, int wsLength)
		{
			public int End { get; } = end;

			public int Start { get; } = start;

			public int WhiteSpaceLength { get; } = wsLength;
		}
		#endregion
	}
}