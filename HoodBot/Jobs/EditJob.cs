namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public abstract class EditJob : WikiJob
	{
		private static readonly MethodInfo InternalGetCurrentMethod = Type.GetType("System.Reflection.RuntimeMethodInfo", true).GetMethod("InternalGetCurrentMethod", BindingFlags.Static | BindingFlags.NonPublic);
		private static readonly Type[] MyStackCrawlMarkRefType = new[] { typeof(MyStackCrawlMark).MakeByRefType() };
		private static MyGetCurrentMethodDelegate dynamicMethod = null;

		protected EditJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
				: base(site, asyncInfo, tasks)
		{
			this.ReadOnly = false;

			var find = MyStackCrawlMark.LookForMyCallersCaller;
			var constructor = MyGetCurrentMethod(ref find) as ConstructorInfo;
			if (constructor != null)
			{
				this.LogName = constructor.GetCustomAttribute<JobInfoAttribute>()?.Name;
			}
		}

		private delegate MethodBase MyGetCurrentMethodDelegate(ref MyStackCrawlMark mark);

		private enum MyStackCrawlMark
		{
			LookForMe,
			LookForMyCaller,
			LookForMyCallersCaller,
			LookForThread
		}

		#region Public Virtual Properties
		public virtual string LogDetails { get; protected set; }
		#endregion

		#region Public Abstract Properties
		public virtual string LogName { get; }
		#endregion

		#region Protected Override Methods
		protected override void OnCompleted()
		{
			this.Site.UserFunctions.EndLogEntry();
			base.OnCompleted();
		}

		protected override void OnStarted()
		{
			base.OnStarted();
			this.StatusWriteLine("Adding Log Entry");
			this.Site.UserFunctions.AddLogEntry(new LogInfo(this.LogName ?? this.GetType().Name, this.LogDetails, this.ReadOnly));
		}
		#endregion

		#region Private Static Methods
		private static MethodBase MyGetCurrentMethod(ref MyStackCrawlMark mark)
		{
			// This code taken from https://stackoverflow.com/questions/5143068/call-private-method-retaining-call-stack.
			// It's used in the constructor to find the specific calling constructor instantiating the class.
			if (dynamicMethod == null)
			{
				var m = new DynamicMethod("GetCurrentMethod", typeof(MethodBase), MyStackCrawlMarkRefType, true); // Ignore all privilege checks :D
				var gen = m.GetILGenerator();
				gen.Emit(OpCodes.Ldarg_0); // NO type checking here!
				gen.Emit(OpCodes.Call, InternalGetCurrentMethod);
				gen.Emit(OpCodes.Ret);
				Interlocked.CompareExchange(ref dynamicMethod, (MyGetCurrentMethodDelegate)m.CreateDelegate(typeof(MyGetCurrentMethodDelegate)), null);
			}

			return dynamicMethod(ref mark);
		}
		#endregion
	}
}