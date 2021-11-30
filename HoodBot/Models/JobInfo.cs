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
			this.Constructor = constructor.NotNull(nameof(constructor));
			this.Groups = jobInfo.NotNull(nameof(jobInfo)).Groups;
			this.Name = jobInfo.Name;

			ParameterInfo[] constructorParameters = constructor.GetParameters()
				.NotNull(ValidationType.Method, nameof(constructor), nameof(constructor.GetParameters));
			List<ConstructorParameter> parameters = new(constructorParameters.Length);
			foreach (ParameterInfo parameter in constructor.GetParameters())
			{
				if (!string.Equals(parameter.ParameterType.Name, nameof(WikiJob.JobManager), StringComparison.Ordinal))
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
			Type wikiJobType = typeof(WikiJob);
			foreach (Type type in Assembly.GetCallingAssembly().GetTypes())
			{
				if (type.IsSubclassOf(wikiJobType))
				{
					foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
					{
						if (constructor.GetCustomAttribute<JobInfoAttribute>() is JobInfoAttribute jobInfo)
						{
							yield return new JobInfo(constructor, jobInfo);
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
			List<object?> objectList = new() { jobManager.NotNull(nameof(jobManager)) };
			if (this.Parameters is IReadOnlyList<ConstructorParameter> jobParams)
			{
				foreach (ConstructorParameter param in jobParams)
				{
					objectList.Add(param.Attribute is JobParameterFileAttribute && param.Value is string value ? Environment.ExpandEnvironmentVariables(value) : param.Value);
				}
			}

			return (WikiJob)this.Constructor.Invoke(objectList.ToArray());
		}
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as JobInfo);

		public override int GetHashCode() => this.Constructor.GetHashCode();

		public override string? ToString() => this.Name;
		#endregion
	}
}
