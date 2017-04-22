#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class ExtensionItem
	{
		#region Public Properties
		public string Author { get; set; }

		public string Credits { get; set; }

		public string Description { get; set; }

		public string DescriptionMessage { get; set; }

		public IReadOnlyList<string> DescriptionMessageParameters { get; set; }

		public string Type { get; set; }

		public string License { get; set; }

		public string LicenseName { get; set; }

		public string Name { get; set; }

		public string NameMessage { get; set; }

		public string Url { get; set; }

		public string Version { get; set; }

		public string VersionControlSystem { get; set; }

		public DateTime? VersionControlSystemDate { get; set; }

		public string VersionControlSystemUrl { get; set; }

		public string VersionControlSystemVersion { get; set; }
		#endregion
	}
}
