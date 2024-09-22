namespace RobinHood70.WallE.Base
{
	/// <summary>Indicates classes which support limit semantics. Those that do are also extended to support fetch X items semantics.</summary>
	public interface ILimitableInput
	{
		#region Properties

		/// <summary>Gets or sets the number of items to fetch at a time.</summary>
		/// <value>The number of items to fetch at a time.</value>
		int Limit { get; set; }

		/// <summary>Gets or sets the number of items to fetch in total.</summary>
		/// <value>The number of items to fetch in total.</value>
		int MaxItems { get; set; }
		#endregion
	}
}