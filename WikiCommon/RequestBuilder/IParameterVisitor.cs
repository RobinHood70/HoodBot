namespace RobinHood70.WikiCommon.RequestBuilder
{
	/// <summary>Specifies the methods required by all parameter visitor implementations.</summary>
	public interface IParameterVisitor
	{
		#region Methods

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		void Visit(FileParameter parameter);

		/// <summary>Visits the specified multi-valued object (specifically, PipedParameter or PipedListParameter).</summary>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		void Visit(PipedParameter parameter);

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		void Visit(StringParameter parameter);
		#endregion
	}
}