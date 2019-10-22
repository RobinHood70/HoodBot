#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class DuplicateFileItem
	{
		#region Constructors
		internal DuplicateFileItem(string name, bool? shared, DateTime timestamp, string user)
		{
			this.Name = name;
			this.Shared = shared;
			this.Timestamp = timestamp;
			this.User = user;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public bool? Shared { get; }

		public DateTime Timestamp { get; }

		public string User { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
