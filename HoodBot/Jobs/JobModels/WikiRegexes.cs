namespace RobinHood70.HoodBot.Jobs.JobModels;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

public static partial class WikiRegexes
{
	[GeneratedRegex(@"^==\s*(?<title>.*?)\s*==", RegexOptions.Multiline, Globals.DefaultGeneratedRegexTimeout)]
	public static partial Regex HeaderFinder();
}