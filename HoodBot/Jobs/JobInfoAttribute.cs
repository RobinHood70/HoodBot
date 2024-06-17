namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;

	[AttributeUsage(AttributeTargets.Constructor)]
	public sealed class JobInfoAttribute : Attribute
	{
		#region Constructors
		public JobInfoAttribute(string name)
			: this(name, null)
		{
		}

		[SuppressMessage("Design", "CA1019:Define accessors for attribute arguments", Justification = "Inappropriate here.")]
		public JobInfoAttribute(string name, string? groupsText)
		{
			this.Name = name;
			List<string> groups = [];
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