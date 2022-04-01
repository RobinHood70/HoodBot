﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.CommonCode;

	#region Public Enumerations
	public enum TagOperation
	{
		Create,
		Delete,
		Activate,
		Deactivate
	}
	#endregion

	public class ManageTagsInput
	{
		#region Constructors
		public ManageTagsInput(TagOperation operation, string tag)
		{
			this.Operation = operation;
			this.Tag = tag.NotNullOrWhiteSpace();
		}
		#endregion

		#region Public Properties
		public bool IgnoreWarnings { get; set; }

		public TagOperation Operation { get; }

		public string? Reason { get; set; }

		public string Tag { get; }

		public string? Token { get; set; }
		#endregion
	}
}
