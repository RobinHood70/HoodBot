#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class LanguageLinksItem
	{
		#region Public Properties
		public string Autonym { get; set; }

		public string Language { get; set; }

		public string Name { get; set; }

		public string Title { get; set; }

		public Uri Url { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Language + ":" + this.Title;
		#endregion
	}
}
