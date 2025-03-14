﻿namespace RobinHood70.WikiCommon.Parser;

using System.Collections.Generic;

/// <summary>Represents a parameter to a template or link.</summary>
public interface IParameterNode : IWikiNode, IParentNode
{
	#region Properties

	/// <summary>Gets a value indicating whether this <see cref="IParameterNode">parameter</see> is anonymous.</summary>
	/// <value><see langword="true"/> if anonymous; otherwise, <see langword="false"/>.</value>
	bool Anonymous { get; }

	/// <summary>Gets the name of the parameter, if not anonymous.</summary>
	/// <value>The name.</value>
	WikiNodeCollection? Name { get; }

	/// <summary>Gets the parameter value.</summary>
	/// <value>The value.</value>
	WikiNodeCollection Value { get; }
	#endregion

	#region Methods

	/// <summary>Adds a name to a previously anonymous parameter.</summary>
	/// <param name="name">The name.</param>
	void AddName(IEnumerable<IWikiNode> name);

	/// <summary>Changes the parameter from a named parameter to an anonymous one.</summary>
	void Anonymize();
	#endregion
}