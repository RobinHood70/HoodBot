namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>"Flattens" text to remove wiki markup and then compares the generated text for equality.</summary>
	public class FlatteningComparer : IEqualityComparer<string>, IEqualityComparer<IEnumerable<IWikiNode>>
	{
		#region Fields
		private readonly Context context;
		private readonly IEqualityComparer<string> comparer;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FlatteningComparer"/> class.</summary>
		/// <param name="context">The context for the text parsing.</param>
		/// <param name="subComparer">The string comparer to use to compare strings once the initial flattening has been performed (e.g., StringComparer.CurrentCulture).</param>
		public FlatteningComparer(Context context, IEqualityComparer<string> subComparer)
		{
			ArgumentNullException.ThrowIfNull(context);
			ArgumentNullException.ThrowIfNull(subComparer);
			this.context = context;
			this.comparer = subComparer;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a function that will be run before doing the final string comparison. Note that this applies to both node-based and string-based comparison, since they're both ultimately resolved to strings.</summary>
		/// <remarks>Strings cannot be null by this point, only empty strings, so null-handling is not required.</remarks>
		public Func<string, string>? ParseBeforeStringCompare { get; set; }
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public bool Equals(IEnumerable<IWikiNode>? x, IEnumerable<IWikiNode>? y)
		{
			if (x is null)
			{
				return y is null;
			}

			if (y is null)
			{
				return false;
			}

			var flatX = ParseToText.Build(x, this.context);
			var flatY = ParseToText.Build(y, this.context);
			if (this.ParseBeforeStringCompare is not null)
			{
				flatX = this.ParseBeforeStringCompare(flatX);
				flatY = this.ParseBeforeStringCompare(flatY);
			}

			return this.comparer.Equals(flatX, flatY);
		}

		/// <inheritdoc/>
		public bool Equals(string? x, string? y)
		{
			if (x is null)
			{
				return y is null;
			}

			if (y is null)
			{
				return false;
			}

			var flatX = ParseToText.Build(x, this.context);
			var flatY = ParseToText.Build(y, this.context);
			if (this.ParseBeforeStringCompare is not null)
			{
				flatX = this.ParseBeforeStringCompare(flatX);
				flatY = this.ParseBeforeStringCompare(flatY);
			}

			/*
			Debug.WriteLine('"' + flatX + '"');
			Debug.WriteLine('"' + flatY + '"');
			var retval = this.comparer.Equals(flatX, flatY);
			Debug.WriteLine(retval);
			*/
			return this.comparer.Equals(flatX, flatY);
		}

		/// <inheritdoc/>
		public int GetHashCode([DisallowNull] string obj) => HashCode.Combine(obj);

		/// <inheritdoc/>
		public int GetHashCode([DisallowNull] IEnumerable<IWikiNode> obj) => HashCode.Combine(obj);
		#endregion
	}
}