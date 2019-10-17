#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class InformationItem
	{
		#region Constructors
		internal InformationItem(string name, RawMessageInfo text, IReadOnlyList<int> values)
		{
			this.Name = name;
			this.Text = text;
			this.Values = values;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public RawMessageInfo Text { get; }

		public IReadOnlyList<int> Values { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
