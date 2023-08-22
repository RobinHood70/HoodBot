namespace RobinHood70.Robby.Design
{
	using System;

	// TODO: Review constructors for various title objects.

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class FullTitle : IFullTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="fullTitle">The <see cref="IFullTitle"/> with the desired information.</param>
		public FullTitle(IFullTitle fullTitle)
		{
			ArgumentNullException.ThrowIfNull(fullTitle);
			this.Title = fullTitle.Title;
			this.Fragment = fullTitle.Fragment;
			this.Interwiki = fullTitle.Interwiki;
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to downcast.</param>
		public FullTitle(Title title)
		{
			this.Title = title;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
		/// <value>The fragment.</value>
		public string? Fragment { get; set; }

		/// <summary>Gets or sets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry? Interwiki { get; set; }

		/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
		/// <value><see langword="true"/> if this instance is local wiki; otherwise, <see langword="false"/>.</value>
		public bool IsLocal => this.Interwiki?.LocalWiki != false;

		/// <inheritdoc/>
		public Title Title { get; }
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="interwiki">The value returned by <see cref="Interwiki"/>.</param>
		/// <param name="title">The title returned by <see cref="Title"/>.</param>
		/// <param name="fragment">The value returned by <see cref="Fragment"/>.</param>
		public void Deconstruct(out InterwikiEntry? interwiki, out Title title, out string? fragment)
		{
			interwiki = this.Interwiki;
			title = this.Title;
			fragment = this.Fragment;
		}
		#endregion
	}
}