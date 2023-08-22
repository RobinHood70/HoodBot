#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DebugInfoLog
	{
		#region Constructors
		public DebugInfoLog(string caller, string logType, string message)
		{
			this.Caller = caller;
			this.LogType = logType;
			this.Message = message;
		}
		#endregion

		#region Public Properties
		public string Caller { get; }

		public string LogType { get; }

		public string Message { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.LogType}: ({this.Caller}) {this.Message}";
		#endregion
	}
}