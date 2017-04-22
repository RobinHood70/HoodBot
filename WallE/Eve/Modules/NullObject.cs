namespace RobinHood70.WallE.Eve.Modules
{
	/// <summary>An object class that does nothing and always returns null.</summary>
	/// <remarks>This class is used in place of a static null value when a class is required.</remarks>
	public sealed class NullObject
	{
		#region Constructors
		private NullObject()
		{
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the null-valued object.</summary>
		/// <value>The null-valued object.</value>
		public static NullObject Null { get; } = new NullObject();
		#endregion

		#region Public Override Methods

		/// <summary>Determines whether the specified <see cref="object" /> is equal to this instance.</summary>
		/// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
		/// <returns><c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
		/// <remarks>All instances of a NullObject are deemed to match all others</remarks>
		public override bool Equals(object obj) => obj is NullObject;

		/// <summary>Returns a hash code for this instance.</summary>
		/// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
		/// <remarks>The hasch code for this object is always 0.</remarks>
		public override int GetHashCode() => 0;

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => null;
		#endregion
	}
}
