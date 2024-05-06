namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	/// <summary>Stores information about a single user contribution.</summary>
	/// <seealso cref="Revision" />
	public class Contribution : Revision
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Contribution" /> class.</summary>
		/// <param name="site">The site the contribution is from.</param>
		/// <param name="contribution">The contribution.</param>
		protected internal Contribution(Site site, UserContributionsItem contribution)
			: base(contribution)
		{
			ArgumentNullException.ThrowIfNull(contribution);
			this.Title = TitleFactory.CoValidate(site, contribution.Namespace, contribution.Title ?? string.Empty);
			this.New = contribution.Flags.HasAnyFlag(UserContributionFlags.New);
			this.Patrolled = contribution.Flags.HasAnyFlag(UserContributionFlags.Patrolled);
			contribution.Title.PropertyThrowNull(nameof(contribution));
			this.NewSize = contribution.Size;
			this.OldSize = contribution.Size - contribution.SizeDifference;
			this.Tags = contribution.Tags;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="Contribution" /> is new.</summary>
		/// <value><see langword="true" /> if it's new; otherwise, <see langword="false" />.</value>
		public bool New { get; }

		/// <summary>Gets a value indicating whether this <see cref="Contribution" /> is patrolled.</summary>
		/// <value><see langword="true" /> if it's patrolled; otherwise, <see langword="false" />.</value>
		public bool Patrolled { get; }

		/// <summary>Gets the size of the text after the edit.</summary>
		/// <value>The new size.</value>
		public int NewSize { get; }

		/// <summary>Gets the size of the text before the edit.</summary>
		/// <value>The old size.</value>
		public int OldSize { get; }

		/// <summary>Gets any tags that were applied to the edit.</summary>
		/// <value>The tags.</value>
		public IReadOnlyList<string> Tags { get; }

		/// <summary>Gets the page title.</summary>
		/// <value>The title.</value>
		public Title Title { get; }
		#endregion
	}
}