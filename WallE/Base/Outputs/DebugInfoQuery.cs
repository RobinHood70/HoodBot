#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DebugInfoQuery
	{
		#region Constructors
		public DebugInfoQuery(string function, bool isMaster, double runTime, string sql)
		{
			this.Function = function;
			this.IsMaster = isMaster;
			this.RunTime = runTime;
			this.Sql = sql;
		}
		#endregion

		#region Public Properties
		public string Function { get; }

		public bool IsMaster { get; }

		public double RunTime { get; }

		public string Sql { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => $"({this.Function}) {this.Sql}";
		#endregion
	}
}