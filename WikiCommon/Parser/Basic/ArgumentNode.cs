namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Properties;

	/// <summary>Represents a template argument, such as <c>{{{1|}}}</c>.</summary>
	public class ArgumentNode : IArgumentNode
	{
		#region Fields
		private List<IParameterNode>? extraValues;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ArgumentNode"/> class.</summary>
		/// <param name="factory">The factory to use when creating new nodes (must match the <paramref name="defaultValue"/> factory).</param>
		/// <param name="name">The title.</param>
		/// <param name="defaultValue">The default value. May be null or an empty collection. If populated, this should preferentially be either a single ParameterNode or a collection of IWikiNodes representing the default value itself. For compatibility with MediaWiki, it can also be a list of parameter nodes, in which case, these will be added as individual entries to the <see cref="ExtraValues"/> collection.</param>
		public ArgumentNode(IWikiNodeFactory factory, IEnumerable<IWikiNode> name, IList<IParameterNode> defaultValue)
		{
			this.Factory = factory.NotNull(nameof(factory));
			this.Name = new NodeCollection(factory, name.NotNull(nameof(name)));
			if (defaultValue.NotNull(nameof(defaultValue)).Count > 0)
			{
				foreach (var parameter in defaultValue)
				{
					if (parameter.Factory != factory)
					{
						throw new InvalidOperationException(Resources.FactoriesMustMatch);
					}
				}

				// defaultValue comes to us from WikiStack as a list of IParameterNodes, but is never actually treated as such, so we morph the main default value into a NodeCollection then, if there are junk values after that, we add them to ExtraValues unaltered.
				NodeCollection nodes = new(factory);
				if (defaultValue[0].Name is NodeCollection valueName)
				{
					nodes.AddRange(valueName);
					nodes.AddText("=");
				}

				nodes.AddRange(defaultValue[0].Value);
				this.DefaultValue = nodes;

				if (defaultValue.Count > 1)
				{
					List<IParameterNode> remaining = new();
					for (var i = 1; i < defaultValue.Count; i++)
					{
						remaining.Add(defaultValue[i]);
					}

					this.extraValues = remaining;
				}
			}
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public NodeCollection? DefaultValue { get; private set; }

		/// <inheritdoc/>
		public IReadOnlyList<IParameterNode>? ExtraValues => this.extraValues;

		/// <inheritdoc/>
		public IWikiNodeFactory Factory { get; }

		/// <inheritdoc/>
		public NodeCollection Name { get; }

		/// <inheritdoc/>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				if (this.Name != null)
				{
					yield return this.Name;
				}

				if (this.DefaultValue != null)
				{
					yield return this.DefaultValue;
				}

				if (this.extraValues != null)
				{
					foreach (var value in this.extraValues)
					{
						if (value.Name != null)
						{
							yield return value.Name;
						}

						yield return value.Value;
					}
				}
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Removes the default value.</summary>
		public void RemoveDefaultValue() => this.DefaultValue = null;

		/// <summary>Adds a default value. If one exists, this will overwrite it.</summary>
		/// <param name="value">The value to add.</param>
		public void SetDefaultValue(IEnumerable<IWikiNode> value) => this.DefaultValue = new NodeCollection(this.Factory, value);

		/// <summary>Trims all extra values from the argument.</summary>
		public void TrimExtraValues() => this.extraValues = null;
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			var retval = "{{{arg";
			var def = this.DefaultValue;
			if (def != null)
			{
				retval += '|';
				if (def.Count > 0)
				{
					retval += "default";
				}

				if (this.extraValues != null)
				{
					retval += $"|Extra Count = {this.extraValues.Count}";
				}
			}

			return retval + "}}}";
		}
		#endregion
	}
}