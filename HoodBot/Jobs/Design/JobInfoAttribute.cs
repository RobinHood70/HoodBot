namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Replaces an array, which would be tagged as non-CLS compliant.")]
	[AttributeUsage(AttributeTargets.Constructor)]
	public sealed class JobInfoAttribute : Attribute
	{
		public JobInfoAttribute(string name) => this.Name = name;

		public JobInfoAttribute(string name, string groupsText)
			: this(name)
		{
			if (!string.IsNullOrWhiteSpace(groupsText))
			{
				var groupSplit = groupsText.Split(TextArrays.Pipe);
				for (var i = 0; i < groupSplit.Length; i++)
				{
					groupSplit[i] = groupSplit[i].Trim();
				}

				this.Groups = groupSplit;
			}
		}

		public IEnumerable<string> Groups { get; }

		public string Name { get; }
	}
}