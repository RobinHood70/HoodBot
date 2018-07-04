namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "Replaces an array, which would be tagged as non-CLS compliant.")]
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class JobInfoAttribute : Attribute
	{
		public JobInfoAttribute(string name) => this.Name = name;

		public JobInfoAttribute(string name, string groupsText)
			: this(name)
		{
			var groupSplit = groupsText.Split('|');
			for (var i = 0; i < groupSplit.Length; i++)
			{
				groupSplit[i] = groupSplit[i].Trim();
			}

			this.Groups = groupSplit;
		}

		public IEnumerable<string> Groups { get; }

		public string Name { get; }

		public static IEnumerable<JobInfoAttribute> FindAll()
		{
			JobInfoAttribute attribute;
			var wikiJobType = typeof(WikiJob);
			foreach (var type in Assembly.GetCallingAssembly().GetTypes())
			{
				if (wikiJobType.IsAssignableFrom(type) && (attribute = type.GetCustomAttribute<JobInfoAttribute>()) != null)
				{
					yield return attribute;
				}
			}
		}
	}
}
