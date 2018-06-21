﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using WikiCommon;

	public class BlockResult
	{
		#region Public Properties
		public DateTime? Expiry { get; set; }

		public BlockFlags Flags { get; set; }

		public long Id { get; set; }

		public string Reason { get; set; }

		public string User { get; set; }

		public long UserId { get; set; }

		public bool WatchUser { get; set; }
		#endregion
	}
}
