namespace RobinHood70.HoodBot.Jobs.Design
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.ViewModel;
	using static RobinHood70.WikiCommon.Globals;

	public class JobConstructorEqualityComparer : IEqualityComparer<JobNode>
	{
		public bool Equals(JobNode x, JobNode y)
		{
			if (x.Constructor == null)
			{
				return y.Constructor != null;
			}

			if (x.Constructor != y?.Constructor)
			{
				return false;
			}

			if (x.Parameters == y.Parameters)
			{
				return true;
			}

			// If constructors are equal, parameter names and counts should be equal as well.
			foreach (var param in x.Parameters)
			{
				if (param.Value != y.Parameters[param.Key])
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(JobNode obj) => CompositeHashCode(obj.Constructor, obj.Parameters);
	}
}