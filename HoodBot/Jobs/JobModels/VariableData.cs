namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;

	internal sealed class VariableData<T> : List<T>
		where T : IEquatable<T>
	{
		#region Public Properties
		public bool AllEqual
		{
			get
			{
				if (this.Count == 0)
				{
					throw new InvalidOperationException();
				}

				for (var i = 1; i < this.Count; i++)
				{
					if (!this[i].Equals(this[0]))
					{
						return false;
					}
				}

				return true;
			}
		}

		public string TextAfter { get; set; } = string.Empty;

		public string TextBefore { get; set; } = string.Empty;
		#endregion

		#region Public Override Methods
		public override string? ToString() => this.AllEqual
			? this[0].ToString()
			: this.TextBefore + "{{Nowrap|[" + string.Join(" / ", this) + "]}}" + this.TextAfter;
		#endregion
	}
}
