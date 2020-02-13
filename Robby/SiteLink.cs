namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations

	/// <summary>The parameter type of a given value.</summary>
	public enum ParameterType
	{
		/// <summary>HTML alt text parameter.</summary>
		Alternate,

		/// <summary>Border parameter.</summary>
		Border,

		/// <summary>Caption parameter.</summary>
		Caption,

		/// <summary>Class parameter.</summary>
		Class,

		/// <summary>Format parameter.</summary>
		Format,

		/// <summary>Horizontal alignment parameter.</summary>
		Halign,

		/// <summary>Language parameter.</summary>
		Language,

		/// <summary>Link parameter.</summary>
		Link,

		/// <summary>Page parameter.</summary>
		Page,

		/// <summary>Size parameter.</summary>
		Size,

		/// <summary>Upright parameter.</summary>
		Upright,

		/// <summary>Vertical alignment parameter.</summary>
		Valign,
	}
	#endregion

	/// <summary>Represents a link with site-specific Title information and parameters in the site's language.</summary>
	public class SiteLink
	{
		#region Static Fields
		private static readonly Dictionary<string, ParameterType> DirectValues = new Dictionary<string, ParameterType>();

		private static readonly List<(ParameterType ParameterType, string Before, string After)> ImageParameterInfo = new List<(ParameterType ParameterType, string Before, string After)>();

		private static readonly Dictionary<string, ParameterType> ImageWords = new Dictionary<string, ParameterType>()
		{
			["img_baseline"] = ParameterType.Valign, // no params
			["img_sub"] = ParameterType.Valign, // no params
			["img_super"] = ParameterType.Valign, // no params
			["img_top"] = ParameterType.Valign, // no params
			["img_text_top"] = ParameterType.Valign, // no params
			["img_middle"] = ParameterType.Valign, // no params
			["img_bottom"] = ParameterType.Valign, // no params
			["img_text_bottom"] = ParameterType.Valign, // no params
			["img_alt"] = ParameterType.Alternate, // has param
			["img_border"] = ParameterType.Border, // no params
			["img_class"] = ParameterType.Class, // has param
			["img_framed"] = ParameterType.Format, // no params
			["img_frameless"] = ParameterType.Format, // no params
			["img_thumbnail"] = ParameterType.Format, // no params
			["img_manualthumb"] = ParameterType.Format, // has param (if no match for set direct value, set thumb=value)
			["img_lang"] = ParameterType.Language, // has param
			["img_link"] = ParameterType.Link, // has param
			["img_right"] = ParameterType.Halign, // no params
			["img_left"] = ParameterType.Halign, // no params
			["img_center"] = ParameterType.Halign, // no params
			["img_none"] = ParameterType.Halign, // no params
			["img_page"] = ParameterType.Page, // has param
			["img_width"] = ParameterType.Size, // has param
			["img_upright"] = ParameterType.Upright, // optional param (use 0 = none)
		};

		private static readonly InvalidOperationException NonNumeric = new InvalidOperationException(Resources.SizeInvalid);

		private static readonly Dictionary<ParameterType, string> PreferredWords = new Dictionary<ParameterType, string>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="title">The title.</param>
		public SiteLink(TitleParts title)
		{
			ThrowNull(title, nameof(title));
			var site = title.Site;
			this.Title = title;
			this.TitleWhitespaceAfter = string.Empty;
			this.TitleWhitespaceBefore = string.Empty;
			if (ImageParameterInfo.Count == 0)
			{
				foreach (var word in ImageWords)
				{
					var magic = site.MagicWords[word.Key];
					foreach (var alias in magic.Aliases)
					{
						var split = alias.Split("$1", 2);
						if (split.Length == 1)
						{
							DirectValues.Add(alias, word.Value);
							if ((word.Value == ParameterType.Border || word.Value == ParameterType.Upright) && !PreferredWords.ContainsKey(word.Value))
							{
								PreferredWords.Add(word.Value, alias);
							}
						}
						else
						{
							ImageParameterInfo.Add((word.Value, split[0], split[1]));
						}
					}
				}
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the alt text for the image.</summary>
		/// <value>The alt text.</value>
		public string? AltText
		{
			get => this.GetValue(ParameterType.Alternate);
			set => this.SetParameterValue(ParameterType.Alternate, value);
		}

		/// <summary>Gets or sets a value indicating whether a border should be displayed.</summary>
		/// <value><see langword="true"/> to display a border; otherwise, <see langword="false"/>.</value>
		/// <remarks>Use this property to insert the default border text in the wiki's language.</remarks>
		public bool Border
		{
			get => this.GetValue(ParameterType.Border) != null;
			set => this.SetDirectValue(ParameterType.Border, value ? PreferredWords[ParameterType.Border] : null);
		}

		/// <summary>Gets or sets the class for the image.</summary>
		/// <value>The class for the image.</value>
		public string? Class
		{
			get => this.GetValue(ParameterType.Class);
			set => this.SetParameterValue(ParameterType.Class, value);
		}

		/// <summary>Gets or sets the image dimensions directly.</summary>
		/// <value>The image dimensions.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public string? Dimensions
		{
			get => this.GetValue(ParameterType.Size);
			set
			{
				this.Parameters.Remove(ParameterType.Upright);
				this.SetParameterValue(ParameterType.Size, value);
			}
		}

		/// <summary>Gets or sets the format (i.e., thumbnail, frame, frameless).</summary>
		/// <value>The format.</value>
		public string? Format
		{
			get => this.GetValue(ParameterType.Format);
			set
			{
				if (!this.SetDirectValue(ParameterType.Format, value))
				{
					// If the value is recognized via SetDirectValue, use it. Otherwise, this should find the only option with a parameter (manualthumb). If there end up being more options with parameters in the future, something else will need to be done here.
					this.SetParameterValue(ParameterType.Format, value);
				}
			}
		}

		/// <summary>Gets or sets the image height.</summary>
		/// <value>The image height.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public int Height
		{
			get => this.GetSize().Height;
			set => this.SetSize(value, this.Width);
		}

		/// <summary>Gets or sets the image's horizontal alignment.</summary>
		/// <value>The image's horizontal alignment.</value>
		public string? HorizontalAlignment
		{
			get => this.GetValue(ParameterType.Halign);
			set => this.SetDirectValue(ParameterType.Halign, value);
		}

		/// <summary>Gets or sets the image's language, for image formats that are language-aware (e.g., SVG).</summary>
		/// <value>The image language.</value>
		public string? Language
		{
			get => this.GetValue(ParameterType.Language);
			set => this.SetParameterValue(ParameterType.Language, value);
		}

		/// <summary>Gets or sets the link for the image.</summary>
		/// <value>The link for the image.</value>
		public string? Link
		{
			get => this.GetValue(ParameterType.Link);
			set => this.SetParameterValue(ParameterType.Link, value);
		}

		/// <summary>Gets or sets the <see cref="Robby.Page"/> value as an integer.</summary>
		/// <value>The page value.</value>
		public int? Page
		{
			get => int.Parse(this.GetValue(ParameterType.Page) ?? "0", CultureInfo.InvariantCulture);
			set => this.SetParameterValue(ParameterType.Page, value?.ToStringInvariant());
		}

		/// <summary>Gets the raw parameter information.</summary>
		/// <value>The parameters.</value>
		/// <remarks>Parameters can be used to change low-level information, such as spacing around the parameters or choosing an alternate language for language-specific parameters. Note that these are not checked in any way, and incorrect data could cause unexpected behaviour or errors.</remarks>
		public Dictionary<ParameterType, EmbeddedValue> Parameters { get; } = new Dictionary<ParameterType, EmbeddedValue>();

		/// <summary>Gets or sets the display text (i.e., the value to the right of the pipe). For categories, this is the sortkey; for images, this is the caption.</summary>
		public string? Text
		{
			get => this.GetValue(ParameterType.Caption);
			set => this.SetDirectValue(ParameterType.Caption, value);
		}

		/// <summary>Gets or sets the title of the link.</summary>
		/// <value>The title.</value>
		public TitleParts Title { get; set; }

		/// <summary>Gets or sets the whitespace after the title.</summary>
		/// <value>The whitespace after the title.</value>
		public string TitleWhitespaceAfter { get; set; }

		/// <summary>Gets or sets the whitespace before the title.</summary>
		/// <value>The whitespace before the title.</value>
		public string TitleWhitespaceBefore { get; set; }

		/// <summary>Gets or sets the upright value as a number.</summary>
		/// <value>The upright value.</value>
		/// <remarks>Setting this option will remove any <see cref="Dimensions"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public double? Upright
		{
			get
			{
				var textValue = this.GetValue(ParameterType.Upright);
				return
					textValue == null ? (double?)null :
					textValue.TrimEnd().Length == 0 ? 1 :
					double.TryParse(textValue, out var retval) ? retval :
					double.NaN;
			}

			set
			{
				if (value == null)
				{
					this.Parameters.Remove(ParameterType.Upright);
				}
				else if (!double.IsNegative(value.Value))
				{
					this.Parameters.Remove(ParameterType.Size);
					if (value == 0)
					{
						this.SetDirectValue(ParameterType.Upright, PreferredWords[ParameterType.Upright]);
					}
					else
					{
						this.SetParameterValue(ParameterType.Upright, value?.ToStringInvariant());
					}
				}
			}
		}

		/// <summary>Gets or sets the image width.</summary>
		/// <value>The image width.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public int Width
		{
			get => this.GetSize().Width;
			set => this.SetSize(value, this.Height);
		}

		/// <summary>Gets or sets the image's vertical alignment.</summary>
		/// <value>The vertical alignment.</value>
		public string? VerticalAlignment
		{
			get => this.GetValue(ParameterType.Valign);
			set => this.SetDirectValue(ParameterType.Valign, value);
		}
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new SiteLink instance from a <see cref="LinkNode"/>.</summary>
		/// <param name="site">The site the link is from.</param>
		/// <param name="link">The link node.</param>
		/// <returns>A new SiteLink.</returns>
		public static SiteLink FromLinkNode(Site site, LinkNode link)
		{
			ThrowNull(link, nameof(link));
			var titleText = WikiTextVisitor.Value(link.Title);
			var valueSplit = SplitWhitespace(titleText);
			var title = new TitleParts(site, valueSplit.Value);
			var retval = new SiteLink(title)
			{
				TitleWhitespaceBefore = valueSplit.Before,
				TitleWhitespaceAfter = valueSplit.After
			};

			foreach (var subNode in link.Parameters)
			{
				if (subNode is ParameterNode parameter)
				{
					var valueRaw = WikiTextVisitor.Raw(parameter.Value);
					retval.AddValueDirect(valueRaw);
				}
			}

			return retval;
		}

		/// <summary>Creates a new SiteLink instance from the provided text.</summary>
		/// <param name="site">The site the link is from.</param>
		/// <param name="link">The text of the link.</param>
		/// <returns>A new SiteLink.</returns>
		/// <remarks>The text may include or exclude surrounding brackets. Pipes in the text are handled properly either way in order to support gallery links.</remarks>
		public static SiteLink FromText(Site site, string link)
		{
			ThrowNull(link, nameof(link));
			if (!link.StartsWith("[[", StringComparison.Ordinal) || !link.EndsWith("]]", StringComparison.Ordinal))
			{
				link = "[[" + link + "]]";
			}

			var linkNode = LinkNode.FromText(link);
			return FromLinkNode(site, linkNode);
		}
		#endregion

		#region Public Methods

		/// <summary>Gets the image size.</summary>
		/// <returns>The image height and width. If either value is missing, a zero will be returned for that value.</returns>
		/// <exception cref="InvalidOperationException">The size text is invalid, and could not be parsed.</exception>
		public (int Height, int Width) GetSize()
		{
			if (this.Dimensions is string dimensions)
			{
				var split = dimensions.Split('x', 2);
				return split.Length switch
				{
					1 => (0, int.TryParse(split[0], out var result) ? result : throw NonNumeric),
					2 => (int.TryParse("0" + split[0], out var width) ? width : throw NonNumeric, int.TryParse("0" + split[1], out var height) ? height : throw NonNumeric),
					_ => (0, 0)
				};
			}

			return (0, 0);
		}

		/// <summary>Sets the image size, formatting the <see cref="Dimensions"/> paramter appropriately.</summary>
		/// <param name="height">The height.</param>
		/// <param name="width">The width.</param>
		public void SetSize(int height, int width)
		{
			if (height == 0)
			{
				if (width == 0)
				{
					this.Parameters.Remove(ParameterType.Size);
				}
				else
				{
					this.Dimensions = width.ToStringInvariant();
				}
			}
			else
			{
				this.Dimensions = (width == 0 ? string.Empty : width.ToStringInvariant()) + "x" + height.ToStringInvariant();
			}
		}

		/// <summary>Converts to the link to a <see cref="LinkNode"/>.</summary>
		/// <returns>A <see cref="LinkNode"/> containing the parsed link text.</returns>
		public LinkNode ToLinkNode()
		{
			var values = new List<string>();
			foreach (var parameter in this.Parameters)
			{
				var text = parameter.Value.ToString();
				values.Add(text);
			}

			return LinkNode.FromParts(this.TitleWhitespaceBefore + this.Title.ToString() + this.TitleWhitespaceAfter, values);
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns the full text of the link.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => WikiTextVisitor.Raw(this.ToLinkNode());
		#endregion

		#region Private Static Methods
		private static EmbeddedValue SplitWhitespace(string titleText)
		{
			var value = new EmbeddedValue();
			var index = 0;
			while (index < titleText.Length && char.IsWhiteSpace(titleText[index]))
			{
				index++;
			}

			value.Before = string.Empty;
			if (index > 0)
			{
				value.Before = titleText.Substring(0, index);
				titleText = titleText.Substring(index);
			}

			index = titleText.Length;
			while (index > 0 && char.IsWhiteSpace(titleText[index - 1]))
			{
				index--;
			}

			value.After = string.Empty;
			if (index < titleText.Length)
			{
				value.After = titleText.Substring(index);
				titleText = titleText.Substring(0, index);
			}

			value.Value = titleText;
			return value;
		}
		#endregion

		#region Private Methods
		private void AddValueDirect(string value)
		{
			ThrowNull(value, nameof(value));
			var parameter = SplitWhitespace(value);
			if (!DirectValues.TryGetValue(parameter.Value, out var parameterType))
			{
				parameterType = ParameterType.Caption;
				foreach (var paramValue in ImageParameterInfo)
				{
					if (parameter.Value.StartsWith(paramValue.Before, StringComparison.Ordinal) && parameter.Value.EndsWith(paramValue.After, StringComparison.Ordinal))
					{
						parameterType = paramValue.ParameterType;
						parameter.Before += paramValue.Before;
						parameter.Value = parameter.Value.Substring(paramValue.Before.Length, parameter.Value.Length - paramValue.Before.Length - paramValue.After.Length);
						parameter.After = paramValue.After + parameter.After;
						break;
					}
				}
			}

			this.Parameters.Add(parameterType, parameter);
		}

		private string? GetValue(ParameterType name) => this.Parameters.TryGetValue(name, out var value) ? value.Value : null;

		private bool SetDirectValue(ParameterType parameterType, string? value)
		{
			var retval = true;
			if (value == null)
			{
				this.Parameters.Remove(parameterType);
			}
			else if (this.Parameters.TryGetValue(parameterType, out var param))
			{
				param.Value = value;
			}
			else if (DirectValues.TryGetValue(value, out var valueType) && parameterType == valueType)
			{
				var paramValue = new EmbeddedValue(value);
				this.Parameters.Add(parameterType, paramValue);
			}
			else
			{
				retval = false;
			}

			return retval;
		}

		private void SetParameterValue(ParameterType parameterType, string? value)
		{
			if (value == null)
			{
				this.Parameters.Remove(parameterType);
			}
			else if (this.Parameters.TryGetValue(parameterType, out var parameter))
			{
				parameter.Value = value;
			}
			else
			{
				foreach (var info in ImageParameterInfo)
				{
					if (info.ParameterType == parameterType)
					{
						this.Parameters.Add(parameterType, new EmbeddedValue(info.Before, value, info.After));
						break;
					}
				}
			}
		}
		#endregion
	}
}
