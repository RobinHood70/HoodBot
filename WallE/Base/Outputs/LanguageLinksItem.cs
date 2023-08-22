#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class LanguageLinksItem
	{
		#region Constructors
		internal LanguageLinksItem(string language, string title, string? autonym, string? name, Uri? url)
		{
			this.Language = language;
			this.Title = title;
			this.Autonym = autonym;
			this.Name = name;
			this.Url = url;
		}
		#endregion

		#region Public Properties
		public string? Autonym { get; }

		public string Language { get; }

		public string? Name { get; }

		public string Title { get; }

		public Uri? Url { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Language + ":" + this.Title;
		#endregion
	}
}