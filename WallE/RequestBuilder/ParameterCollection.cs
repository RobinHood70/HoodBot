namespace RobinHood70.WallE.RequestBuilder
{
	// Note: Throughout this class, conditions are specified as booleans. While these could certainly be changed to Predicates to delay evaluation until execution time rather than at call time, there is no advantage to doing so here—it would only add overhead (from creating the closure), rather than reducing it.
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using static Properties.Messages;
	using static RobinHood70.Globals;

	/// <summary>Provides an easy-to-use parameter dictionary which maintains insertion order.</summary>
	/// <seealso cref="KeyedCollection{TKey, TItem}" />
	/// <remarks>The current implementation of the Dictionary class seems to maintain insertion order, but this is not documented and should not be relied upon.</remarks>
	[Serializable]
	public class ParameterCollection : KeyedCollection<string, IParameter>
	{
		#region Constants

		// Declared as a constant so we don't need to cast it in this.Add to make it clear we want to pass null specifically as a string.
		private const string NullString = null;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the prefix to prepend to each parameter name.</summary>
		/// <value>The prefix. This can be null or empty if no prefix is required.</value>
		public string Prefix { get; set; }
		#endregion

		#region Public Methods

		/// <summary>Adds a parameter with no value.</summary>
		/// <param name="name">The parameter name.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name) => this.Add(name, NullString);

		/// <summary>Adds a boolean parameter if the value is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, bool value) => value ? this.Add(name) : this;

		/// <summary>Adds an enumeration parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		// Enum.ToString() has a bug that can cause 0-valued enums to appear in outputs if there's more than one of them (e.g., None = 0, Default = None). Enum.GetName() does not seem to suffer from this, so we use that instead.
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Not a normalization")]
		public ParameterCollection Add(string name, Enum value) =>
			value != null ? this.Add(name, Enum.GetName(value.GetType(), value).ToLowerInvariant()) : this;

		/// <summary>Adds a DateTime parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, DateTime value) => this.Add(name, value.ToMediaWiki());

		/// <summary>Adds a nullable-DateTime parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, DateTime? value) => value != null ? this.Add(name, value.Value) : this;

		/// <summary>Adds a file parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, string fileName, byte[] value)
		{
			if (value != null)
			{
				this.Add(new FileParameter(this.Prefix + name, fileName, value));
			}

			return this;
		}

		/// <summary>Adds an integer parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, int value) => this.Add(name, value.ToStringInvariant());

		/// <summary>Adds a nullable-integer parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, int? value) => value != null ? this.Add(name, value.Value) : this;

		/// <summary>Adds a long parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, long value) => this.Add(name, value.ToStringInvariant());

		/// <summary>Adds a nullable-long parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, long? value) => value != null ? this.Add(name, value.Value) : this;

		/// <summary>Adds a string parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, string value)
		{
			this.Add(new StringParameter(this.Prefix + name, value));
			return this;
		}

		/// <summary>Adds a DateTime enumeration parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, IEnumerable<DateTime> values)
		{
			if (values != null)
			{
				var newList = new List<string>();
				foreach (var value in values)
				{
					newList.Add(value.ToMediaWiki());
				}

				this.Add(name, newList);
			}

			return this;
		}

		/// <summary>Adds an enumerable string parameter if it has at least one value.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection Add(string name, IEnumerable<string> values)
		{
			// Do not add if values is empty.
			if (values?.AsReadOnlyCollection().Count > 0)
			{
				this.AddForced(name, values);
			}

			return this;
		}

		/// <summary>Adds an enumerable IFormattable parameter if the value is non-null.</summary>
		/// <typeparam name="T">Any type that implements IFormattable.</typeparam>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>A sorted copy of the values is added, not the original list.</remarks>
		public ParameterCollection Add<T>(string name, IEnumerable<T> values)
			where T : IFormattable
		{
			if (values != null)
			{
				var newList = new List<string>();
				foreach (var value in values)
				{
					newList.Add(value.ToStringInvariant());
				}

				newList.Sort();
				this.Add(name, newList);
			}

			return this;
		}

		/// <summary>Adds a toggled FilterOption parameter if the value is not All. The name of the parameter is fixed, and the value emitted is either <c>trueValue</c> or <c>!trueValue</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="trueValue">The true value.</param>
		/// <param name="filterOption">The filter variable.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddFilterOptionPiped(string name, string trueValue, FilterOption filterOption)
		{
			switch (filterOption)
			{
				case FilterOption.Only:
					return this.Add(name, new string[] { trueValue });
				case FilterOption.Filter:
					return this.Add(name, new string[] { '!' + trueValue });
				default:
					return this;
			}
		}

		/// <summary>Adds a toggled FilterOption parameter if the value is not All and the condition is true. The name of the parameter is fixed, and the value emitted is either <c>trueValue</c> or <c>!trueValue</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="trueValue">The true value.</param>
		/// <param name="filterOption">The filter variable.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddFilterOptionPipedIf(string name, string trueValue, FilterOption filterOption, bool condition) => condition ? this.AddFilterOptionPiped(name, trueValue, filterOption) : this;

		/// <summary>Adds a text-based FilterOption parameter if the value is not All. The name of the parameter is fixed, and the value emitted is either <c>onlyName</c> or <c>filterName</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="onlyName">The text to emit when the filter is set to Only.</param>
		/// <param name="filterName">The text to emit when the filter is set to Filter.</param>
		/// <param name="filterOption">The filter variable.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddFilterOptionText(string name, string onlyName, string filterName, FilterOption filterOption)
		{
			switch (filterOption)
			{
				case FilterOption.Only:
					return this.Add(name, onlyName);
				case FilterOption.Filter:
					return this.Add(name, filterName);
				default:
					return this;
			}
		}

		/// <summary>Adds a flags parameter as a pipe-separated string, provided at least one flag is set. The text added will be the same as the enumeration value name, converted to lower-case.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Not a normalization")]
		public ParameterCollection AddFlags(string name, Enum values)
		{
			ThrowNull(values, nameof(values));
			var list = new List<string>();
			foreach (var prop in values.GetUniqueFlags())
			{
				// Enum.ToString() has a bug that can cause 0-valued enums to appear in outputs if there's more than one of them (e.g., None = 0, Default = None). Enum.GetName() does not seem to suffer from this.
				list.Add(Enum.GetName(values.GetType(), prop).ToLowerInvariant());
			}

			this.Add(name, list);
			return this;
		}

		/// <summary>Adds a flags parameter if the condition is true and at least one flag is set.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddFlagsIf(string name, Enum values, bool condition) => condition ? this.AddFlags(name, values) : this;

		/// <summary>Adds an enumerable string parameter, even if it evaluates to null or has no items.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddForced(string name, IEnumerable<string> values)
		{
			var newKey = this.Prefix + name;
			if (this.TryGetItem(newKey, out IParameter param))
			{
				var piped = param as PipedParameter;
				if (piped == null)
				{
					throw new InvalidOperationException(CurrentCulture(NotAPipedParameter, name));
				}

				piped.Value.UnionWith(values);
			}
			else
			{
				this.Add(new PipedParameter(newKey, values));
			}

			return this;
		}

		/// <summary>Adds the format parameter.</summary>
		/// <param name="value">The format parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddFormat(string value)
		{
			this.Add(new FormatParameter(value));
			return this;
		}

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens).</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddHidden(string name, string value)
		{
			this.Add(new HiddenParameter(this.Prefix + name, value));
			return this;
		}

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens) if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddHiddenIf(string name, string value, bool condition) => condition ? this.AddHidden(name, value) : this;

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens)if the value is not null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddHiddenIfNotNull(string name, string value) => value != null ? this.AddHidden(name, value) : this;

		/// <summary>Adds a boolean parameter if both it and the specified conidition are true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, bool value, bool condition) => this.Add(name, value && condition);

		/// <summary>Adds an enumeration parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, Enum value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds an integer parameter if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, int value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds a nullable-integer parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, int? value, bool condition) => (condition && value != null) ? this.Add(name, value.Value) : this;

		/// <summary>Adds a long value if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, long value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds a nullable-long parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, long? value, bool condition) => (condition && value != null) ? this.Add(name, value.Value) : this;

		/// <summary>Adds a string parameter if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, string value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds an enumerable string parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf(string name, IEnumerable<string> values, bool condition) => condition ? this.Add(name, values) : this;

		/// <summary>Adds an enumerable IFormattable parameter if the value is non-null and the condition is true.</summary>
		/// <typeparam name="T">Any IFormattable type.</typeparam>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIf<T>(string name, IEnumerable<T> values, bool condition)
			where T : IFormattable => condition ? this.Add(name, values) : this;

		/// <summary>Adds a string parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfNotNull(string name, string value) => value != null ? this.Add(name, value) : this;

		/// <summary>Adds a string parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfNotNullIf(string name, string value, bool condition) => (condition && value != null) ? this.Add(name, value) : this;

		/// <summary>Adds an enumeration parameter if its integer value is greater than zero.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositive(string name, Enum value) => Convert.ToUInt64(value, CultureInfo.InvariantCulture) > 0 ? this.Add(name, value) : this;

		/// <summary>Adds an integer parameter if the value is greater than zero.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositive(string name, int value) => value > 0 ? this.Add(name, value) : this;

		/// <summary>Adds a long parameter if the value is greater than zero.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositive(string name, long value) => value > 0 ? this.Add(name, value) : this;

		/// <summary>Adds an enumeration parameter if its integer value is greater than zero and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositiveIf(string name, Enum value, bool condition) => condition && Convert.ToUInt64(value, CultureInfo.InvariantCulture) > 0 ? this.Add(name, value) : this;

		/// <summary>Adds an integer parameter if the value is greater than zero and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositiveIf(string name, int value, bool condition) => condition && value > 0 ? this.Add(name, value) : this;

		/// <summary>Adds a long parameter if the value is greater than zero and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddIfPositiveIf(string name, long value, bool condition) => condition && value > 0 ? this.Add(name, value) : this;

		/// <summary>Adds an enumerable string parameter. Duplicate values will be emitted unaltered.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddList(string name, IEnumerable<string> values)
		{
			this.Add(new PipedListParameter(name, values));
			return this;
		}

		/// <summary>Adds or changes a string parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddOrChangeIfNotNull(string name, string value)
		{
			// Removes any existing key and replaces it with the new key/value, not preserving order. We must remove and re-add, since the only way to set a value is to construct a new Parameter object.
			if (value != null)
			{
				this.Remove(name);
				this.Add(name, value);
			}

			return this;
		}

		/// <summary>Adds a Tristate parameter if the value is non-null. The name of the parameter changes depending on the value supplied in the tristate variable, and the value emitted is always null.</summary>
		/// <param name="trueName">Parameter name to use when the value is true.</param>
		/// <param name="falseName">Parameter name to use when the value is false.</param>
		/// <param name="tristateVar">The tristate parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public ParameterCollection AddTristate(string trueName, string falseName, Tristate tristateVar)
		{
			switch (tristateVar)
			{
				case Tristate.True:
					return this.Add(trueName);
				case Tristate.False:
					return this.Add(falseName);
				default:
					return this;
			}
		}

		/// <summary>Tries to get the named item from the parameter collection. Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="item">The item.</param>
		/// <returns><c>true</c> if the item was found; otherwise <c>false</c>.</returns>
		public bool TryGetItem(string name, out IParameter item)
		{
			if (this.Dictionary == null)
			{
				foreach (var testItem in this)
				{
					if (this.GetKeyForItem(testItem) == name)
					{
						item = testItem;
						return true;
					}
				}
			}
			else
			{
				return this.Dictionary.TryGetValue(name, out item);
			}

			item = null;
			return false;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => RequestVisitorDisplay.Build(this);
		#endregion

		#region Protected Override Methods

		/// <summary>Gets the key for item.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The key corresponding to the item.</returns>
		protected override string GetKeyForItem(IParameter item) => item?.Name;
		#endregion
	}
}