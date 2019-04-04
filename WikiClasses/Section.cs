namespace RobinHood70.WikiClasses
{
	using System.Collections.Generic;
	using System.Text;
	using static WikiCommon.Globals;

	/// <summary>Represents a section on a wiki page.</summary>
	/// <remarks>Note that this class is fairly simplistic and geared primarily to representation and manipulation of existing wikitext. It does not attempt to deal with unexpected usage such as adding sections via the <see cref="Text"/> property or commenting out a section header.</remarks>
	public class Section
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		public Section()
			: this(null, 0, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="level">The level (number of equals signs in the title).</param>
		public Section(string title, int level)
			: this(title, level, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Section"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="level">The level.</param>
		/// <param name="text">The text.</param>
		public Section(string title, int level, string text)
		{
			this.Title = new PaddedString(title);
			this.Level = level;
			this.Text = text;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the text to add after the section title.</summary>
		/// <value>The text to add after the title.</value>
		/// <remarks>While normally blank, this property allows insertion of text after the section title. This may include an HTML comment, a noinclude tag, or similar text that should be inserted after any <c>==</c>-type title text. All whitespace is the responsibility of the caller.</remarks>
		public string AddAfterTitle { get; set; }

		/// <summary>Gets or sets the text to add before the section title.</summary>
		/// <value>The text to add before the title.</value>
		/// <remarks>While normally blank, this property allows insertion of text before the section title. This may include prepended text, a noinclude tag, or similar text that should be inserted before any <c>==</c>-type title text. All whitespace is the responsibility of the caller.</remarks>
		public string AddBeforeTitle { get; set; }

		/// <summary>Gets or sets the section level.</summary>
		/// <value>The section level.</value>
		public int Level { get; set; }

		/// <summary>Gets or sets the title, including surrounding whitespace.</summary>
		/// <value>The padded title.</value>
		public PaddedString PaddedTitle { get; set; }

		/// <summary>Gets the subsections of the current section.</summary>
		/// <value>The subsections.</value>
		public IList<Section> Subsections { get; } = new List<Section>();

		/// <summary>Gets or sets the section text.</summary>
		/// <value>The text.</value>
		/// <remarks>Section text will always start on a new line and have a trailing NewLine. Any other whitespace is the responsibility of the caller. Updates to the text that include new sections will <i>not</i> be reflected in the class.</remarks>
		public string Text { get; set; }

		/// <summary>Gets or sets the section title, ignoring surrounding whitespace.</summary>
		/// <value>The title.</value>
		public string Title
		{
			get => this.PaddedTitle?.Value;
			set
			{
				if (value == null)
				{
					this.PaddedTitle = null;
				}
				else
				{
					this.PaddedTitle = this.PaddedTitle ?? new PaddedString();
					this.PaddedTitle.Value = value;
				}
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Builds the section into the specified StringBuilder.</summary>
		/// <param name="sb">  The StringBuilder to build into.</param>
		/// <returns>The StringBuilder that was passed, to allow method chaining.</returns>
		public StringBuilder Build(StringBuilder sb)
		{
			ThrowNull(sb, nameof(sb));
			if (this.PaddedTitle != null)
			{
				var equals = "======".Substring(0, this.Level);
				sb
					.Append(this.AddBeforeTitle)
					.Append(equals);
				this.PaddedTitle.Build(sb)
					.Append(this.Title)
					.Append(equals)
					.Append(this.AddAfterTitle)
					.Append('\n');
			}

			sb.Append(this.Text);
			foreach (var section in this.Subsections)
			{
				sb.Append('\n');
				section.Build(sb);
			}

			return sb;
		}

		/// <summary>Finds the subsection with the specified title.</summary>
		/// <param name="title">The title.</param>
		/// <returns>The subsection with the specified title, or null if no section with the title was found.</returns>
		public Section Find(string title)
		{
			foreach (var section in this.Subsections)
			{
				if (section.Title == title)
				{
					return section;
				}
			}

			// Separate loops so that all of current level is checked before trying subtitles.
			foreach (var section in this.Subsections)
			{
				var subTitle = section.Find(title);
				if (subTitle != null)
				{
					return subTitle;
				}
			}

			return null;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a string that represents the section.</summary>
		/// <returns>A <see cref="string"/> that represents the section.</returns>
		public override string ToString() => this.Title;
		#endregion
	}
}