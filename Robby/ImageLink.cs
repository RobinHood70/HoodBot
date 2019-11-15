namespace RobinHood70.Robby
{
	using System;
	using System.Globalization;
	using System.Text;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents an Image link.</summary>
	/// <remarks>
	///   <para>This class is loosely language aware, but does not do any value checking of the various parameters. Be careful not to use invalid values that may be interpreted as a caption by the wiki.</para>
	///   <para>While this class can be used for non-images in File space (e.g., Zip files, if allowed on the wiki), everything but the link page name will be ignored by the wiki parser.</para>
	/// </remarks>
	public class ImageLink : SiteLink
	{
		#region Static Fields
		private static readonly InvalidOperationException NonNumeric = new InvalidOperationException("Size value was non-numeric.");
		#endregion

		#region Fields
		private readonly ParameterCollection parameters = new ParameterCollection();
		private readonly string? borderWord;
		private readonly string? uprightWord;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ImageLink"/> class.</summary>
		/// <param name="site">The Site the link is from.</param>
		/// <param name="link">The link text to parse.</param>
		public ImageLink(Site site, string link)
			: base(site, link)
		{
			ThrowNull(site, nameof(site));
			this.borderWord = site.GetPreferredImageMagicWord(Site.ImageBorderName);
			this.uprightWord = site.GetPreferredImageMagicWord(Site.ImageUprightName);
			if (this.Parser == null)
			{
				throw new InvalidOperationException();
			}

			foreach (var parameter in this.Parser.Parameters)
			{
				foreach (var entry in site.ImageParameterRegexes)
				{
					var match = entry.Value.Match(parameter.Value);
					if (match.Success)
					{
						parameter.Name = entry.Key;
						var value = match.Groups["value"];
						if (value.Success)
						{
							this.ExtractActualValue(parameter.FullValue, value.Index, value.Length);
						}

						break;
					}
				}

				parameter.Name ??= Site.ImageCaptionName;
				this.parameters.Add(parameter);
			}
		}

		/// <summary>Initializes a new instance of the <see cref="ImageLink"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="pageName">The name of the image page.</param>
		/// <param name="caption">The caption.</param>
		public ImageLink(Site site, string pageName, string caption)
			: base((site ?? throw ArgumentNull(nameof(site))).Namespaces[MediaWikiNamespaces.File], pageName)
		{
			this.borderWord = site.GetPreferredImageMagicWord(Site.ImageBorderName);
			this.uprightWord = site.GetPreferredImageMagicWord(Site.ImageUprightName);
			this.Namespace = site.Namespaces[MediaWikiNamespaces.File];
			this.PageName = Title.CoercePageName(site, MediaWikiNamespaces.File, pageName);
			this.DisplayText = caption;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the alt text for the image.</summary>
		/// <value>The alt text.</value>
		public PaddedString? AltText
		{
			get => this.GetValue(Site.ImageAltName);
			set => this.SetParameter(Site.ImageAltName, value);
		}

		/// <summary>Gets or sets the border text. Use <see cref="BorderValue"/> to use the standardized word for your wiki's language.</summary>
		/// <value>The border text.</value>
		public PaddedString? Border
		{
			get => this.GetValue(Site.ImageBorderName);
			set => this.SetParameter(Site.ImageBorderName, value);
		}

		/// <summary>Gets or sets a value indicating whether a border should be displayed.</summary>
		/// <value><see langword="true"/> to display a border; otherwise, <see langword="false"/>.</value>
		/// <remarks>Use this property to insert the default border text in the wiki's language.</remarks>
		public bool BorderValue
		{
			get => this.GetValue(Site.ImageBorderName) != null;
			set => this.SetParameterValue(Site.ImageBorderName, value ? this.borderWord : null);
		}

		/// <summary>Gets or sets the class for the image.</summary>
		/// <value>The class for the image.</value>
		public PaddedString? Class
		{
			get => this.GetValue(Site.ImageClassName);
			set => this.SetParameter(Site.ImageClassName, value);
		}

		/// <summary>Gets or sets the caption for the image.</summary>
		/// <value>The caption for the image.</value>
		public PaddedString? Caption
		{
			get => this.GetValue(Site.ImageCaptionName);
			set => this.SetParameter(Site.ImageCaptionName, value);
		}

		/// <summary>Gets or sets the image dimensions directly.</summary>
		/// <value>The image dimensions.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public PaddedString? Dimensions
		{
			get => this.GetValue(Site.ImageSizeName);
			set
			{
				this.parameters.Remove(Site.ImageUprightName);
				this.SetParameter(Site.ImageSizeName, value);
			}
		}

		/// <summary>Gets or sets the display text. For images, this is an alias for the caption property.</summary>
		public override PaddedString? DisplayParameter
		{
			get => this.parameters[Site.ImageCaptionName]?.FullValue;
			set
			{
				if (value == null)
				{
					this.parameters.Remove(Site.ImageCaptionName);
					return;
				}

				var param = this.parameters[Site.ImageCaptionName];
				if (param == null)
				{
					this.parameters.Add(new Parameter(new PaddedString(Site.ImageCaptionName), value.Clone()));
				}
				else
				{
					param.FullValue.CopyFrom(value);
				}
			}
		}

		/// <summary>Gets or sets the format (i.e., thumbnail, frame, frameless).</summary>
		/// <value>The format.</value>
		public PaddedString? Format
		{
			get => this.GetValue(Site.ImageFormatName);
			set => this.SetParameter(Site.ImageFormatName, value);
		}

		/// <summary>Gets or sets the image height.</summary>
		/// <value>The image height.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public int Height
		{
			get => this.GetSize().height;
			set => this.SetSize(value, this.Width);
		}

		/// <summary>Gets or sets the image's horizontal alignment.</summary>
		/// <value>The image's horizontal alignment.</value>
		public PaddedString? HorizontalAlignment
		{
			get => this.GetValue(Site.ImageHAlignName);
			set => this.SetParameter(Site.ImageHAlignName, value);
		}

		/// <summary>Gets or sets the image's language, for image formats that are language-aware (e.g., SVG).</summary>
		/// <value>The image language.</value>
		public PaddedString? Language
		{
			get => this.GetValue(Site.ImageLanguageName);
			set => this.SetParameter(Site.ImageLanguageName, value);
		}

		/// <summary>Gets or sets the link for the image.</summary>
		/// <value>The link for the image.</value>
		public PaddedString? Link
		{
			get => this.GetValue(Site.ImageLinkName);
			set => this.SetParameter(Site.ImageLinkName, value);
		}

		/// <summary>Gets or sets the page for the document, for formats like PDF and DJVU.</summary>
		/// <value>The page for the document.</value>
		public PaddedString? Page
		{
			get => this.GetValue(Site.ImagePageName);
			set => this.SetParameter(Site.ImagePageName, value);
		}

		/// <summary>Gets or sets the <see cref="Page"/> value as an integer.</summary>
		/// <value>The page value.</value>
		public int PageValue
		{
			get => int.Parse(this.GetRegexValue(Site.ImagePageName) ?? "0", CultureInfo.InvariantCulture);
			set => this.SetParameterValue(Site.ImagePageName, value.ToStringInvariant());
		}

		/// <summary>Gets or sets the upright scaling factor.</summary>
		/// <value>The upright factor.</value>
		/// <remarks>Setting this option will remove any <see cref="Dimensions"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public PaddedString? Upright
		{
			get => this.GetValue(Site.ImageUprightName);
			set
			{
				this.parameters.Remove(Site.ImageSizeName);
				this.SetParameter(Site.ImageUprightName, value);
			}
		}

		/// <summary>Gets or sets the upright value as a number.</summary>
		/// <value>The upright value.</value>
		/// <remarks>Setting this option will remove any <see cref="Dimensions"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public double UprightValue
		{
			get => double.TryParse(this.GetRegexValue(Site.ImageUprightName), out var retval) ? retval : double.NaN;
			set
			{
				this.parameters.Remove(Site.ImageSizeName);
				if (value == 0 || double.IsNaN(value))
				{
					this.parameters.Add(new Parameter(Site.ImageUprightName, this.uprightWord, true));
				}
				else if (value >= 0)
				{
					this.parameters.Add(new Parameter(Site.ImageUprightName, value.ToStringInvariant(), true));
				}
			}
		}

		/// <summary>Gets or sets the image width.</summary>
		/// <value>The image width.</value>
		/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
		public int Width
		{
			get => this.GetSize().width;
			set => this.SetSize(value, this.Height);
		}

		/// <summary>Gets or sets the image's vertical alignment.</summary>
		/// <value>The vertical alignment.</value>
		public PaddedString? VerticalAlignment
		{
			get => this.GetValue(Site.ImageVAlignName);
			set => this.SetParameter(Site.ImageVAlignName, value);
		}
		#endregion

		#region Public Methods

		/// <summary>Gets the image size.</summary>
		/// <returns>The image height and width. If either value is missing, a zero will be returned for that value.</returns>
		/// <exception cref="InvalidOperationException">The size text is invalid, and could not be parsed.</exception>
		public (int height, int width) GetSize()
		{
			var split = this.Dimensions?.Value.Split('x');
			return split?.Length switch
			{
				1 => (0, int.TryParse(split[0], out var result) ? result : throw NonNumeric),
				2 => (int.TryParse("0" + split[0], out var width) ? width : throw NonNumeric, int.TryParse("0" + split[1], out var height) ? height : throw NonNumeric),
				_ => throw new InvalidOperationException(Resources.SizeInvalid)
			};
		}

		/// <summary>Reformats all parameters using the specified formats and sorts them in a standardized order.</summary>
		/// <param name="nameFormat">Whitespace to add before and after the link name. The <see cref="PaddedString.Value"/> property is ignored.</param>
		/// <param name="valueFormat">Whitespace to add before and after every parameter's value. The <see cref="PaddedString.Value"/> property is ignored.</param>
		/// <remarks>Note that this method differs somewhat from the template method of the same name, since all parameters in a link are treated as anonymous due to the flexibility of parameter formats, such as <c>upright</c>/<c>upright 1.0</c>/<c>upright=1.0</c>.</remarks>
		public override void Reformat(PaddedString nameFormat, PaddedString valueFormat)
		{
			if (nameFormat != null)
			{
				this.NameLeadingWhiteSpace = nameFormat.LeadingWhiteSpace;
				this.Normalize();
				this.NameTrailingWhiteSpace = nameFormat.TrailingWhiteSpace;
			}

			if (valueFormat != null)
			{
				foreach (var param in this.parameters)
				{
					param.FullValue.LeadingWhiteSpace = valueFormat.LeadingWhiteSpace;
					param.FullValue.TrailingWhiteSpace = valueFormat.TrailingWhiteSpace;
				}
			}

			this.Sort();
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
					this.parameters.Remove(Site.ImageSizeName);
				}
				else
				{
					this.Dimensions = new PaddedString(width.ToStringInvariant());
				}
			}
			else
			{
				var textHeight = height.ToStringInvariant();
				this.Dimensions = new PaddedString((width != 0 ? width.ToStringInvariant() : string.Empty) + "x" + textHeight);
			}
		}

		/// <summary>Sorts parameters in a pre-determined format.</summary>
		/// <remarks>The format is: format, horizontal alignment, vertical alignment, border, size, upright, alt text, class name, language, link, page number, caption.</remarks>
		public void Sort() => this.parameters.Sort(Site.ImageFormatName, Site.ImageHAlignName, Site.ImageVAlignName, Site.ImageBorderName, Site.ImageSizeName, Site.ImageUprightName, Site.ImageAltName, Site.ImageClassName, Site.ImageLanguageName, Site.ImageLinkName, Site.ImagePageName, Site.ImageCaptionName);

		/// <summary>Sorts parameters in the specified order.</summary>
		/// <param name="order">The order.
		/// Use the Site object's Image*Name string constants as values.</param>
		public void Sort(params string[] order) => this.parameters.Sort(order);
		#endregion

		#region Protected Methods

		/// <summary>Builds the parameter text, if needed.</summary>
		/// <param name="builder">The builder to build into.</param>
		/// <returns>A copy of the <see cref="StringBuilder"/> passed to the method.</returns>
		protected override StringBuilder BuildParameters(StringBuilder builder) => this.parameters.Build(builder);
		#endregion

		#region Private Methods
		private void ExtractActualValue(PaddedString value, int index, int length)
		{
			// We're somewhat abusing whitespace values here, but whitespace was intended to hold anything not directly part of a value, which is basically what we have happening here.
			value.LeadingWhiteSpace += value.Value.Substring(0, index);
			value.TrailingWhiteSpace = value.Value.Substring(index + length) + value.TrailingWhiteSpace;
			value.Value = value.Value.Substring(index, length);
		}

		private string? GetRegexValue(string paramName)
		{
			var param = this.parameters[paramName];
			if (param == null)
			{
				return null;
			}

			if (this.Site.ImageParameterRegexes.TryGetValue(paramName, out var regex))
			{
				var value = regex.Match(param.Value).Groups["value"];
				if (value.Success)
				{
					return value.Value;
				}
			}

			return param.Value;
		}

		private PaddedString? GetValue(string paramName)
		{
			ThrowNull(paramName, nameof(paramName));
			return this.parameters.ValueOrDefault(paramName)?.FullValue;
		}

		private void SetParameter(string paramName, PaddedString? value)
		{
			if (value == null)
			{
				this.parameters.Remove(paramName);
			}
			else
			{
				var param = this.parameters[paramName];
				if (param == null)
				{
					this.parameters.Add(new Parameter(new PaddedString(paramName), value, true));
				}
				else
				{
					param.FullValue.CopyFrom(value);
				}
			}
		}

		private void SetParameterValue(string paramName, string? value)
		{
			if (value == null)
			{
				this.parameters.Remove(paramName);
			}
			else
			{
				var param = this.parameters[paramName];
				if (param == null)
				{
					this.parameters.Add(new Parameter(paramName, value, true));
				}
				else
				{
					param.Value = value;
				}
			}
		}

		#endregion
	}
}