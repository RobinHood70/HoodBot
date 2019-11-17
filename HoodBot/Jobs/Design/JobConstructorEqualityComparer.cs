namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.ViewModel;
	using static RobinHood70.WikiCommon.Globals;

	public class JobConstructorEqualityComparer : IEqualityComparer<JobNode>
	{
		public bool Equals(JobNode x, JobNode y)
		{
			if (x == null)
			{
				return y == null;
			}

			if (y == null)
			{
				return false;
			}

			if (x.Constructor == null)
			{
				return y.Constructor != null;
			}

			if (x.Constructor != y.Constructor)
			{
				return false;
			}

			if (x.Parameters == y.Parameters)
			{
				return true;
			}

			if (x.Parameters == null || y.Parameters == null)
			{
				return x.Parameters == null && y.Parameters == null;
			}

			if (x.Parameters.Count != y.Parameters.Count)
			{
				return false;
			}

			// If constructors are equal, parameter names and counts should be equal as well.
			for (var i = 0; i < x.Parameters.Count; i++)
			{
				if (x.Parameters[i] != y.Parameters[i])
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(JobNode obj) => obj == null ? 0 : CompositeHashCode(obj.Constructor, obj.Parameters);
	}
}