﻿namespace RobinHood70.HoodBot.Models
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using RobinHood70.HoodBot;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	public class JobInfo : IEquatable<JobInfo>
	{
		#region Constructors
		private JobInfo(ConstructorInfo constructor, JobInfoAttribute jobInfo)
		{
			ThrowNull(constructor, nameof(constructor));
			ThrowNull(jobInfo, nameof(jobInfo));
			this.Constructor = constructor;
			this.Groups = jobInfo.Groups;
			this.Name = jobInfo.Name;

			var constructorParameters = constructor.GetParameters();
			ThrowNull(constructorParameters, nameof(constructor), nameof(constructor.GetParameters));

			var parameters = new List<ConstructorParameter>(constructorParameters.Length - 2);
			foreach (var parameter in constructor.GetParameters())
			{
				switch (parameter.ParameterType.Name)
				{
					case nameof(WikiJob.AsyncInfo):
					case nameof(WikiJob.Site):
						break;
					default:
						parameters.Add(new ConstructorParameter(parameter));
						break;
				}
			}

			this.Parameters = parameters;
		}
		#endregion

		#region Public Properties
		public ConstructorInfo Constructor { get; }

		public IReadOnlyList<string> Groups { get; } = new List<string>();

		public string Name { get; }

		public IReadOnlyList<ConstructorParameter> Parameters { get; }
		#endregion

		#region Public Operators
		public static bool operator ==(JobInfo? left, JobInfo? right) =>
			ReferenceEquals(left, right) || (!(left is null) && left.Equals(right));

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
							yield return new JobInfo(constructor, jobInfo);
						}
					}
				}
			}
		}
		#endregion

		#region Public Methods
		public bool Equals(JobInfo? other) => other != null && this.Constructor == other.Constructor;

		public WikiJob Instantiate(Site site, AsyncInfo asyncInfo)
		{
			ThrowNull(site, nameof(site));
			var objectList = new List<object?> { site, asyncInfo };

			if (this.Parameters is IReadOnlyList<ConstructorParameter> jobParams)
			{
				foreach (var param in jobParams)
				{
					objectList.Add(param.Attribute is JobParameterFileAttribute && param.Value is string value ? Environment.ExpandEnvironmentVariables(value) : param.Value);
				}
			}

			return (WikiJob)this.Constructor.Invoke(objectList.ToArray());
		}
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as JobInfo);

		public override int GetHashCode() => HashCode.Combine(this.Constructor);

		public override string? ToString() => this.Name;
		#endregion
	}
}