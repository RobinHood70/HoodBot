namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;

	/// <summary>Interface for the general concept of an enumeration of titles.</summary>
	/// <typeparam name="TTitle">The type of titles in the enumeration. May be any derivative of <see cref="ISimpleTitle"/>.</typeparam>
	public interface ITitleCollection<out TTitle> : ISiteSpecific, IEnumerable<TTitle>
		where TTitle : ISimpleTitle
	{
	}
}
