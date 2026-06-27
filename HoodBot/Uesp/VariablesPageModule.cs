namespace RobinHood70.HoodBot.Uesp;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

public class VariablesPageModule
{
	#region Public Constants
	public const string PropertyName = PropVariables.ModuleName;
	#endregion

	#region Fields
	private readonly Dictionary<string, IDictionary<string, string>> sets = new(StringComparer.Ordinal);
	#endregion

	#region Constructors
	public VariablesPageModule(IEnumerable<VariablesResult> variables)
	{
		foreach (var result in variables)
		{
			foreach (var item in result)
			{
				var setName = item.Set ?? string.Empty;
				if (!this.sets.TryGetValue(setName, out var set))
				{
					set = new Dictionary<string, string>(StringComparer.Ordinal);
					this.sets.Add(setName, set);
				}

				foreach (var entry in item.Dictionary)
				{
					set[entry.Key] = entry.Value;
				}
			}
		}
	}
	#endregion

	#region Public Properties
	public IReadOnlyDictionary<string, string> MainSet => this.sets.TryGetValue(string.Empty, out var mainSet)
		? mainSet.AsReadOnly()
		: ImmutableDictionary<string, string>.Empty;
	#endregion

	#region Public Static Methods
	public static (string Key, object Value) ParseVariablesResult(object result) => result is IEnumerable<VariablesResult> variables
		? (PropertyName, new VariablesPageModule(variables))
		: throw new InvalidOperationException($"Unexpected result type: {result?.GetType().FullName ?? "null"}");
	#endregion

	#region Public Methods
	public IDictionary<string, string> GetSet(string setName) => this.sets.TryGetValue(setName, out var set)
		? set
		: ImmutableDictionary<string, string>.Empty;

	public string? GetVariable(string name) =>
		this.MainSet != null && this.MainSet.TryGetValue(name, out var retval)
			? retval
			: default;

	public string? GetVariable(string setName, string name) =>
		this.sets.TryGetValue(setName ?? string.Empty, out var set) &&
		set.TryGetValue(name, out var retval)
			? retval
			: null;
	#endregion
}