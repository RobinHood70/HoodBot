namespace RobinHood70.Robby
{
	using System;
	using System.Globalization;
	using System.Runtime.Serialization;
	using static Properties.Resources;

	[Serializable]
	public class UnexpectedResultException : Exception
	{
		#region Constructors
		public UnexpectedResultException()
		{
		}

		public UnexpectedResultException(string message)
			: base(message)
		{
		}

		public UnexpectedResultException(string methodName, string issue, params object[] parameters)
			: base(FormatMessage(methodName, issue, parameters))
		{
		}

		public UnexpectedResultException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		protected UnexpectedResultException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion

		#region Private Static Methods
		private static string FormatMessage(string methodName, string issue, params object[] parameters)
		{
			var issueParsed = string.Format(CultureInfo.CurrentCulture, issue, parameters);
			return string.Format(CultureInfo.CurrentCulture, UnexpectedResult, methodName, issueParsed);
		}
		#endregion
	}
}