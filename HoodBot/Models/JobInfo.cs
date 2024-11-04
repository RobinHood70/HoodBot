namespace RobinHood70.HoodBot.Models
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs;

	public sealed class JobInfo : IEquatable<JobInfo>
	{
		#region Constructors
		private JobInfo(ConstructorInfo constructor, JobInfoAttribute jobInfo)
		{
			ArgumentNullException.ThrowIfNull(constructor);
			ArgumentNullException.ThrowIfNull(jobInfo);
			var constructorParameters = constructor.GetParameters();
			Globals.ThrowIfNull(constructorParameters, nameof(constructor), nameof(constructor.GetParameters));

			this.Constructor = constructor;
			this.Groups = jobInfo.Groups;
			this.Name = jobInfo.Name;
			List<ConstructorParameter> parameters = new(constructorParameters.Length);
			foreach (var parameter in constructor.GetParameters())
			{
				if (!parameter.ParameterType.Name.OrdinalEquals(nameof(JobManager)))
				{
					parameters.Add(new ConstructorParameter(parameter));
				}
			}

			parameters.TrimExcess();
			this.Parameters = parameters;
		}
		#endregion

		#region Public Properties
		public ConstructorInfo Constructor { get; }

		public IReadOnlyList<string> Groups { get; }

		public bool Login { get; private set; } = true;

		public string Name { get; }

		public IReadOnlyList<ConstructorParameter> Parameters { get; }
		#endregion

		#region Public Operators
		public static bool operator ==(JobInfo? left, JobInfo? right) =>
			ReferenceEquals(left, right) || (left is not null && left.Equals(right));

		public static bool operator !=(JobInfo? left, JobInfo? right) => !(left == right);
		#endregion

		#region Public Static Methods
		public static IEnumerable<JobInfo> GetAllJobs()
		{
			var wikiJobType = typeof(WikiJob);
			foreach (var type in Assembly.GetCallingAssembly().GetTypes())
			{
				if (type.IsSubclassOf(wikiJobType))
				{
					foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
					{
						if (constructor.GetCustomAttribute<JobInfoAttribute>() is JobInfoAttribute jobInfo)
						{
							var job = new JobInfo(constructor, jobInfo);
							if ((
								constructor.GetCustomAttribute<NoLoginAttribute>() ??
								type.GetCustomAttribute<NoLoginAttribute>()) is not null)
							{
								job.Login = false;
							}

							yield return job;
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		public bool Equals(JobInfo? other) => other != null && this.Constructor == other.Constructor;

		public WikiJob Instantiate(JobManager jobManager)
		{
			ArgumentNullException.ThrowIfNull(jobManager);
			List<object?> objectList = [jobManager];
			if (this.Parameters is IReadOnlyList<ConstructorParameter> jobParams)
			{
				foreach (var param in jobParams)
				{
					objectList.Add(param.Attribute is JobParameterFileAttribute && param.Value is string value ? Environment.ExpandEnvironmentVariables(value) : param.Value);
				}
			}

			return (WikiJob)this.Constructor.Invoke([.. objectList]);
		}
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as JobInfo);

		public override int GetHashCode() => this.Constructor.GetHashCode();

		public override string? ToString() => this.Name;
		#endregion
	}
}