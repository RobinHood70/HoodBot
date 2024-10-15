namespace RobinHood70.WikiCommon.Parser
{
	using System.Collections.Generic;

	/// <summary>Represents a template argument, such as <c>{{{1|}}}</c>.</summary>
	public interface IArgumentNode : IWikiNode, IParentNode
	{
		#region Properties

		/// <summary>Gets the default value.</summary>
		/// <value>The default value. This will be <see langword="null"/> if there is no default value (e.g., <c>{{{1}}}</c>) in order to distinguish it from a node with an empty default value (e.g., <c>{{{1|}}}</c>).</value>
		/// <remarks>To prevent the possibility of DefaultValue being set to a WikiNodeCollection from another object, it cannot be set directly. Use the provided methods to add or remove default values. You may also trim extraneous values from the object (only available by iterating the ArgumentNode itself).</remarks>
		WikiNodeCollection? DefaultValue { get; }

		/// <summary>Gets any additional values after the default value (e.g., the b in {{{1|a|b}}}).</summary>
		/// <value>The extra values.</value>
		/// <remarks>The MediaWiki software allows constructs such as <c>{{{1|a|b}}}</c> but will only take <c>a</c> as the default value in that instance, ignoring <c>b</c> altogether. This property provides access to values beyond the first so that no information is lost.</remarks>
		IReadOnlyList<IParameterNode>? ExtraValues { get; }

		/// <summary>Gets the name of the argument.</summary>
		/// <value>The argument name.</value>
		WikiNodeCollection Name { get; }
		#endregion

		#region Methods

		/// <summary>Removes the default value.</summary>
		void RemoveDefaultValue();

		/// <summary>Adds a default value. If one exists, this will overwrite it.</summary>
		/// <param name="value">The value to add.</param>
		void SetDefaultValue(IEnumerable<IWikiNode> value);

		/// <summary>Trims all extra values from the argument.</summary>
		void TrimExtraValues();
		#endregion
	}
}