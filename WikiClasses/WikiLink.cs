namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using static RobinHood70.WikiCommon.Globals;

	public class WikiLink
	{
		#region Constructors
		public WikiLink()
		{
		}

		public WikiLink(string link)
		{
			ThrowNull(link, nameof(link));
			link = link.Trim();
			if (link.StartsWith("[[", StringComparison.Ordinal) && link.EndsWith("]]", StringComparison.Ordinal))
			{
				link = link.Substring(2, link.Length - 4);
				link = link.Trim();
			}

			var split = link.Split(new[] { '|' }, 2);
			var page = split[0];
			if (split.Length == 2)
			{
				this.DisplayText = split[1];
			}

			if (page[0] == ':')
			{
				this.ForceLink = true;
				page = page.Substring(1);
			}

			split = page.Split(new[] { ':' }, 2);
			if (split.Length == 1)
			{
				this.Namespace = string.Empty;
			}
			else
			{
				this.Namespace = split[0];
			}

			var pageName = split[split.Length - 1].Trim();
			if (pageName.Length == 0)
			{
				this.PageName = pageName;
			}
			else
			{
				var fragmentSplit = pageName.Split(new[] { '#' }, 2);
				this.PageName = fragmentSplit[0];
				if (fragmentSplit.Length == 2)
				{
					this.Fragment = fragmentSplit[1];
				}
			}
		}

		public WikiLink(Match linkFinderMatch)
		{
			if (linkFinderMatch == null)
			{
				return;
			}

			this.ForceLink = linkFinderMatch.Groups["pre"].Value.Length > 0;
			this.Namespace = linkFinderMatch.Groups["namespace"].Value;
			this.PageName = linkFinderMatch.Groups["pagename"].Value;
			this.Fragment = linkFinderMatch.Groups["fragment"].Value;
			this.DisplayText = linkFinderMatch.Groups["displaytext"].Value;
		}
		#endregion

		#region Public Properties
		public string DisplayText { get; set; }

		public bool ForceLink { get; set; }

		public string FullPageName => this.Namespace + ':' + this.PageName;

		public string Fragment { get; set; }

		public string Namespace { get; set; }

		public string PageName { get; set; }

		public string RootPage => this.Namespace + ':' + this.PageName.Split(new[] { '/' }, 2)[0];
		#endregion

		#region Public Static Methods
		public static string EnumerableRegex(IEnumerable<string> input, bool ignoreInitialCaps)
		{
			if (input != null)
			{
				var sb = new StringBuilder();
				foreach (var name in input)
				{
					sb.Append('|');
					if (name.Length > 0)
					{
						sb.Append("(?i:" + Regex.Escape(name[0].ToString()) + ")");
						if (name.Length > 1)
						{
							var nameRemainder = Regex.Escape(name.Substring(1));
							if (ignoreInitialCaps)
							{
								nameRemainder = nameRemainder.Replace(@"\ ", @"[_\ ]+");
							}

							sb.Append(nameRemainder);
						}
					}
				}

				if (sb.Length > 0)
				{
					return sb.ToString(1, sb.Length - 1);
				}
			}

			return null;
		}

		public static WikiLink FromParts(string ns, string name) => FromParts(ns, name, name);

		public static WikiLink FromParts(string ns, string pageName, string displayText) => new WikiLink()
		{
			Namespace = ns,
			PageName = pageName,
			DisplayText = displayText,
		};

		public static bool IsLink(string value) =>
			value != null &&
			value.Length > 4 &&
			value[0] == '[' &&
			value[1] == '[' &&
			value[value.Length - 2] == ']' &&
			value[value.Length - 1] == ']' &&
			value.Substring(2, value.Length - 4).IndexOfAny(new[] { '[', ']' }) == -1;

		public static Regex LinkFinder() => LinkFinder(null, null, null, null, null);

		public static Regex LinkFinder(IEnumerable<string> namespaces, IEnumerable<string> pageNames, IEnumerable<string> displayTexts) => LinkFinder(null, namespaces, pageNames, displayTexts, null);

		public static Regex LinkFinder(string regexBefore, IEnumerable<string> namespaces, IEnumerable<string> pageNames, IEnumerable<string> displayTexts, string regexAfter) =>
			LinkFinderRaw(
				regexBefore,
				EnumerableRegex(namespaces, true),
				EnumerableRegex(pageNames, true),
				EnumerableRegex(displayTexts, false),
				regexAfter);

		public static Regex LinkFinderRaw(string regexBefore, string regexNamespace, string regexPageName, string regexDisplayText, string regexAfter)
		{
			const string regexWild = ".*?";
			if (regexBefore != null)
			{
				regexBefore = @"(?<before>" + regexBefore + ")";
			}

			if (regexNamespace == null)
			{
				regexNamespace = regexWild;
			}

			if (regexPageName == null)
			{
				regexPageName = regexWild;
			}

			if (regexDisplayText == null)
			{
				regexDisplayText = regexWild;
			}

			if (regexAfter != null)
			{
				regexAfter = @"(?<after>" + regexAfter + ")";
			}

			// Use string check in case .*? was passed as a parameter instead of null.
			var nsOptional = regexNamespace == regexWild ? "?" : string.Empty;
			var displayOptional = regexDisplayText == regexWild ? "?" : string.Empty;

			return new Regex(regexBefore + @"\[\[(?<pre>:)?\s*((?<namespace>" + regexNamespace + "):)" + nsOptional + @"(?<pagename>" + regexPageName + @")(\#(?<fragment>.*?))?(\s*\|\s*(?<displaytext>" + regexDisplayText + @"))" + displayOptional + @"\s*]]" + regexAfter);
		}
		#endregion

		#region Public Methods
		public void Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder.Append("[[");
			if (this.ForceLink)
			{
				builder.Append(':');
			}

			builder.Append(this.Namespace);
			if (!string.IsNullOrEmpty(this.Namespace))
			{
				builder.Append(':');
			}

			builder.Append(this.PageName);
			if (!string.IsNullOrEmpty(this.Fragment))
			{
				builder.Append('#');
				builder.Append(this.Fragment);
			}

			if (!string.IsNullOrEmpty(this.DisplayText))
			{
				builder.Append('|');
				builder.Append(this.DisplayText);
			}

			builder.Append("]]");
		}

		public string PipeTrick() => this.PipeTrick(false);

		public string PipeTrick(bool useFragmentIfPresent)
		{
			string retval;
			if (useFragmentIfPresent && !string.IsNullOrWhiteSpace(this.Fragment))
			{
				retval = this.Fragment;
			}
			else
			{
				retval = this.PageName ?? string.Empty;
				var split = retval.Split(new[] { ',' }, 2);
				if (split.Length == 1)
				{
					var lastIndex = retval.LastIndexOf('(');
					if (retval.LastIndexOf(')') > lastIndex)
					{
						retval = retval.Substring(0, lastIndex);
					}
				}
				else
				{
					retval = split[0];
				}
			}

			return retval.Replace('_', ' ').Trim();
		}
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			var sb = new StringBuilder();
			this.Build(sb);
			return sb.ToString();
		}
		#endregion
	}
}