#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class DuplicateFilesItem(string name, bool? shared, DateTime timestamp, string user)
	{
		#region Public Properties
		public string Name => name;

		public bool? Shared => shared;

		public DateTime Timestamp => timestamp;

		public string User => user;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}