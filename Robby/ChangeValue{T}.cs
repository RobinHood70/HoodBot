namespace RobinHood70.Robby
{
	using System.Diagnostics.CodeAnalysis;

	/// <summary>Stores the result of a change to the wiki, along with any data associated with the change.</summary>
	/// <typeparam name="T">The type of data being returned from the function.</typeparam>
	public class ChangeValue<T>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ChangeValue{T}"/> class.</summary>
		/// <param name="result">The change status.</param>
		/// <param name="data">The data returned by the function.</param>
		internal ChangeValue(ChangeStatus result, [MaybeNull] T data)
		{
			this.Status = result;
			this.Value = data;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the change status.</summary>
		/// <value>The change status.</value>
		public ChangeStatus Status { get; }

		/// <summary>Gets the value returned by the function.</summary>
		/// <value>The value.</value>
		[property: MaybeNull]
		public T Value { get; }
		#endregion
	}
}