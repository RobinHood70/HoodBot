namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Replaces an array, which would be tagged as non-CLS compliant.")]
	[AttributeUsage(AttributeTargets.Constructor)]
	public sealed class JobInfoAttribute : Attribute
	{
		#region Constructors
		public JobInfoAttribute(string name)
			: this(name, null)
		{
		}

		public JobInfoAttribute(string name, string? groupsText)
		{
			this.Name = name;
			var groups = new List<string>();
			if (!string.IsNullOrWhiteSpace(groupsText))
			{
				var groupSplit = groupsText.Split(TextArrays.Pipe);
				foreach (var group in groupSplit)
				{
					groups.Add(group.Trim());
				}
			}

			this.Groups = groups;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Groups { get; }

		public string Name { get; }
		#endregion
	}
}