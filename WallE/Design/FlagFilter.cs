#pragma warning disable SA1402 // File may only contain a single class; Justification = It makes sense here to have these in one file, and avoids having file names that don't match class names.
namespace RobinHood70.WallE.Design
{
	using System;
	using System.Globalization;

	/// <summary>A static class whose only purpose is to create a FlagFilter using type inference.</summary>
	public static class FlagFilter
	{
		#region Public Methods

		/// <summary>Create a typed FlagFilter instance for checking.</summary>
		/// <typeparam name="T">The type of the flags enumeration (inferred).</typeparam>
		/// <param name="siteVersion">The MediaWiki site version, expressed as an integer.</param>
		/// <param name="originalValue">The value to filter.</param>
		/// <returns>The input value with any relevant flags cleared.</returns>
		public static FlagFilter<T> Check<T>(int siteVersion, T originalValue)
			where T : struct, Enum => new(siteVersion, originalValue);
		#endregion
	}

	/// <summary>A class for filtering flags enumerations based on the MediaWiki site version.</summary>
	/// <typeparam name="T">The type of the flags filter to be checked.</typeparam>
	/// <remarks>Initializes a new instance of the <see cref="FlagFilter{T}" /> class.</remarks>
	/// <param name="siteVersion">The site version.</param>
	/// <param name="originalValue">The original value.</param>
	public sealed class FlagFilter<T>(int siteVersion, T originalValue)
		where T : struct, Enum
	{
		#region Fields
		private ulong longValue = Convert.ToUInt64(originalValue, CultureInfo.InvariantCulture);
		#endregion

		#region Public Properties

		/// <summary>Gets the current flags value, with any filters applied to this point.</summary>
		/// <value>The current flags value, with any filters applied to this point.</value>
		public T Value => (T)Enum.ToObject(typeof(T), this.longValue);
		#endregion

		#region Public Methods

		/// <summary>Filters flags that do not exist before a specific version.</summary>
		/// <param name="version">The version.</param>
		/// <param name="flagFilter">The flag(s) to filter.</param>
		/// <returns>The input flags, filtered appropriately.</returns>
		public FlagFilter<T> FilterBefore(int version, T flagFilter)
		{
			if (siteVersion < version)
			{
				this.longValue &= ~Convert.ToUInt64(flagFilter, CultureInfo.InvariantCulture);
			}

			return this;
		}

		/// <summary>Filters flags that do not exist from a specific version onwards.</summary>
		/// <param name="version">The version.</param>
		/// <param name="flagFilter">The flag(s) to filter.</param>
		/// <returns>The input flags, filtered appropriately.</returns>
		public FlagFilter<T> FilterFrom(int version, T flagFilter)
		{
			if (siteVersion >= version)
			{
				this.longValue &= ~Convert.ToUInt64(flagFilter, CultureInfo.InvariantCulture);
			}

			return this;
		}
		#endregion
	}
}