#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class ExtensionItem
	{
		#region Constructors
		internal ExtensionItem(string type, string? author, string? credits, string? description, string? descriptionMessage, IReadOnlyList<string>? descriptionMessageParameters, string? license, string? licenseName, string? name, string? nameMessage, string? url, string? version, string? versionControlSystem, DateTime? versionControlSystemDate, string? versionControlSystemUrl, string? versionControlSystemVersion)
		{
			this.Type = type;
			this.Author = author;
			this.Credits = credits;
			this.Description = description;
			this.DescriptionMessage = descriptionMessage;
			this.DescriptionMessageParameters = descriptionMessageParameters;
			this.License = license;
			this.LicenseName = licenseName;
			this.Name = name;
			this.NameMessage = nameMessage;
			this.Url = url;
			this.Version = version;
			this.VersionControlSystem = versionControlSystem;
			this.VersionControlSystemDate = versionControlSystemDate;
			this.VersionControlSystemUrl = versionControlSystemUrl;
			this.VersionControlSystemVersion = versionControlSystemVersion;
		}
		#endregion

		#region Public Properties
		public string? Author { get; }

		public string? Credits { get; }

		public string? Description { get; }

		public string? DescriptionMessage { get; }

		public IReadOnlyList<string>? DescriptionMessageParameters { get; }

		public string? License { get; }

		public string? LicenseName { get; }

		public string? Name { get; }

		public string? NameMessage { get; }

		public string Type { get; }

		public string? Url { get; }

		public string? Version { get; }

		public string? VersionControlSystem { get; }

		public DateTime? VersionControlSystemDate { get; }

		public string? VersionControlSystemUrl { get; }

		public string? VersionControlSystemVersion { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Type;
		#endregion
	}
}
