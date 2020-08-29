namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Properties;
	using static RobinHood70.CommonCode.Globals;
	#region Public Enumerations

	/// <summary>A combination of the HTTP method and the content type.</summary>
	public enum RequestType
	{
		/// <summary>HTTP GET method with form-urlencoded data.</summary>
		Get,

		/// <summary>HTTP POST method with form-urlencoded data.</summary>
		Post,

		/// <summary>HTTP POST method with multipart form data.</summary>
		PostMultipart
	}
	#endregion

	/// <summary>Builds a form query parameter by parameter. Provides methods to add parameters of built-in types and convert them to what MediaWiki expects. Parameters will maintain insertion order.</summary>
	/// <seealso cref="KeyedCollection{TKey, TItem}" />
	/// <remarks>The current implementation of the Dictionary class seems to maintain insertion order, but this is not documented and should not be relied upon.</remarks>
	// Note: Throughout this class, conditions are specified as booleans. While these could certainly be changed to Predicates to delay evaluation until execution time rather than at call time, there is no advantage to doing so here—it would only add overhead (from creating the closure), rather than reducing it.
	[Serializable]
	public class Request : KeyedCollection<string, Parameter>
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Request" /> class.</summary>
		/// <param name="baseUri">The base URI.</param>
		/// <param name="requestType">The request type.</param>
		/// <param name="supportsUnitSeparator">if set to <see langword="true" /> [supports unit separator].</param>
		public Request(Uri baseUri, RequestType requestType, bool supportsUnitSeparator)
		{
			ThrowNull(baseUri, nameof(baseUri));
			this.Uri = baseUri;
			this.Type = requestType;
			this.SupportsUnitSeparator = supportsUnitSeparator;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the prefix to prepend to each parameter name.</summary>
		/// <value>The prefix. This can be null or empty if no prefix is required.</value>
		public string Prefix { get; set; } = string.Empty;

		/// <summary>Gets a value indicating whether the wiki supports \x1F unit separators.</summary>
		/// <value><see langword="true" /> if the wiki supports \x1F unit separators; otherwise, <see langword="false" />.</value>
		public bool SupportsUnitSeparator { get; }

		/// <summary>Gets or sets the request type. Defaults to <see cref="RequestType.Get" />.</summary>
		/// <value>The requesty type.</value>
		public RequestType Type { get; set; }

		/// <summary>Gets the base URI for the request.</summary>
		/// <value>The base URI for the request.</value>
		public Uri Uri { get; }
		#endregion

		#region Public Methods

		/// <summary>Adds a parameter with no value.</summary>
		/// <param name="name">The parameter name.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name) => this.Add(name, string.Empty);

		/// <summary>Adds a boolean parameter if the value is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, bool value) => value ? this.Add(name) : this;

		/// <summary>Adds an enumeration parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		// Enum.ToString() has a bug that can cause 0-valued enums to appear in outputs if there's more than one of them (e.g., None = 0, Default = None). Enum.GetName() does not seem to suffer from this, so we use that instead.
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Not a normalization")]
		public Request Add(string name, Enum value) =>
			value?.GetType().GetEnumName(value) is string enumName
				? this.Add(name, enumName.ToLowerInvariant())
				: this;

		/// <summary>Adds a DateTime parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, DateTime value) => this.Add(name, value.ToMediaWiki());

		/// <summary>Adds a nullable-DateTime parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, DateTime? value) => value != null ? this.Add(name, value.Value) : this;

		/// <summary>Adds a file parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, string fileName, byte[] value)
		{
			if (value == null || value.Length == 0)
			{
				throw new ArgumentException(Resources.EmptyFile);
			}

			this.Add(new FileParameter(this.Prefix + name, fileName, value));
			return this;
		}

		/// <summary>Adds a numeric parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value. This must be an <see cref="IConvertible"/> that can be converted to a <see cref="long"/> value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, IConvertible? value) => value == null ? this : this.Add(name, value.ToInt64(CultureInfo.CurrentCulture).ToStringInvariant());

		/// <summary>Adds a string parameter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value. If null, will be converted to an empty string.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, string? value)
		{
			ThrowNull(name, nameof(name));
			this.Add(new StringParameter(this.Prefix + name, value));
			return this;
		}

		/// <summary>Adds a DateTime enumeration parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request Add(string name, IEnumerable<DateTime>? values)
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
		/// <remarks>Value order must not be important, and duplicate values will be removed.</remarks>
		public Request Add(string name, IEnumerable<string>? values) => values == null || values.IsEmpty()
			? this
			: this.AddPiped(name, new HashSet<string>(values));

		/// <summary>Adds an enumerable string parameter if it has at least one value.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>Values added by this method can be modified using the source collection.</remarks>
		public Request Add(string name, ICollection<string>? values) => values == null || values.Count == 0
			? this
			: this.AddPiped(name, values);

		/// <summary>Adds a collection of numeric parameter if the collection value is non-null.</summary>
		/// <typeparam name="T">Any <see cref="IConvertible"/> type that can be converted to a <see cref="long"/> value.</typeparam>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>A copy of the values will be added to the request, not the original list. The new list will be converted to <see cref="long"/> numbers, sorted numerically with duplicates removed, then put into a <see cref="PipedParameter"/>.</remarks>
		public Request Add<T>(string name, IEnumerable<T>? values)
			where T : IConvertible
		{
			// Note: IEnumerable<IConvertible> fails to capture some calls, so generics are used instead.
			if (values != null)
			{
				var sorted = new SortedSet<long>();
				foreach (var value in values)
				{
					sorted.Add(value.ToInt64(CultureInfo.CurrentCulture));
				}

				var newList = new List<string>(sorted.Count);
				foreach (var value in sorted)
				{
					newList.Add(value.ToStringInvariant() ?? string.Empty);
				}

				this.AddPiped(name, newList);
			}

			return this;
		}

		/// <summary>Adds a toggled Filter parameter if the value is not All. The name of the parameter is fixed, and the value emitted is either <c>trueValue</c> or <c>!trueValue</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="trueValue">The true value.</param>
		/// <param name="filter">The filter variable.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddFilterPiped(string name, string trueValue, Filter filter)
		{
			ThrowNull(trueValue, nameof(trueValue));
			return filter switch
			{
				Filter.Only => this.AddToPiped(name, trueValue),
				Filter.Exclude => this.AddToPiped(name, '!' + trueValue),
				_ => this,
			};
		}

		/// <summary>Adds a toggled Filter parameter if the value is not All and the condition is true. The name of the parameter is fixed, and the value emitted is either <c>trueValue</c> or <c>!trueValue</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="trueValue">The true value.</param>
		/// <param name="filter">The filter variable.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddFilterPipedIf(string name, string trueValue, Filter filter, bool condition) => condition ? this.AddFilterPiped(name, trueValue, filter) : this;

		/// <summary>Adds a text-based Filter parameter if the value is not All. The name of the parameter is fixed, and the value emitted is either <c>onlyName</c> or <c>filterName</c>, depending on the value of the filter.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="onlyName">The text to emit when the filter is set to Only.</param>
		/// <param name="filterName">The text to emit when the filter is set to Filter.</param>
		/// <param name="filter">The filter variable.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddFilterText(string name, string onlyName, string filterName, Filter filter) => filter switch
		{
			Filter.Only => this.Add(name, onlyName),
			Filter.Exclude => this.Add(name, filterName),
			_ => this,
		};

		// TODO: Add AddFlags that distinguishes between None and Default and sends empty parameter for None instead of nothing. Update all relevant calls and flag values as appropriate.

		/// <summary>Adds a flags parameter as a pipe-separated string, provided at least one flag is set. The text added will be the same as the enumeration value name, converted to lower-case.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		[SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Not a normalization")]
		public Request AddFlags(string name, Enum values)
		{
			ThrowNull(values, nameof(values));
			var list = new List<string>();
			foreach (var prop in values.GetUniqueFlags())
			{
				// Enum.ToString() has a bug that can cause 0-valued enums to appear in outputs if there's more than one of them (e.g., None = 0, Default = None). Enum.GetName() does not seem to suffer from this.
				if (values.GetType().GetEnumName(prop) is string enumName)
				{
					list.Add(enumName.ToLowerInvariant());
				}
			}

			this.Add(name, list);
			return this;
		}

		/// <summary>Adds a flags parameter if the condition is true and at least one flag is set.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddFlagsIf(string name, Enum values, bool condition) => condition ? this.AddFlags(name, values) : this;

		/// <summary>Adds the format parameter.</summary>
		/// <param name="value">The format parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddFormat(string value)
		{
			ThrowNull(value, nameof(value));
			this.Add(new StringParameter("format", value, ValueType.Modify));
			return this;
		}

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens).</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddHidden(string name, string? value)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(value, nameof(value)); // Unlike regular Add, there is no condition in which this should be null.
			this.Add(new StringParameter(this.Prefix + name, value, ValueType.Hidden));
			return this;
		}

		/// <summary>Adds a set of string parameters whose values should be hidden for debugging (e.g., captcha data).</summary>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddHidden(IEnumerable<KeyValuePair<string, string>>? values)
		{
			if (values != null)
			{
				foreach (var kvp in values)
				{
					this.AddHidden(kvp.Key, kvp.Value);
				}
			}

			return this;
		}

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens) if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddHiddenIf(string name, string? value, bool condition) => condition ? this.AddHidden(name, value) : this;

		/// <summary>Adds a string parameter whose value should be hidden for debugging (e.g., passwords or tokens)if the value is not null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddHiddenIfNotNull(string name, string? value) => value != null ? this.AddHidden(name, value) : this;

		/// <summary>Adds a boolean parameter if both it and the specified conidition are true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIf(string name, bool value, bool condition) => this.Add(name, value && condition);

		/// <summary>Adds an enumeration parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIf(string name, Enum value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds a numeric parameter if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value. This must be an <see cref="IConvertible"/> that can be converted to a <see cref="long"/> value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIf(string name, IConvertible? value, bool condition) => (condition && value != null) ? this.Add(name, value.ToInt64(CultureInfo.CurrentCulture).ToStringInvariant()) : this;

		/// <summary>Adds a string parameter if the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIf(string name, string value, bool condition) => condition ? this.Add(name, value) : this;

		/// <summary>Adds an enumerable string parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIf(string name, IEnumerable<string>? values, bool condition) => condition ? this.Add(name, values) : this;

		/// <summary>Adds an enumerable parameter if the value collection is non-null and the condition is true.</summary>
		/// <typeparam name="T">Any <see cref="IConvertible"/> type that can be converted to a <see cref="long"/> value.</typeparam>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The values.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>A copy of the values will be added to the request, not the original list. The new list will be converted to <see cref="long"/> numbers, sorted with duplicates removed, then put into a <see cref="PipedParameter"/>.</remarks>
		public Request AddIf<T>(string name, IEnumerable<T>? values, bool condition)
			where T : IConvertible => condition ? this.Add(name, values) : this;

		/// <summary>Adds a string parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfNotNull(string name, string? value) => value != null ? this.Add(name, value) : this;

		/// <summary>Adds a string parameter if the value is non-null and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfNotNullIf(string name, string? value, bool condition) => (condition && value != null) ? this.Add(name, value) : this;

		/// <summary>Adds an enumeration parameter if its integer value is greater than zero.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfPositive(string name, Enum value) => Convert.ToUInt64(value, CultureInfo.InvariantCulture) > 0 ? this.Add(name, value) : this;

		/// <summary>Adds a numeric parameter if the value is greater than zero.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value. This must be an <see cref="IConvertible"/> that can be converted to a <see cref="long"/> value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfPositive(string name, IConvertible value)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(value, nameof(value));
			var longValue = value.ToInt64(CultureInfo.CurrentCulture);
			return longValue > 0 ? this.Add(name, value) : this;
		}

		/// <summary>Adds an enumeration parameter if its integer value is greater than zero and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfPositiveIf(string name, Enum value, bool condition) => condition && Convert.ToUInt64(value, CultureInfo.InvariantCulture) > 0 ? this.Add(name, value) : this;

		/// <summary>Adds a numeric parameter if the value is greater than zero and the condition is true.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value. This must be an <see cref="IConvertible"/> that can be converted to a <see cref="long"/> value.</param>
		/// <param name="condition">The condition to check.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddIfPositiveIf(string name, IConvertible value, bool condition)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(value, nameof(value));
			var longValue = value.ToInt64(CultureInfo.CurrentCulture);
			return (condition && longValue > 0) ? this.Add(name, value) : this;
	}

		/// <summary>Adds or changes a string parameter if the value is non-null.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddOrChangeIfNotNull(string name, string? value)
		{
			// Removes any existing key and replaces it with the new key/value, not preserving order. We must remove and re-add, since the only way to set a value is to construct a new Parameter object.
			if (value != null)
			{
				this.Remove(name);
				this.Add(name, value);
			}

			return this;
		}

		/// <summary>Adds a value to an enumerable string parameter. If the parameter doesn't exist, it will be created.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value to add to the list.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>If a <see cref="PipedParameter"/> is created, the <see cref="PipedParameter.Values">Values</see> property will be a HashSet.</remarks>
		public Request AddToPiped(string name, string value)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(value, nameof(value));
			var newKey = this.Prefix + name;
			if (this.TryGetValue(newKey, out var param))
			{
				if (param is PipedParameter piped)
				{
					piped.Values.Add(value);
				}
				else
				{
					throw new InvalidOperationException(CurrentCulture(Resources.NotAPipedParameter, name));
				}
			}
			else
			{
				this.Add(new PipedParameter(newKey, new HashSet<string>() { value }));
			}

			return this;
		}

		/// <summary>Adds a Tristate parameter if the value is non-null. The name of the parameter changes depending on the value supplied in the tristate variable, and the value emitted is always null.</summary>
		/// <param name="trueName">Parameter name to use when the value is true.</param>
		/// <param name="falseName">Parameter name to use when the value is false.</param>
		/// <param name="tristateVar">The tristate parameter value.</param>
		/// <returns>The current collection (fluent interface).</returns>
		public Request AddTristate(string trueName, string falseName, Tristate tristateVar) => tristateVar switch
		{
			Tristate.True => this.Add(trueName),
			Tristate.False => this.Add(falseName),
			_ => this,
		};

		/// <summary>Builds the request using the specified IParameterVisitor.</summary>
		/// <param name="visitor">The visitor.</param>
		public void Build(IParameterVisitor visitor)
		{
			ThrowNull(visitor, nameof(visitor));
			foreach (var param in this)
			{
				param.Accept(visitor);
			}
		}

		/// <summary>Comparable to <see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)" />, attempts to get the value associated with the specified key.</summary>
		/// <param name="key">The key of the value to get.</param>
		/// <returns><see langword="true" /> if the collection contains an element with the specified key; otherwise, <see langword="false" />.</returns>
		public Parameter? ValueOrDefault(string key)
		{
			if (key != null)
			{
				if (this.Dictionary != null)
				{
					return this.Dictionary.TryGetValue(key, out var value) ? value : default;
				}

				foreach (var testItem in this)
				{
					if (this.GetKeyForItem(testItem) == key)
					{
						return testItem;
					}
				}
			}

			return default;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a displayable value for the request.</summary>
		/// <returns>An <i>unencoded</i>, displayable Uri-like string with hidden and binary values treated appropriately.</returns>
		public override string ToString() => RequestVisitorDisplay.Build(this);
		#endregion

		#region Protected Override Methods

		/// <summary>Gets the key for item.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The key corresponding to the item.</returns>
		protected override string GetKeyForItem(Parameter item) => (item ?? throw ArgumentNull(nameof(item))).Name;
		#endregion

		#region Private Methods

		/// <summary>Adds an piped list of strings, even if the value has no items.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="values">The parameter values.</param>
		/// <returns>The current collection (fluent interface).</returns>
		/// <remarks>Values added by this method can be modified using the source collection.</remarks>
		private Request AddPiped(string name, ICollection<string> values)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(values, nameof(values));
			var newKey = this.Prefix + name;
			this.Add(new PipedParameter(newKey, values));

			return this;
		}
		#endregion
	}
}
