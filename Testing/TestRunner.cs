namespace RobinHood70.Testing
{
	using System.Collections.Generic;

	public abstract class TestRunner : ITestRunner
	{
		#region Constructors
		protected TestRunner(ITestForm parentForm, WikiInfo wikiInfo)
		{
			this.ParentForm = parentForm;
			this.WikiInfo = wikiInfo;
		}
		#endregion

		#region Protected Properties
		protected ITestForm ParentForm { get; }

		protected WikiInfo WikiInfo { get; }
		#endregion

		#region Public Abstract Methods
		public abstract void RunAll();

		public abstract void RunOne();

		public abstract void Setup();

		public abstract void Teardown();
		#endregion

		#region Protected Methods
		protected void Assert(bool condition, string message)
		{
			if (!condition)
			{
				this.ParentForm.AppendResults(message);
			}
		}

		protected void CheckCollection<T>(IReadOnlyCollection<T> collection, string name)
		{
			if (collection == null)
			{
				this.ParentForm.AppendResults($"Collection {name} is null");
				return;
			}

			if (collection.Count == 0)
			{
				this.ParentForm.AppendResults($"Collection {name} has no members");
			}
		}

		protected void CheckForNull(object check, string name)
		{
			if (check == null)
			{
				this.ParentForm.AppendResults($"{name} is null");
			}
		}
		#endregion
	}
}
