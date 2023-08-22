#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SiteInfoSubscribedHook
	{
		#region Constructors
		internal SiteInfoSubscribedHook(string name, IReadOnlyList<string> subscribers)
		{
			this.Name = name;
			this.Subscribers = subscribers;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public IReadOnlyList<string> Subscribers { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}