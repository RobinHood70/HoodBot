namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>This class acts as a mutable-once dictionary. It is initialized as an empty dictionary, which you can update once via the <see cref="CopyFrom(IReadOnlyDictionary{string, string})"/> method. After that, it becomes immutable.</summary>
	public class VariableDictionary : IReadOnlyDictionary<string, string>
	{
		#region Fields
		private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
		private bool initialized = false;
		#endregion

		#region Constructors
		public VariableDictionary()
		{
		}

		public VariableDictionary(IReadOnlyDictionary<string, string> other) => this.CopyFrom(other);
		#endregion

		#region Public Properties
		public IEnumerable<string> Keys => ((IReadOnlyDictionary<string, string>)this.variables).Keys;

		public IEnumerable<string> Values => ((IReadOnlyDictionary<string, string>)this.variables).Values;

		public int Count => this.variables.Count;
		#endregion

		#region Indexers
		public string this[string key] => this.variables[key];
		#endregion

		#region Public Methods
		public bool ContainsKey(string key) => this.variables.ContainsKey(key);

		public void CopyFrom(IEnumerable<KeyValuePair<string, string>> other)
		{
			if (this.initialized)
			{
				throw new InvalidOperationException("VariableDictionary is immutable once initialized.");
			}

			ThrowNull(other, nameof(other));
			this.initialized = true;
			foreach (var item in other)
			{
				this.variables.Add(item.Key, item.Value);
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IReadOnlyDictionary<string, string>)this.variables).GetEnumerator();

		public bool TryGetValue(string key, out string value) => this.variables.TryGetValue(key, out value);

		IEnumerator IEnumerable.GetEnumerator() => ((IReadOnlyDictionary<string, string>)this.variables).GetEnumerator();
		#endregion
	}
}
