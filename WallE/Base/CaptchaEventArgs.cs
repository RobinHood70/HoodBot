namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	/// <summary>This class allows Captcha data to be passed back to the user, solved, and then the solution returned to the wiki.</summary>
	/// <remarks>Initializes a new instance of the <see cref="CaptchaEventArgs" /> class.</remarks>
	/// <param name="data">The Captcha data provided by the wiki.</param>
	/// <param name="solution">The solution to the Captcha provided by the client.</param>
	public class CaptchaEventArgs(IReadOnlyDictionary<string, string> data, IDictionary<string, string> solution) : EventArgs
	{
		#region Public Properties

		/// <summary>Gets Captcha data for an edit.</summary>
		/// <value>The captcha data.</value>
		/// <remarks>When the event is raised, CaptchaData will be filled with string-string key-value pairs as returned by the wiki.</remarks>
		public IReadOnlyDictionary<string, string> CaptchaData { get; } = data;

		/// <summary>Gets the dictionary containing the Captcha solution.</summary>
		/// <value>The dictionary containing the Captcha solution.</value>
		/// <remarks>On return, CaptchaSolve should be filled with string-string key-value pairs containing the data to solve the Captcha.</remarks>
		public IDictionary<string, string> CaptchaSolution { get; } = solution;
		#endregion

		#region Public Static Methods

		/// <summary>This is a quick and dirty solver for the "Simple" math algorithm provided with MediaWiki.</summary>
		/// <param name="sender">The wiki abstraction layer sending the Captcha event.</param>
		/// <param name="e">The Captcha event arguments.</param>
		public static void SolveSimple(IWikiAbstractionLayer sender, CaptchaEventArgs e)
		{
			if (sender != null && e != null && e.CaptchaData["type"].OrdinalEquals("simple"))
			{
				Regex math = new(@"(?<num1>\d+)(?<num2>[+-]\d+)", RegexOptions.None, Globals.DefaultRegexTimeout);
				var nums = math.Match(e.CaptchaData["question"].Replace('−', '-'));
				var solved = int.Parse(nums.Groups["num1"].Value, CultureInfo.InvariantCulture) + int.Parse(nums.Groups["num2"].Value, CultureInfo.InvariantCulture);

				e.CaptchaSolution.Add("captchaid", e.CaptchaData["id"]);
				e.CaptchaSolution.Add("captchaword", solved.ToStringInvariant());
			}
		}
		#endregion
	}
}