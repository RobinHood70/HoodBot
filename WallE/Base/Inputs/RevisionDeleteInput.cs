#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum RevisionDeleteProperties
	{
		None = 0,
		Content = 1,
		Comment = 1 << 1,
		User = 1 << 2,
		All = Content | Comment | User
	}

	public enum RevisionDeleteSuppression
	{
		NoChange,
		Yes,
		No
	}

	public enum RevisionDeleteType
	{
		Revision,
		Archive,
		OldImage,
		FileArchive,
		Logging
	}
	#endregion

	public class RevisionDeleteInput
	{
		#region Constructors
		public RevisionDeleteInput(RevisionDeleteType type, IEnumerable<long> ids)
		{
			ArgumentNullException.ThrowIfNull(ids);
			this.Type = type;
			this.Ids = ids;
		}
		#endregion

		#region Public Properties
		public RevisionDeleteProperties Hide { get; set; }

		public IEnumerable<long> Ids { get; }

		public string? Reason { get; set; }

		public RevisionDeleteProperties Show { get; set; }

		public RevisionDeleteSuppression Suppress { get; set; }

		public string? Target { get; set; }

		public string? Token { get; set; }

		public RevisionDeleteType Type { get; }
		#endregion
	}
}