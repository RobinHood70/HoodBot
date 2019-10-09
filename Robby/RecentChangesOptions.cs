namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Stores options for a Recent Changes filter. This can be used to create a Recent Changes query that's more complex than allowed by the other built-in methods, as well as being used internally by those methods.</summary>
	public class RecentChangesOptions
	{
		#region Public Properties

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they were made by an anonymous (IP) user or a logged-in user.</summary>
		/// <value><c>Any</c> to retrieve all changes; <c>Only</c> to retrieve only anonymous changes; <c>Exclude</c> to retrieve only changes by logged in users.</value>
		public Filter Anonymous { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they were made by a bot.</summary>
		/// <value><c>Any</c> to retrieve all changes; <c>Only</c> to retrieve only bot changes; <c>Exclude</c> to retrieve everything except bot changes.</value>
		public Filter Bots { get; set; }

		/// <summary>Gets the number of changes to retrieve.</summary>
		/// <value>The number of changes to retrieve.</value>
		public int Count { get; internal set; }

		/// <summary>Gets or sets the date and time at which to stop retrieving changes.</summary>
		/// <value>The date and time at which to end.</value>
		public DateTime? End { get; set; }

		/// <summary>Gets or sets a value indicating whether to exclude the user specified in <see cref="User"/> or include them.</summary>
		/// <value><see langword="true"/> if the user specified in <see cref="User"/> should be excluded; otherwise, <see langword="false"/>.</value>
		public bool ExcludeUser { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they're minor.</summary>
		/// <value><c>Any</c> to retrieve all changes; <c>Only</c> to retrieve only minor edits; <c>Exclude</c> to retrieve everything except minor edits.</value>
		public Filter Minor { get; set; }

		/// <summary>Gets or sets the namespaces to retrieve.</summary>
		/// <value>The namespaces to retrieve.</value>
		public IEnumerable<int> Namespaces { get; set; }

		/// <summary>Gets or sets a value indicating whether direction changes should be sorted from older to newer (true) or newer to older (false).</summary>
		/// <value><see langword="true"/> if changes should be sorted older to newer; otherwise, <see langword="false"/>.</value>
		/// <remarks>If only a Start date is provided but no End date, "older to newer" (true) means changes will be returned from the Start date to the current date; "newer to older" (false) means changes will be returned from the Start Date to the earliest Recent Change. Similarly, if only an End date is provided but no Start date, "older to newer" (true) means changes returned will be from the earliest Recent Change up to the End date; "newer to older" (false) means changes will be returned from the latest Recent Change back to the End date. If dates are provided for both Start and End, the API abstraction layer will ignore this value in favour of the direction indicated by the dates (behaviour from any other abstraction layer is not guaranteed). If no dates are provided, all Recent Changes will be retrieved, sorted according to this setting.</remarks>
		public bool Newer { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they're patrolled.</summary>
		/// <value><c>Any</c> to retrieve all changes; <c>Only</c> to retrieve only patrolled changes; <c>Exclude</c> to retrieve only unpatrolled changes.</value>
		public Filter Patrolled { get; set; }

		/// <summary>Gets or sets a value indicating whether to filter edits based on whether they're redirects.</summary>
		/// <value><c>Any</c> to retrieve all changes; <c>Only</c> to retrieve only changes to redirect pages; <c>Exclude</c> to retrieve changes made to all pages except redirects.</value>
		public Filter Redirects { get; set; }

		/// <summary>Gets or sets the date and time at which to start retrieving changes.</summary>
		/// <value>The date and time at which to start.</value>
		public DateTime? Start { get; set; }

		/// <summary>Gets or sets the tag to limit Recent Changes to.</summary>
		/// <value>The tag to limit Recent Changes to.</value>
		public string Tag { get; set; }

		/// <summary>Gets or sets the Recent Change types to load.</summary>
		/// <value>The Recent Change types to load.</value>
		public RecentChangesTypes Types { get; set; }

		/// <summary>Gets or sets the user whose changes should be retrieved (or ignored, if <see cref="ExcludeUser"/> is set).</summary>
		/// <value>The user whose changes should be loaded or ignored.</value>
		public string User { get; set; }
		#endregion

		#region Internal Methods

		/// <summary>Gets a RecentChangesInput object based on the current object.</summary>
		/// <value>The equivalent RecentChangesInput object.</value>
		internal RecentChangesInput ToWallEInput => new RecentChangesInput()
		{
			Start = this.Start,
			End = this.End,
			SortAscending = this.Newer,
			User = this.User,
			ExcludeUser = this.ExcludeUser,
			Namespaces = this.Namespaces,
			Tag = this.Tag,
			Types = this.Types,
			FilterAnonymous = this.Anonymous,
			FilterBots = this.Bots,
			FilterMinor = this.Minor,
			FilterPatrolled = this.Patrolled,
			FilterRedirects = this.Redirects,
			MaxItems = this.Count,
		};
		#endregion
	}
}