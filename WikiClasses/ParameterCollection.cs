namespace RobinHood70.WikiClasses
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.WikiClasses.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>A class for templates and file links which provides Parameter collection-related functionality.</summary>
	public class ParameterCollection : IList<Parameter>
	{
		#region Fields
		private readonly List<Parameter> parameters = new List<Parameter>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterCollection"/> class.</summary>
		public ParameterCollection()
			: this(StringComparer.Ordinal)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ParameterCollection"/> class.</summary>
		/// <param name="comparer">The string comparer used to compare parameter names.</param>
		public ParameterCollection(StringComparer comparer)
		{
			ThrowNull(comparer, nameof(comparer));
			this.Comparer = comparer;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the parameter collection filtered to anonymous parameters only.</summary>
		public IEnumerable<Parameter> AnonymousOnly
		{
			get
			{
				foreach (var param in this)
				{
					if (param.Anonymous)
					{
						yield return param;
					}
				}
			}
		}

		/// <summary>Gets the collection's a <see cref="IComparer{T}">comparer</see> for comparing collection names.</summary>
		public StringComparer Comparer { get; }

		/// <summary>Gets or sets a value indicating whether to copy the format of the final parameter when adding new ones.</summary>
		/// <value><see langword="true"/> if the last parameter's format should be copied; <see langword="false"/> to use <see cref="DefaultNameFormat"/> and <see cref="DefaultValueFormat"/>.</value>
		/// <remarks>If set to <see langword="true"/> and there are no parameters in the collection, the default formats will be used.</remarks>
		public bool CopyLast { get; set; } = true;

		/// <summary>Gets the number of parameters in this collection.</summary>
		public int Count => this.parameters.Count;

		/// <summary>Gets or sets the default format to use for the whitespace surrounding the Name property of any new parameters.</summary>
		/// <remarks>Only the whitespace properties are used for formatting; the Value property is ignored.</remarks>
		/// <seealso cref="CopyLast"/>
		public PaddedString DefaultNameFormat { get; set; } = new PaddedString();

		/// <summary>Gets or sets the default format to use for the whitespace surrounding the Value property of any new parameters.</summary>
		/// <remarks>Only the whitespace properties are used for formatting; the Value property is ignored.</remarks>
		/// <seealso cref="CopyLast"/>
		public PaddedString DefaultValueFormat { get; set; } = new PaddedString();

		/// <summary>Gets a value indicating whether the collection is read-only.</summary>
		bool ICollection<Parameter>.IsReadOnly => false;

		/// <summary>Gets an enumeration of the name or position of each parameter.</summary>
		/// <value>The parameter names and values.</value>
		public IEnumerable<string> PositionalNames
		{
			get
			{
				var anon = 0;
				foreach (var param in this.parameters)
				{
					if (param.Anonymous)
					{
						anon++;
					}

					yield return param.FullName?.Value ?? anon.ToStringInvariant();
				}
			}
		}

		/// <summary>Gets an enumeration of the name or position of each parameter, along with the parameter itself.</summary>
		/// <value>The parameter names and values.</value>
		public IEnumerable<(string positionalName, Parameter parameter)> PositionalParameters
		{
			get
			{
				var anon = 0;
				foreach (var param in this.parameters)
				{
					if (param.Anonymous)
					{
						anon++;
					}

					yield return (param.FullName?.Value ?? anon.ToStringInvariant(), param);
				}
			}
		}
		#endregion

		#region Public Indexers

		/// <summary>Gets or sets the <see cref="Parameter"/> at the specified index.</summary>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="Parameter"/>.</returns>
		public Parameter this[int index]
		{
			get => this.parameters[index];
			set => this.parameters[index] = value;
		}

		/// <summary>Gets the <see cref="Parameter"/> with the specified key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The specified <see cref="Parameter"/> or <see langword="null"/> if the key was not found.</returns>
		/// <remarks>Keys that evaluate to an integer will match either a named parameter or an anonymous parameter in the specified position (compared with other anonymous parameters, not the collection itself). Conflicts will be resolved following the same rules that MediaWiki templates and links use (i.e., last match wins). The collection's <see cref="Comparer"/> property will be used to determine if parameter names are a match.</remarks>
		public Parameter? this[string key]
		{
			get
			{
				// We have to search the entire collection, rather than bailing out at the first match, because parameters could be duplicated or have anonymous vs. numbered conflicts.
				Parameter? match = null;
				foreach (var (positionalName, parameter) in this.PositionalParameters)
				{
					if (this.Comparer.Compare(key, positionalName) == 0)
					{
						match = parameter;
					}
				}

				return match;
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Adds a <see cref="Parameter"/> to the collection.</summary>
		/// <param name="item">The <see cref="Parameter"/> to add to the collection.</param>
		public void Add(Parameter item) => this.parameters.Add(item);

		/// <summary>Adds a parameter with the specified name and value to the collection.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter Add(string name, string value)
		{
			var parameter = this.CreateFormattedParameter(name, value);
			this.Add(parameter);

			return parameter;
		}

		/// <summary>Adds a <see cref="Parameter"/> to the collection after the given name, or at the beginning if the name was not found.</summary>
		/// <param name="afterName">The parameter name to search for.</param>
		/// <param name="item">The <see cref="Parameter"/> to add to the collection.</param>
		public void AddAfter(string afterName, Parameter item)
		{
			ThrowNull(item, nameof(item));
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(item);
			}
			else
			{
				this.Insert(offset + 1, item);
			}
		}

		/// <summary>Adds a parameter with the specified name and value to the collection after the given name, or at the end if the name was not found.</summary>
		/// <param name="afterName">The parameter name to search for.</param>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter AddAfter(string afterName, string name, string value)
		{
			var item = this.CreateFormattedParameter(name, value);
			var offset = this.IndexOf(this[afterName]);
			if (offset == -1)
			{
				this.Add(item);
			}
			else
			{
				this.Insert(offset + 1, item);
			}

			return item;
		}

		/// <summary>Adds an anonymous parameter with the specified value to the collection.</summary>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> object that was added.</returns>
		public Parameter AddAnonymous(string value)
		{
			var p = new Parameter(value);
			this.Add(p);
			return p;
		}

		/// <summary>Adds a set of anonymous parameters with the specified values to the collection.</summary>
		/// <param name="values">The parameter values.</param>
		public void AddAnonymous(params string[] values) => this.AddAnonymous(values as IEnumerable<string>);

		/// <summary>Adds a set of anonymous parameters with the specified values to the collection.</summary>
		/// <param name="values">The parameter values.</param>
		public void AddAnonymous(IEnumerable<string> values)
		{
			if (values != null)
			{
				foreach (var value in values)
				{
					this.Add(new Parameter(value));
				}
			}
		}

		/// <summary>Adds a <see cref="Parameter"/> to the collection before the given name, or at the beginning if the name was not found.</summary>
		/// <param name="beforeName">The parameter name to search for.</param>
		/// <param name="item">The <see cref="Parameter"/> to add to the collection.</param>
		public void AddBefore(string beforeName, Parameter item)
		{
			var offset = this.IndexOf(this[beforeName]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, item);
		}

		/// <summary>Adds a parameter with the specified name and value to the collection after the given name, or at the beginning if the name was not found.</summary>
		/// <param name="beforeName">The parameter name to search for.</param>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The <see cref="Parameter"/> that was added.</returns>
		public Parameter AddBefore(string beforeName, string name, string value)
		{
			var item = this.CreateFormattedParameter(name, value);
			var offset = this.IndexOf(this[beforeName]);
			if (offset == -1)
			{
				offset = 0;
			}

			this.Insert(offset, item);

			return item;
		}

		/// <summary>Adds a parameter to the collection if a parameter with that name is blank or does not exist.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The new parameter, or null if the parameter already had a non-blank value.</returns>
		public Parameter? AddIfBlank(string name, string value)
		{
			var param = this[name];
			if (param == null)
			{
				return this.Add(name, value);
			}
			else if (param.Value.Length == 0 && !string.IsNullOrEmpty(value))
			{
				param.Value = value;
				return param;
			}

			return null;
		}

		/// <summary>Adds a parameter to the collection if a parameter with that name does not already exist.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter AddIfNotPresent(string name, string value) => this[name] ?? this.Add(name, value);

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter? AddOrChange(string name, string value) => this.AddOrChange(name, value, false);

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter? AddOrChange(string name, int value) => this.AddOrChange(name, value.ToString(CultureInfo.InvariantCulture), false);

		/// <summary>Adds a parameter with the specified name and value, or changes the value if the parameter already exists.</summary>
		/// <param name="name">The parameter name.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="onlyChangeIfBlank">Whether to change the parameter value only if the current value is blank or does not exist.</param>
		/// <returns>The parameter with the name provided.</returns>
		public Parameter? AddOrChange(string name, string value, bool onlyChangeIfBlank)
		{
			var param = this[name];
			this.AddOrChangeInternal(param, name, value, onlyChangeIfBlank);
			return param;
		}

		/// <summary>Removes the parameter name, making it into an anonymous parameter, or a numbered parameter if anonymization is not possible.</summary>
		/// <param name="name">The current parameter name.</param>
		/// <param name="position">The anonymous position the parameter should end up in.</param>
		/// <returns><see langword="true"/> if the parameter was successfully anonymized; otherwise <see langword="false"/>.</returns>
		/// <remarks>If the parameter cannot be anonymized due to an equals sign in the current value, the parameter name will be changed to the position number if it's not already.</remarks>
		public bool Anonymize(string name, int position) => this.Anonymize(name, position, position.ToStringInvariant());

		/// <summary>Removes the parameter name, making it into an anonymous parameter, or changing the name of the parameter to the specified label if anonymization is not possible.</summary>
		/// <param name="name">The current parameter name.</param>
		/// <param name="position">The anonymous position the parameter should end up in.</param>
		/// <param name="label">The name to change the parameter to if anonymization is not possible.</param>
		/// <returns><see langword="true"/> if the parameter was successfully anonymized; otherwise <see langword="false"/>.</returns>
		/// <remarks>If the parameter cannot be anonymized due to an equals sign in the current value, the parameter name will be changed to the position number if it's not already.</remarks>
		public bool Anonymize(string name, int position, string label)
		{
			var retval = false;
			var param = this[name];
			if (param != null)
			{
				retval |= param.Anonymize(label);
				if (param.Anonymous)
				{
					var newPos = this.GetAnonymousPosition(param);
					if (newPos != position)
					{
						throw new InvalidOperationException(CurrentCulture(Resources.AnonymizeBad, param.Name, newPos, position));
					}
				}
			}

			return retval;
		}

		/// <summary>Builds the collection into the specified <see cref="StringBuilder"/>.</summary>
		/// <param name="builder">The <see cref="StringBuilder"/> to append to.</param>
		/// <returns>The <paramref name="builder"/> to allow for method chaining.</returns>
		public virtual StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			if (this.parameters.Count > 0)
			{
				foreach (var param in this.parameters)
				{
					builder.Append('|');
					param.Build(builder);
				}
			}

			return builder;
		}

		/// <summary>Removes all parameters from the collection.</summary>
		public void Clear() => this.parameters.Clear();

		/// <summary>Determines whether this collection contains the specified parameter.</summary>
		/// <param name="item">The parameter to locate in the collection.</param>
		/// <returns><see langword="true"/> if <paramref name="item" /> is found in the collection; otherwise, <see langword="false"/>.</returns>
		public bool Contains(Parameter item) => this.parameters.Contains(item);

		/// <summary>Determines whether this collection contains a parameter with the specified name.</summary>
		/// <param name="name">The parameter name to locate in the collection.</param>
		/// <returns><see langword="true"/> if <paramref name="name" /> is found in the collection; otherwise, <see langword="false"/>.</returns>
		public bool Contains(string name) => this[name] != null;

		/// <summary>Copies the elements of the collection to an array, starting at a particular array index.</summary>
		/// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(Parameter[] array, int arrayIndex) => this.parameters.CopyTo(array, arrayIndex);

		/// <summary>Creates a deep copy of the existing collection.</summary>
		/// <param name="copy">The collection to copy to.</param>
		public virtual void DeepCloneTo(ParameterCollection copy)
		{
			ThrowNull(copy, nameof(copy));
			copy.CopyLast = this.CopyLast;
			copy.DefaultNameFormat = this.DefaultNameFormat.Clone();
			copy.DefaultValueFormat = this.DefaultValueFormat.Clone();
			foreach (var param in copy)
			{
				this.parameters.Add(param.DeepClone());
			}
		}

		/// <summary>Returns a <see cref="HashSet{T}"/> of duplicate parameter names within the collection.</summary>
		/// <returns>A <see cref="HashSet{T}"/> of duplicate parameter names within the collection (ignoring null values).</returns>
		public HashSet<string> DuplicateNames()
		{
			var names = new HashSet<string>();
			var duplicates = new HashSet<string>();
			foreach (var param in this)
			{
				var name = param.Name;
				if (name != null)
				{
					if (names.Contains(name))
					{
						duplicates.Add(name);
					}
					else
					{
						names.Add(name);
					}
				}
			}

			return duplicates;
		}

		/// <summary>Finds a parameter whose name is in the parameter list, preferring names that come first in parameter order.</summary>
		/// <param name="names">The parameter names to check for.</param>
		/// <returns>The best match among the <see cref="Parameter"/>s or <see langword="null"/> if no match was found.</returns>
		/// <remarks>This function is primarily intended to reflect MediaWiki's template search. If a template parameter were specified as <c>{{{name|{{{1|}}}}}}</c>, you would call this methods as <c>FindFirst("name", "1");</c> in order to find the "name" parameter if it exists, or the "1" parameter (either specifically named or anonymous) if "name" was not found.</remarks>
		public Parameter? FindFirst(params string[] names)
		{
			ThrowNull(names, nameof(names));
			foreach (var name in names)
			{
				var retval = this[name];
				if (retval != null)
				{
					return retval;
				}
			}

			return null;
		}

		/// <summary>Forces all parameters to have names that match their anonymous position, overriding any existing aliases. This does not affect their status as anonymous.</summary>
		public void ForcePositionalNames()
		{
			var anon = 0;
			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				param.Name = anon.ToStringInvariant();
			}
		}

		/// <summary>Gets the position of an anonymous parameter within the collection.</summary>
		/// <param name="item">The parameter to find.</param>
		/// <returns>The anonymous position of the parameter within the collection. This value is 1-based, corresponding to MediaWiki's numbering. In other words, given a well-formed collection, the anonymous parameter corresponding to <c>{{{2|}}}</c> would return 2. A value of 0 will be returned if the parameter specified is not anonymous; a value of -1 will be returned if the parameter was not found.</returns>
		public int GetAnonymousPosition(Parameter item)
		{
			ThrowNull(item, nameof(item));
			if (!item.Anonymous)
			{
				return 0;
			}

			var anon = 0;
			foreach (var param in this.AnonymousOnly)
			{
				anon++;
				if (param == item)
				{
					return anon;
				}
			}

			return -1;
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Parameter> GetEnumerator() => this.parameters.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => this.parameters.GetEnumerator();

		/// <summary>Determines whether this instance has anonymous parameters.</summary>
		/// <returns><see langword="true"/> if this instance has anonymous parameters; otherwise, <see langword="false"/>.</returns>
		public bool HasAnonymous()
		{
			foreach (var param in this)
			{
				if (param.Anonymous)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>Determines whether this collection has multiple identically named or numbered parameters.</summary>
		/// <returns><see langword="true"/> if this instance has duplicates; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method uses short-circuit logic and is therefore usually faster than <see cref="DuplicateNames"/> if knowing whether duplicates exist is all that's required.</remarks>
		public bool HasDuplicates()
		{
			var names = new HashSet<string>(this.Comparer);
			foreach (var name in this.PositionalNames)
			{
				if (names.Contains(name))
				{
					return true;
				}

				names.Add(name);
			}

			return false;
		}

		/// <summary>Determines the index of a specific item in the list.</summary>
		/// <param name="item">The object to locate in the list.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		/// <seealso cref="GetAnonymousPosition(Parameter)"/>
		public int IndexOf(Parameter? item) => item == null ? -1 : this.parameters.IndexOf(item);

		/// <summary>Inserts a <see cref="Parameter"/> into the collection at the specified index (not to be confused with the anonymous position).</summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The parameter to insert into the collection.</param>
		public void Insert(int index, Parameter item) => this.parameters.Insert(index, item);

		/// <summary>Removes the first occurrence of the specified <see cref="Parameter"/> from the collection.</summary>
		/// <param name="item">The <see cref="Parameter"/> to remove from the collection.</param>
		/// <returns><see langword="true"/> if <paramref name="item" /> was successfully removed from the collection; otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if <paramref name="item" /> is not found in the original collection.</returns>
		public bool Remove(Parameter item) => this.parameters.Remove(item);

		/// <summary>Removes all parameters with the specified names from the collection.</summary>
		/// <param name="names">The parameter names to remove.</param>
		/// <remarks>This function will remove all copies of any parameters with the given names, including numeric names, alias names and anonymous parameters by position.</remarks>
		/// <returns><see langword="true"/> if any parameters were removed; otherwise, <see langword="false"/>.</returns>
		public bool Remove(params string[] names)
		{
			ThrowNull(names, nameof(names));
			var uniqueNames = new HashSet<string>(names, this.Comparer);

			// Copy the list of parameter names so we're not corrupting the actual name for anonymous parameters, if aliases are present.
			var allNames = new List<string>(this.PositionalNames);

			// Now, check the created names against those provided and remove all parameters with those names, including duplicates, anonymous, and numbered parameters.
			var retval = false;
			for (var i = this.parameters.Count - 1; i >= 0; i--)
			{
				var paramName = this.parameters[i].Name;
				if (uniqueNames.Contains(allNames[i]) || (paramName != null && uniqueNames.Contains(paramName)))
				{
					this.parameters.RemoveAt(i);
					retval = true;
				}
			}

			return retval;
		}

		/// <summary>Removes the <see cref="Parameter"/> at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index) => this.parameters.RemoveAt(index);

		/// <summary>Cleans any parameters that would be ignored by a wiki because they have duplicate names (including hidden names for unnamed parameters).</summary>
		/// <returns>True if anything was removed.</returns>
		public bool RemoveDuplicates()
		{
			var keyOrder = new List<string>();
			var parameterCopy = new Dictionary<string, Parameter>();
			foreach (var (positionalName, parameter) in this.PositionalParameters)
			{
				if (parameterCopy.TryGetValue(positionalName, out var item))
				{
					keyOrder.Remove(positionalName);
				}

				keyOrder.Add(positionalName);
				parameterCopy[positionalName] = parameter;
			}

			var retval = keyOrder.Count != this.Count;
			this.Clear();
			foreach (var name in keyOrder)
			{
				this.Add(parameterCopy[name]);
			}

			return retval;
		}

		/// <summary>Removes all parameters that have no value, except anonymous parameters (which may be important for ordering).</summary>
		/// <returns><see langword="true"/> if any values were removed; otherwise <see langword="false"/>.</returns>
		public bool RemoveEmpty()
		{
			var retval = false;
			for (var i = this.parameters.Count - 1; i >= 0; i--)
			{
				var param = this[i];
				if (!param.Anonymous && param.Value.Length == 0)
				{
					retval = true;
					this.parameters.RemoveAt(i);
				}
			}

			return retval;
		}

		/// <summary>Removes all parameters specified if they're named or numbered parameters and have no value.</summary>
		/// <param name="names">The parameter names to check. Numbered names will only be applied to explicitly numbered parameters.</param>
		/// <returns><see langword="true"/> if any values were removed; otherwise <see langword="false"/>.</returns>
		public bool RemoveEmpty(params string[] names)
		{
			var retval = false;
			if (names != null)
			{
				var uniqueNames = new HashSet<string>(names);
				for (var i = this.parameters.Count - 1; i >= 0; i--)
				{
					var param = this[i];
					if (!param.Anonymous && param.Value.Length == 0 && param.Name != null && uniqueNames.Contains(param.Name))
					{
						this.parameters.RemoveAt(i);
					}
				}
			}

			return retval;
		}

		/// <summary>Removes a parameter if its value matches the value specified.</summary>
		/// <param name="name">The name of the parameter to check.</param>
		/// <param name="value">The value to check for.</param>
		/// <returns><see langword="true"/> if the parameter was removed; otherwise, <see langword="false"/>.</returns>
		public bool RemoveIfEquals(string name, string value)
		{
			ThrowNull(name, nameof(name));
			var param = this[name];
			if (param != null && param.Value == value)
			{
				this.Remove(param);
				return true;
			}

			return false;
		}

		/// <summary>Removes the specified parameter if it matches the provided predicate.</summary>
		/// <param name="name">The name of the parameter to check.</param>
		/// <param name="predicate">The predicate to check against.</param>
		/// <returns><see langword="true"/> if the parameter was removed; otherwise, <see langword="false"/>.</returns>
		public bool RemoveIf(string name, Predicate<string> predicate)
		{
			ThrowNull(name, nameof(name));
			ThrowNull(predicate, nameof(predicate));
			var param = this[name];
			if (param != null && predicate(param.Value))
			{
				this.Remove(param);
				return true;
			}

			return false;
		}

		/// <summary>Removes the named parameter if the predicate evaluates to true; otherwise, changes it to the provided value.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <param name="removeCondition">The condition under which the value should be removed (typically, when it's the default value for the parameter or should not be displayed at all).</param>
		/// <returns><see langword="true"/> if the template was changed in any way; <see langword="false"/> only if the predicate evaluates to false and the value was already set to the provided value.</returns>
		public bool RemoveOrChange(string name, string value, bool removeCondition)
		{
			ThrowNull(name, nameof(name));
			return removeCondition ? this.Remove(name) : this.AddOrChangeInternal(this[name], name, value, false);
		}

		/// <summary>Removes the named parameter if the predicate evaluates to true; otherwise, changes it to the provided value.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <param name="removeCondition">The predicate.</param>
		/// <returns><see langword="true"/> if the template was changed in any way; <see langword="false"/> only if the predicate evaluates to false and the value was already set to the provided value.</returns>
		public bool RemoveOrChange(string name, string value, Predicate<Parameter?> removeCondition)
		{
			ThrowNull(removeCondition, nameof(removeCondition));
			var param = this[name];
			return removeCondition(param) ? this.Remove(name) : this.AddOrChangeInternal(param, name, value, false);
		}

		/// <summary>Renames the specified parameter.</summary>
		/// <param name="from">What to rename the parameter from.</param>
		/// <param name="to">What to rename the parameter to.</param>
		/// <returns><see langword="true"/> if the parameter was renamed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="InvalidOperationException">The <paramref name="to" /> name already exists in the collection (except if it matches the <paramref name="from"/> name, which will be ignored).</exception>
		/// <remarks>To bypass the parameter name checks, use <see cref="Parameter.Rename(string)"/> instead.</remarks>
		public bool RenameParameter(string from, string to)
		{
			var fromParam = this[from];
			return fromParam == null ? false : this.RenameParameter(fromParam, to);
		}

		/// <summary>Renames the specified parameter.</summary>
		/// <param name="from">The <see cref="Parameter"/> to rename.</param>
		/// <param name="to">What to rename the parameter to.</param>
		/// <returns><see langword="true"/> if the parameter was renamed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="InvalidOperationException">The <paramref name="to" /> name already exists in the collection (except if it matches the <paramref name="from" /> name, which will be ignored).</exception>
		/// <remarks>To bypass the parameter name checks, use <see cref="Parameter.Rename(string)"/> instead.</remarks>
		public bool RenameParameter(Parameter from, string to) =>
			(from == null || from.Name == to) ? false :
			this.Contains(to) ? throw new InvalidOperationException(CurrentCulture(Resources.ParameterExists, to)) :
			from.Rename(to);

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public void Sort(params string[] sortOrder) => this.Sort(sortOrder as IEnumerable<string>);

		/// <summary>Sorts parameters in the order specified.</summary>
		/// <param name="sortOrder">A list of parameter names in the order to sort them.</param>
		/// <remarks>Any parameters not specified in <paramref name="sortOrder"/> will be moved after the specified parameters, and will otherwise retain their original order.</remarks>
		public void Sort(IEnumerable<string> sortOrder)
		{
			ThrowNull(sortOrder, nameof(sortOrder));
			var order = new ComparableCollection<string>(sortOrder, this.Comparer as IEqualityComparer<string>);

			/* Ensures that any parameters not specified in the order will retain their original sorting. */
			foreach (var name in this.PositionalNames)
			{
				if (!order.Contains(name))
				{
					order.Add(name);
				}
			}

			this.parameters.Sort(IndexedComparer);

			int IndexedComparer(Parameter param1, Parameter param2)
			{
				var index1 = param1.Name == null ? -1 : order.IndexOf(param1.Name);
				var index2 = param2.Name == null ? -1 : order.IndexOf(param2.Name);
				return index1.CompareTo(index2);
			}
		}

		/// <summary>Returns the value of the parameter if it exists; otherwise, <see cref="string.Empty"/>.</summary>
		/// <param name="key">The name of the parameter to get.</param>
		/// <returns>The value of the parameter if it exists; otherwise, <see cref="string.Empty"/>.</returns>
		public string ValueOrDefault(string key) => this.ValueOrDefault(key, string.Empty);

		/// <summary>Returns the value of the parameter if it exists; otherwise, <paramref name="defaultValue"/>.</summary>
		/// <param name="key">The name of the parameter to get.</param>
		/// <param name="defaultValue">The value to use if the parameter is not found.</param>
		/// <returns>The value of the parameter if it exists; otherwise, <paramref name="defaultValue"/>.</returns>
		public string ValueOrDefault(string key, string defaultValue) => this[key]?.Value ?? defaultValue;
		#endregion

		#region Public Override Methods

		/// <summary>Converts the collection to its full wiki text.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is a simple wrapper around the <see cref="Build(StringBuilder)"/> method.</remarks>
		public override string ToString() => this.Build(new StringBuilder()).ToString();
		#endregion

		#region Private Methods
		private bool AddOrChangeInternal(Parameter? param, string name, string value, bool onlyChangeIfBlank)
		{
			// Private method is because indexing is expensive here, and Predicate version would need to call it twice if it just sent the result of the predicate to the bool version.
			if (param == null)
			{
				this.Add(name, value);
				return true;
			}

			if (param.Value == value)
			{
				return false;
			}

			if (!onlyChangeIfBlank || string.IsNullOrWhiteSpace(param.Value))
			{
				param.Value = value;
				return true;
			}

			return false;
		}

		private Parameter CreateFormattedParameter(string name, string value)
		{
			var lastParam = (this.CopyLast && this.Count > 0) ? this[this.Count - 1] : null;
			return new Parameter(name == null ? null : this.CreateParameterString(lastParam, name, true), this.CreateParameterString(lastParam, value, false));
		}

		private PaddedString CreateParameterString(Parameter? lastParam, string value, bool fromName)
		{
			// Need to check FullName beforehand, since it could be null if last parameter is anonymous.
			var copyString =
				lastParam == null ? null :
				fromName ? lastParam.FullName : lastParam.FullValue;
			if (copyString == null)
			{
				copyString = fromName ? this.DefaultNameFormat : this.DefaultValueFormat;
			}

			return new PaddedString(copyString?.LeadingWhiteSpace ?? string.Empty, value, copyString?.TrailingWhiteSpace ?? string.Empty);
		}
		#endregion
	}
}