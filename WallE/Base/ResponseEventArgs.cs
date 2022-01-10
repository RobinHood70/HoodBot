﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class ResponseEventArgs : EventArgs
	{
		#region Constructors
		public ResponseEventArgs(string response)
		{
			this.Response = response;
		}
		#endregion

		#region Public Properties
		public string Response { get; }
		#endregion
	}
}