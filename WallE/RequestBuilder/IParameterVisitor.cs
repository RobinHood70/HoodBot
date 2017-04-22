namespace RobinHood70.WallE.RequestBuilder
{
	using System.Collections.Generic;

	/// <summary>Specifies the methods required by all parameter visitor implementations.</summary>
	public interface IParameterVisitor
	{
		#region Methods

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		void Visit(FileParameter parameter);

		/// <summary>Visits the specified FormatParameter object.</summary>
		/// <param name="parameter">The FormatParameter object.</param>
		void Visit(FormatParameter parameter);

		/// <summary>Visits the specified HiddenParameter object.</summary>
		/// <param name="parameter">The HiddenParameter object.</param>
		void Visit(HiddenParameter parameter);

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <typeparam name="T">An enumerable string collection.</typeparam>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>;

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		void Visit(StringParameter parameter);
		#endregion
	}
}
